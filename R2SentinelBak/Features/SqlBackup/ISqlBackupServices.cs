namespace R2SentinelBak.Features.SqlBackup;

public interface ISqlBackupServices
{
    public Task BackupDatabasesAsync(string connectionString, string backupFolder, string getDbs);
}
