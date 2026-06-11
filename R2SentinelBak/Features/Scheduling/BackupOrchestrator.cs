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

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var hostName = configuration["BackupHostName"] ?? Environment.MachineName;
        var connectionString = configuration["Sentinel:DbConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString))        
            throw new InvalidOperationException("Sentinel:DbConnectionString is required.");
        
        
        var backupFolder = configuration["BackupFolder"];
        
        backupFolder = string.IsNullOrWhiteSpace(backupFolder) ? Path.Combine(AppContext.BaseDirectory, "Backup") : backupFolder;

        backupFolder = Path.GetFullPath(backupFolder);

        Directory.CreateDirectory(backupFolder);

        var databasesToBackup = configuration.GetSection("Sentinel:IncludedDatabases").Get<List<string>>() ?? [];

        if (databasesToBackup.Count == 0)
        {
            logger.LogWarning("No databases specified in Sentinel:IncludedDatabases. Skipping backup cycle.");
            return;
        }

        logger.LogInformation("Starting backup cycle for {Count} databases using folder {BackupFolder}.", databasesToBackup.Count, backupFolder);

        string zipPath = string.Empty;        
        try
        {
            await sqlBackupServices.BackupDatabasesAsync(connectionString, backupFolder,  databasesToBackup);
            
            zipPath = await zipServices.CreateZipAsync(backupFolder, hostName, "", cancellationToken: cancellationToken);
            
            logger.LogInformation("Backup folder compressed into {ZipPath}.", zipPath);

            await uploader.UploadBackupAsync(zipPath, cancellationToken);            
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

    private void CleanupLocalFiles(string zipPath, string backupFolder)
    {
        if (File.Exists(zipPath))
                File.Delete(zipPath);

        foreach (var bak in Directory.GetFiles(backupFolder, "*.bak"))
        {
            try 
            { 
                File.Delete(bak); 
            } catch (Exception ex) 
            {
                logger.LogWarning(ex, "Could not delete {File}.", bak); 
            }
        }

        logger.LogInformation("Cleaned up local backup files.");
    }
}
