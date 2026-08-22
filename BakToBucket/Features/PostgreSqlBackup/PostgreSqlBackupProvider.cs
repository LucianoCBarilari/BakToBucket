using System.Diagnostics;
using BakToBucket.Features.Abstractions;
using BakToBucket.Features.Scheduling;
using Microsoft.Extensions.Options;

namespace BakToBucket.Features.PostgreSqlBackup;

public class PostgreSqlBackupProvider(IOptions<AppOptions> options, ILogger<PostgreSqlBackupProvider> logger) : IBackupProvider
{
    private readonly AppOptions options = options.Value;
    public DatabaseEngine DatabaseType => DatabaseEngine.postgresql;

    public async Task TestConnectionAsync(string connectionString, CancellationToken ct)
    {
        // TODO: Implement Npgsql SELECT 1
        await Task.CompletedTask;
    }

    public async Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct)
    {
        var containerName = await GetPostgresContainerNameAsync(ct);
        logger.LogInformation("Detected PostgreSQL container: {ContainerName}", containerName);

        // Simple connection string parsing for username
        var username = "postgres"; // default fallback
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && (kv[0].Trim().Equals("Username", StringComparison.OrdinalIgnoreCase) || kv[0].Trim().Equals("User ID", StringComparison.OrdinalIgnoreCase)))
            {
                username = kv[1].Trim();
            }
        }

        foreach (var db in dbList)
        {
            var isLinuxPath = backupFolder.StartsWith('/') || (!backupFolder.Contains('\\') && backupFolder.Contains('/'));
            var separator = isLinuxPath ? "/" : "\\";
            var cleanFolder = backupFolder.TrimEnd('/', '\\');

            var mkdirInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {containerName} mkdir -p {cleanFolder}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var mkdirProcess = Process.Start(mkdirInfo))
            {
                if (mkdirProcess != null)
                {
                    await mkdirProcess.WaitForExitAsync(ct);
                    if (mkdirProcess.ExitCode != 0)
                    {
                        var err = await mkdirProcess.StandardError.ReadToEndAsync(ct);
                        logger.LogWarning("Could not ensure folder '{Folder}' creation inside container. Error: {Error}", cleanFolder, err);
                    }
                }
            }

            var backupFile = $"{cleanFolder}{separator}{db}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";

            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {containerName} pg_dump -U {username} -d {db} -F c -f {backupFile}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            logger.LogInformation("Executing backup in Docker for database {Db}...", db);
            
            using var process = Process.Start(processInfo);
            if (process == null) throw new InvalidOperationException("Failed to start Docker process.");

            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                throw new Exception($"pg_dump failed in Docker (Exit Code {process.ExitCode}): {error}");
            }
            
            logger.LogInformation("Backup completed for {Db} at {BackupFile}", db, backupFile);
        }
    }

    private async Task<string> GetPostgresContainerNameAsync(CancellationToken ct)
    {        
        if (options.Engines.TryGetValue(DatabaseEngine.postgresql, out var pgConfig) &&
           !string.IsNullOrWhiteSpace(pgConfig.DockerContainerName))
        {
            return pgConfig.DockerContainerName;
        }

        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "ps --filter \"ancestor=postgres\" --format \"{{.Names}}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null) throw new InvalidOperationException("Failed to execute docker ps for auto-discovery.");
        
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var containers = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (containers.Length == 0)
            throw new InvalidOperationException("No running PostgreSQL container found and DockerContainerName was not specified in configuration.");
        
        if (containers.Length > 1)
            throw new InvalidOperationException("Multiple PostgreSQL containers are running. Please specify 'DockerContainerName' in the PostgreSql configuration.");

        return containers[0];
    }
}
