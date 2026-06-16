namespace R2SentinelBak.Features.Backup;

public interface IBackupProvider
{
    string DatabaseType { get; }
    Task TestConnectionAsync(string connectionString, CancellationToken ct);
    Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct);
}
