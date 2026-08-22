namespace BakToBucket.Features.Abstractions;

public interface IDatabasePing
{
    DatabaseEngine DatabaseType { get; }
    Task TestConnectionAsync(string connectionString, CancellationToken ct);
}
