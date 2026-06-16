using Microsoft.Extensions.Options;
using R2SentinelBak.Features.Scheduling;
using R2SentinelBak.Features.CloudflareR2;
using R2SentinelBak.Features.SqlBackup;

namespace R2SentinelBak.Infrastructure.Diagnostics;

public class StartupSanityCheck(
    IOptions<AppOptions> appOptions,
    IOptions<StorageOptions> storageOptions,
    IOptions<ConnectionStringsOptions> connOptions,
    R2ClientFactory r2ClientFactory,
    IEnumerable<IDatabasePing> databasePingers,
    ILogger<StartupSanityCheck> logger)
{
    public async Task RunAllChecksAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting pre-flight sanity checks.");

        var databaseType = appOptions.Value.DatabaseType;
        var pinger = databasePingers.FirstOrDefault(p => 
            p.DatabaseType.Equals(databaseType, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No database pinger found for database type: {databaseType}");

        var connectionString = databaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? connOptions.Value.SqlServer
            : connOptions.Value.PostgreSql;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string for {databaseType} is missing or empty.");
        }

        try
        {
            await pinger.TestConnectionAsync(connectionString, ct);
            logger.LogInformation("Database connectivity verified for {DatabaseType}.", databaseType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify database connectivity for {DatabaseType}.", databaseType);
            throw;
        }

        try
        {
            using var client = r2ClientFactory.CreateClient();
            await client.HeadBucketAsync(new Amazon.S3.Model.HeadBucketRequest 
            { 
                BucketName = storageOptions.Value.BucketName 
            }, ct);
            logger.LogInformation("Cloudflare R2 connectivity verified for bucket {Bucket}.", storageOptions.Value.BucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify Cloudflare R2 connectivity.");
            throw;
        }

        var backupFolder = appOptions.Value.BackupFolder;
        if (string.IsNullOrWhiteSpace(backupFolder))
        {
            backupFolder = Path.Combine(AppContext.BaseDirectory, "Backup");
        }

        var folderPath = Path.GetFullPath(backupFolder);
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            var testFilePath = Path.Combine(folderPath, ".write_test");
            await File.WriteAllTextAsync(testFilePath, "write_test", ct);
            File.Delete(testFilePath);
            logger.LogInformation("Local backup directory is writable: {Path}", folderPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify local directory write permissions at {Path}.", folderPath);
            throw;
        }
    }
}
