using System.Diagnostics;
using BakToBucket.Features.Abstractions;
using BakToBucket.Features.Scheduling;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BakToBucket.Features.PostgreSqlBackup;

public class PostgreSqlBackupProvider(IOptions<AppOptions> options, ILogger<PostgreSqlBackupProvider> logger) : IBackupProvider
{
    private readonly AppOptions options = options.Value;
    public DatabaseEngine DatabaseType => DatabaseEngine.postgresql;

    public async Task TestConnectionAsync(string connectionString, CancellationToken ct)
    {
        logger.LogDebug("Testing PostgreSQL connection.");
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        using var cmd = new NpgsqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
    }

    public async Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var host = builder.Host ?? "localhost";
        var port = builder.Port > 0 ? builder.Port : 5432;
        var username = builder.Username ?? "postgres";
        var password = builder.Password;

        foreach (var db in dbList)
        {
            var isLinuxPath = backupFolder.StartsWith('/') || (!backupFolder.Contains('\\') && backupFolder.Contains('/'));
            var separator = isLinuxPath ? "/" : "\\";
            var cleanFolder = backupFolder.TrimEnd('/', '\\');

            // Instead of executing 'mkdir' inside a remote container, 
            // BakToBucket now manages the file system natively.
            Directory.CreateDirectory(cleanFolder);

            var backupFile = $"{cleanFolder}{separator}{db}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";

            var processInfo = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h {host} -p {port} -U {username} -F c -f \"{backupFile}\" {db}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (!string.IsNullOrEmpty(password))
            {
                processInfo.EnvironmentVariables["PGPASSWORD"] = password;
            }
            
            logger.LogInformation("Executing pg_dump for database {Db} at {Host}...", db, host);
            
            using var process = Process.Start(processInfo);
            if (process == null) throw new InvalidOperationException("Failed to start pg_dump process. Ensure postgresql-client is installed.");

            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                throw new Exception($"pg_dump failed (Exit Code {process.ExitCode}): {error}");
            }
            
            logger.LogInformation("Backup completed for {Db} at {BackupFile}", db, backupFile);
        }
    }
}
