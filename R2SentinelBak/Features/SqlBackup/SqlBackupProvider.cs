using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace R2SentinelBak.Features.SqlBackup;

public class SqlBackupProvider(ILogger<SqlBackupProvider> logger) : IBackupProvider
{
    public string DatabaseType => "SqlServer";

    private static readonly Regex DatabaseNameRegex = new(@"^[a-zA-Z0-9_\-]+$", RegexOptions.Compiled);

    public async Task TestConnectionAsync(string connectionString, CancellationToken ct)
    {
        logger.LogDebug("Testing SQL Server connection.");
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        
        using var cmd = new SqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
    }

    public async Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFolder);

        backupFolder = Path.GetFullPath(backupFolder);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct).ConfigureAwait(false);

            foreach (var db in dbList)
            {
                ValidateDatabaseName(db);

                var backupFile = Path.Combine(
                    backupFolder,
                    $"{db}_{timestamp}.bak");

                var safeDb = EscapeSqlIdentifier(db);
                var safeBackupFile = EscapeSqlLiteral(backupFile);

                var backupSql =
                    $"BACKUP DATABASE [{safeDb}] " +
                    $"TO DISK = '{safeBackupFile}' " +
                    $"WITH FORMAT, INIT;";

                using var backupCmd = new SqlCommand(backupSql, conn)
                {
                    CommandTimeout = 3600
                };

                await backupCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                var verifySql = $"RESTORE VERIFYONLY FROM DISK = '{safeBackupFile}';";
                using var verifyCmd = new SqlCommand(verifySql, conn)
                {
                    CommandTimeout = 3600
                };
                await verifyCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

                logger.LogInformation("Backup integrity verified for {DatabaseName}.", db);
                logger.LogInformation("Backed up database {DatabaseName} to {BackupFile}.", db, backupFile);    
            }
        }
        catch (SqlException ex)
        {
            LogSqlException(ex);
            throw;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Connection issue while running SQL backup.");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while running SQL backup.");
            throw;
        }
    }

    public static void ValidateDatabaseName(string dbName)
    {
        if (string.IsNullOrWhiteSpace(dbName))
            throw new InvalidOperationException("Database name cannot be empty.");

        if (!DatabaseNameRegex.IsMatch(dbName))
            throw new InvalidOperationException($"Invalid database name: {dbName}");
    }

    public static string EscapeSqlIdentifier(string value)
    {
        return value.Replace("]", "]]");
    }

    public static string EscapeSqlLiteral(string value)
    {
        return value.Replace("'", "''");
    }

    private void LogSqlException(SqlException ex)
    {
        switch (ex.Number)
        {
            case 2:
                logger.LogError(ex, "Server not found or timeout.");
                break;

            case 18456:
                logger.LogError(ex, "Login failed for user.");
                break;

            case 945:
                logger.LogError(ex, "Database unavailable.");
                break;

            case 3201:
                logger.LogError(ex, "Backup path error.");
                break;

            case 3013:
                logger.LogError(ex, "Backup terminated unexpectedly.");
                break;

            case 1105:
                logger.LogError(ex, "Disk is full.");
                break;

            default:
                logger.LogError(ex, "SQL Error {ErrorNumber}.", ex.Number);
                break;
        }
    }
}
