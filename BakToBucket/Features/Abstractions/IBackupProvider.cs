namespace BakToBucket.Features.Abstractions;

public interface IBackupProvider
{
    DatabaseEngine DatabaseType { get; }
    Task TestConnectionAsync(string connectionString, CancellationToken ct);
    Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct);
}
