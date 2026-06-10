using Microsoft.Data.SqlClient;

namespace R2SentinelBak.Features.SqlBackup;

public sealed class SqlBackupServices(ILogger<SqlBackupServices> logger) : ISqlBackupServices
{
    public async Task BackupDatabasesAsync(string connectionString, string backupFolder, string getDbs, List<string> dbList)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(backupFolder);
        ArgumentException.ThrowIfNullOrWhiteSpace(getDbs);       

        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync().ConfigureAwait(false);          

            foreach (var db in dbList)
            {
                var backupFile = Path.Combine(backupFolder, $"{db}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
                var backupSql = $"BACKUP DATABASE [{db}] TO DISK = '{backupFile}' WITH FORMAT, INIT;";

                using var backupCmd = new SqlCommand(backupSql, conn)
                {
                    CommandTimeout = 0
                };

                await backupCmd.ExecuteNonQueryAsync().ConfigureAwait(false);

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
