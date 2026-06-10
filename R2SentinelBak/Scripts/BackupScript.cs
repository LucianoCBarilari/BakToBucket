#:package Microsoft.Data.SqlClient@5.2.0
using Microsoft.Data.SqlClient;

string connectionString = @"Data Source=DEVELOPER;Initial Catalog=master;Integrated Security=True;Encrypt=False;TrustServerCertificate=True";
string backupFolder = @"C:\Backup";
string getDbs = @"SELECT name FROM sys.databases WHERE database_id > 4 AND state_desc = 'ONLINE'";

List<string> dbList = [];
try
{    
    using var conn = new SqlConnection(connectionString);
    conn.Open();   

    using (var cmd = new SqlCommand(getDbs, conn))
    using (var reader = cmd.ExecuteReader())
        while (reader.Read())
            dbList.Add(reader.GetString(0));

    foreach (var db in dbList)
    {
        string backupFile = Path.Combine(backupFolder, $"{db}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");
        string backupSql = $"BACKUP DATABASE [{db}] TO DISK = '{backupFile}' WITH FORMAT, INIT;";

        using var backupCmd = new SqlCommand(backupSql, conn);

        backupCmd.CommandTimeout = 0;
        backupCmd.ExecuteNonQuery();

        Console.WriteLine($"Backed up: {db} on {backupFile}");
    }
    
}catch (SqlException ex)
{
    switch (ex.Number)
    {
        case 2:        // Server not found / connection timeout
            Console.WriteLine($"Server not found or timeout: {ex.Message}");
            break;

        case 18456:    // Login failed
            Console.WriteLine($"Login failed for user: {ex.Message}");
            break;

        case 945:      // Database cannot be opened
            Console.WriteLine($"Database unavailable: {ex.Message}");
            break;

        case 3201:     // Cannot open backup device (bad path/permissions)
            Console.WriteLine($"Backup path error: {ex.Message}");
            break;

        case 3013:     // Backup/restore terminated abnormally
            Console.WriteLine($"Backup terminated unexpectedly: {ex.Message}");
            break;

        case 1105:     // Disk full
            Console.WriteLine($"Disk is full: {ex.Message}");
            break;

        default:
            Console.WriteLine($"SQL Error [{ex.Number}]: {ex.Message}");
            break;
    }
}
catch (InvalidOperationException ex)
{
    // Connection is closed or broken mid-operation
    Console.WriteLine($"Connection issue: {ex.Message}");
}
catch (Exception ex)
{
    // Fallback for anything else
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
