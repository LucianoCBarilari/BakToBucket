using Microsoft.Extensions.Options;
using BakToBucket.Features.SqlBackup;
using BakToBucket.Features.Scheduling;
using BakToBucket.Features.CloudflareR2;

namespace BakToBucket.Infrastructure.Diagnostics;

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

        var options = appOptions.Value;

        if (options.SqlServer?.Enabled == true)
        {
            await CheckDatabaseAsync("SqlServer", connOptions.Value.SqlServer, options.SqlServer, ct);
        }

        if (options.PostgreSql?.Enabled == true)
        {
            // Note: Postgres uses Npgsql connection string which we might not be pinging yet, but we will pass it.
            await CheckDatabaseAsync("PostgreSql", connOptions.Value.PostgreSql, options.PostgreSql, ct);
        }

        if (!options.LocalOnly)
        {
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
        }
        else
        {
            logger.LogInformation("Local-only mode enabled: skipping Cloudflare R2 connectivity check.");
        }
    }

    private async Task CheckDatabaseAsync(string databaseType, string connectionString, EngineOptions engineConfig, CancellationToken ct)
    {
        var pinger = databasePingers.FirstOrDefault(p => 
            p.DatabaseType.Equals(databaseType, StringComparison.OrdinalIgnoreCase));

        // If a pinger exists for this type, test connection
        if (pinger != null)
        {
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
        }

        var readPath = !string.IsNullOrWhiteSpace(engineConfig.LocalBackupPath) ? engineConfig.LocalBackupPath : engineConfig.EngineBackupPath;

        if (string.IsNullOrWhiteSpace(readPath))
        {
            readPath = Path.Combine(AppContext.BaseDirectory, "Backup");
        }

        var folderPath = Path.GetFullPath(readPath);
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            
            var testFilePath = Path.Combine(folderPath, $".write_test_{databaseType}");
            await File.WriteAllTextAsync(testFilePath, "write_test", ct);
            File.Delete(testFilePath);
            logger.LogInformation("Local backup directory is writable for {DatabaseType}: {Path}", databaseType, folderPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to verify local directory write permissions for {DatabaseType} at {Path}.", databaseType, folderPath);
            throw;
        }
    }
}
