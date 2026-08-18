using BakToBucket.Features.Archiving;
using BakToBucket.Features.CloudflareR2;
using BakToBucket.Features.SqlBackup;
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
        var databases = options.IncludedDatabases;

        if (databases.Count == 0)
        {
            logger.LogWarning("No databases configured for backup.");
            return;
        }

        var writePath = options.EngineBackupPath;
        var readPath = !string.IsNullOrWhiteSpace(options.LocalBackupPath) 
            ? Path.GetFullPath(options.LocalBackupPath) 
            : EnsureBackupFolderExists(options.EngineBackupPath);

        Directory.CreateDirectory(readPath);
        
        var connectionString = GetConnectionString(options);
        var provider = GetBackupProvider(options.DatabaseType);

        logger.LogInformation("Starting backup cycle for {Count} databases.", databases.Count);

        string zipPath = string.Empty;
        try
        {
            await provider.BackupDatabasesAsync(connectionString, writePath, databases, cancellationToken);
            
            var hostName = !string.IsNullOrWhiteSpace(options.BackupHostName) ? options.BackupHostName : Environment.MachineName;
            var zipOutputDir = ResolveZipOutputDirectory(options, readPath);
            Directory.CreateDirectory(zipOutputDir);

            zipPath = await zipServices.CreateZipAsync(readPath, hostName, zipOutputDir, cancellationToken: cancellationToken);
            
            if (new FileInfo(zipPath).Length <= 22) 
            {
                throw new InvalidOperationException($"Backup generated is empty. Verify that 'LocalBackupPath' ({readPath}) points to the correct directory.");
            }

            if (!options.LocalOnly)
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
            logger.LogError(ex, "Backup orchestration failed.");
            throw;
        }
        finally
        {
            CleanupLocalFiles(zipPath, readPath, options.LocalOnly);
        }
    }

    internal string ResolveZipOutputDirectory(AppOptions options, string localReadPath)
    {
        if (!string.IsNullOrWhiteSpace(options.ZipOutputPath))
        {
            return Path.IsPathRooted(options.ZipOutputPath)
                ? Path.GetFullPath(options.ZipOutputPath)
                : Path.GetFullPath(Path.Combine(localReadPath, options.ZipOutputPath));
        }

        if (options.LocalOnly)
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

    internal string GetConnectionString(AppOptions options)
    {
        var connectionString = options.DatabaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? connOptions.Value.SqlServer
            : connOptions.Value.PostgreSql;

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Connection string for {options.DatabaseType} is missing.");

        return connectionString;
    }

    internal IBackupProvider GetBackupProvider(string databaseType)
    {
        return backupProviders.FirstOrDefault(p => p.DatabaseType.Equals(databaseType, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No backup provider found for: {databaseType}");
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
