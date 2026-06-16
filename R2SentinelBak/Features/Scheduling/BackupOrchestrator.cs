using Microsoft.Extensions.Options;
using R2SentinelBak.Features.Archiving;
using R2SentinelBak.Features.CloudflareR2;
using R2SentinelBak.Features.SqlBackup;
using R2SentinelBak.Infrastructure.Diagnostics;

namespace R2SentinelBak.Features.Scheduling;

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

        var backupFolder = EnsureBackupFolderExists(options.BackupFolder);
        var connectionString = GetConnectionString(options);
        var provider = GetBackupProvider(options.DatabaseType);

        logger.LogInformation("Starting backup cycle for {Count} databases.", databases.Count);

        string zipPath = string.Empty;
        try
        {
            await provider.BackupDatabasesAsync(connectionString, backupFolder, databases, cancellationToken);
            
            var hostName = !string.IsNullOrWhiteSpace(options.BackupHostName) ? options.BackupHostName : Environment.MachineName;
            zipPath = await zipServices.CreateZipAsync(backupFolder, hostName, "", cancellationToken: cancellationToken);
            
            if (await IsUploadPermitted(zipPath, cancellationToken))
            {
                await uploader.UploadBackupAsync(zipPath, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Backup orchestration failed.");
            throw;
        }
        finally
        {
            CleanupLocalFiles(zipPath, backupFolder);
        }
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

    private void CleanupLocalFiles(string zipPath, string backupFolder)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);

        foreach (var bak in Directory.GetFiles(backupFolder, "*.bak"))
        {
            try { File.Delete(bak); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to delete local file: {File}", bak); }
        }
    }
}
