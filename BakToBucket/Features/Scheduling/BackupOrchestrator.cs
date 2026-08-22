using BakToBucket.Features.Abstractions;
using BakToBucket.Features.Archiving;
using BakToBucket.Features.CloudflareR2;
using BakToBucket.Infrastructure.Diagnostics;
using Microsoft.Extensions.Options;

namespace BakToBucket.Features.Scheduling;

public sealed class BackupOrchestrator(
    IEnumerable<IBackupProvider> backupProviders,
    IZipServices zipServices,
    Uploader uploader,
    IOptions<AppOptions> appOptions,
    IOptions<ConnectionStringsOptions> connOptions,
    IOptions<RetentionOptions> retentionOptions,
    IBucketSizeChecker bucketSizeChecker,
    ILogger<BackupOrchestrator> logger)
{ 
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var options = appOptions.Value;

        var activeEngines = options.Engines
           .Where(e => e.Value.Enabled && e.Value.IncludedDatabases.Count > 0)
           .ToList();

        if (activeEngines.Count == 0)
        {
            logger.LogWarning("No database engines are enabled for backup.");
            return;
        }
        foreach (var engine in activeEngines)
        {
            await ProcessEngineBackupAsync(engine.Key, engine.Value, options, cancellationToken);
        }
    }

    private async Task ProcessEngineBackupAsync(DatabaseEngine databaseType, EngineOptions engineConfig, AppOptions globalOptions, CancellationToken cancellationToken)
    {
        var databases = engineConfig.IncludedDatabases;
        var writePath = engineConfig.EngineBackupPath;
        var readPath = !string.IsNullOrWhiteSpace(engineConfig.LocalBackupPath) 
            ? Path.GetFullPath(engineConfig.LocalBackupPath) 
            : EnsureBackupFolderExists(engineConfig.EngineBackupPath);

        Directory.CreateDirectory(readPath);
        
        var connectionString = GetConnectionString(databaseType);
        var provider = GetBackupProvider(databaseType);

        logger.LogInformation("Starting backup cycle for {Count} {DatabaseType} databases.", databases.Count, databaseType);

        string zipPath = string.Empty;
        try
        {
            await provider.BackupDatabasesAsync(connectionString, writePath, databases, cancellationToken);
            
            var hostName = !string.IsNullOrWhiteSpace(globalOptions.BackupHostName) ? globalOptions.BackupHostName : Environment.MachineName;
            var zipOutputDir = ResolveZipOutputDirectory(globalOptions.ZipOutputPath, globalOptions.LocalOnly, readPath);
            Directory.CreateDirectory(zipOutputDir);

            zipPath = await zipServices.CreateZipAsync(readPath, hostName, databaseType.ToString(), zipOutputDir, cancellationToken: cancellationToken);
            
            if (new FileInfo(zipPath).Length <= 22) 
            {
                throw new InvalidOperationException($"Backup generated is empty for {databaseType}. Verify that 'LocalBackupPath' ({readPath}) points to the correct directory.");
            }

            if (!globalOptions.LocalOnly)
            {
                if (await IsUploadPermitted(zipPath, cancellationToken))
                {
                    await uploader.UploadBackupAsync(zipPath, cancellationToken);
                }
            }
            else
            {
                logger.LogInformation("Local-only mode: backup preserved at {ZipPath}. Cloud upload skipped.", zipPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Backup orchestration failed for {DatabaseType}.", databaseType);
            throw;
        }
        finally
        {
            CleanupLocalFiles(zipPath, readPath, globalOptions.LocalOnly);
        }
    }

    internal string ResolveZipOutputDirectory(string? zipOutputPath, bool localOnly, string localReadPath)
    {
        if (!string.IsNullOrWhiteSpace(zipOutputPath))
        {
            return Path.IsPathRooted(zipOutputPath)
                ? Path.GetFullPath(zipOutputPath)
                : Path.GetFullPath(Path.Combine(localReadPath, zipOutputPath));
        }

        if (localOnly)
        {
            return Path.GetFullPath(Path.Combine(localReadPath, "Archives"));
        }

        return Path.GetTempPath();
    }

    internal string EnsureBackupFolderExists(string folderPath)
    {
        var path = string.IsNullOrWhiteSpace(folderPath) ? Path.Combine(AppContext.BaseDirectory, "Backup") : folderPath;
        path = Path.GetFullPath(path);
        Directory.CreateDirectory(path);
        return path;
    }
    internal string GetConnectionString(DatabaseEngine databaseType)
    {
        if (connOptions.Value.TryGetValue(databaseType, out var connString) && !string.IsNullOrWhiteSpace(connString))
            return connString;
        throw new InvalidOperationException($"Connection string for {databaseType} is missing.");
    }

    internal IBackupProvider GetBackupProvider(DatabaseEngine databaseType)
    {        
        return backupProviders.FirstOrDefault(p => p.DatabaseType == databaseType) ?? 
               throw new InvalidOperationException($"No backup provider found for: {databaseType}");
    }

    internal async Task<bool> IsUploadPermitted(string zipPath, CancellationToken cancellationToken)
    {
        var zipSize = new FileInfo(zipPath).Length;
        var currentBucketSize = await bucketSizeChecker.GetTotalBucketSizeAsync(cancellationToken);
        var maxBucketSize = retentionOptions.Value.MaxBucketSizeGB * 1024L * 1024L * 1024L;

        if (currentBucketSize + zipSize > maxBucketSize)
        {
            logger.LogCritical("Upload aborted. Bucket size {Current} + Zip {Zip} > Max {Max}.", 
                currentBucketSize, zipSize, maxBucketSize);
            return false;
        }

        return true;
    }

    private void CleanupLocalFiles(string zipPath, string readPath, bool isLocalOnly)
    {
        if (!isLocalOnly && File.Exists(zipPath))
        {
            try { File.Delete(zipPath); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to delete temporary zip file: {File}", zipPath); }
        }

        foreach (var bak in Directory.GetFiles(readPath, "*.bak"))
        {
            try { File.Delete(bak); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to delete local file: {File}", bak); }
        }
    }
}
