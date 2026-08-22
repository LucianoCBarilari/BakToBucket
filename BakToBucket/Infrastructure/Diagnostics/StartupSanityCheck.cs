using Microsoft.Extensions.Options;
using BakToBucket.Features.Scheduling;
using BakToBucket.Features.CloudflareR2;
using BakToBucket.Features.Abstractions;

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
        
        var activeEngines = options.Engines
            .Where(e => e.Value.Enabled)
            .ToList();
        
        foreach (var engine in activeEngines)
        {
            if (connOptions.Value.TryGetValue(engine.Key, out var connString))
            {
                await CheckDatabaseAsync(engine.Key, connString, engine.Value, ct);
            }
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

    private async Task CheckDatabaseAsync(DatabaseEngine databaseType, string connectionString, EngineOptions engineConfig, CancellationToken ct)
    {
        var pinger = databasePingers.FirstOrDefault(p => p.DatabaseType == databaseType);
                
        if (pinger != null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))            
                throw new InvalidOperationException($"Connection string for {databaseType} is missing or empty.");
            

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
