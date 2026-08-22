using BakToBucket.Features.Abstractions;
using Microsoft.Data.SqlClient;

namespace BakToBucket.Features.SqlServerBackup;

public class SqlDatabasePinger : IDatabasePing
{
    public DatabaseEngine DatabaseType => DatabaseEngine.sqlserver;

    public async Task TestConnectionAsync(string connectionString, CancellationToken ct)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        
        using var cmd = new SqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
    }
}
