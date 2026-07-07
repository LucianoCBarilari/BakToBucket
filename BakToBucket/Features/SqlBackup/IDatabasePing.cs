namespace BakToBucket.Features.SqlBackup;

public interface IDatabasePing
{
    string DatabaseType { get; } 
    Task TestConnectionAsync(string connectionString, CancellationToken ct);
}
