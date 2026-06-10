using R2SentinelBak.Features.Archiving;
using R2SentinelBak.Features.CloudflareR2;
using R2SentinelBak.Features.SqlBackup;

namespace R2SentinelBak.Features.Scheduling;

public sealed class BackupOrchestrator(
    ISqlBackupServices sqlBackupServices,
    IZipServices zipServices,
    Uploader uploader,
    IConfiguration configuration,
    ILogger<BackupOrchestrator> logger)
{
    private const string DefaultGetDbsQuery = "SELECT name FROM sys.databases WHERE database_id > 4 AND state_desc = 'ONLINE'";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration["Sentinel:DbConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Sentinel:DbConnectionString is required.");
        }

        var backupFolder = configuration["BackupFolder"];
        backupFolder = string.IsNullOrWhiteSpace(backupFolder)
            ? Path.Combine(AppContext.BaseDirectory, "Backup")
            : backupFolder;

        backupFolder = Path.GetFullPath(backupFolder);
        Directory.CreateDirectory(backupFolder);

        logger.LogInformation("Starting backup cycle using folder {BackupFolder}.", backupFolder);

        try
        {
            await sqlBackupServices.BackupDatabasesAsync(connectionString, backupFolder, DefaultGetDbsQuery);

            var zipPath = await zipServices.CreateZipAsync(backupFolder, cancellationToken: cancellationToken);
            logger.LogInformation("Backup folder compressed into {ZipPath}.", zipPath);

            var uploaded = false;
            try
            {
                await uploader.UploadBackupAsync(zipPath, cancellationToken);
                uploaded = true;
            }
            finally
            {
                if (uploaded && File.Exists(zipPath))
                {
                    File.Delete(zipPath);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Backup orchestration failed.");
            throw;
        }
    }
}
