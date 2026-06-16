using Microsoft.Data.SqlClient;

namespace R2SentinelBak.Features.SqlBackup;

public class SqlDatabasePinger : IDatabasePing
{
    public string DatabaseType => "SqlServer";

    public async Task TestConnectionAsync(string connectionString, CancellationToken ct)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct).ConfigureAwait(false);
        
        using var cmd = new SqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
    }
}
