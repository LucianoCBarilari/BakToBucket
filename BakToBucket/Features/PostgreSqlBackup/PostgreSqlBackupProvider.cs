using System.Diagnostics;
using BakToBucket.Features.Scheduling;
using BakToBucket.Features.SqlBackup;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BakToBucket.Features.PostgreSqlBackup;

public class PostgreSqlBackupProvider(IOptions<AppOptions> options, ILogger<PostgreSqlBackupProvider> logger) : IBackupProvider
{
    private readonly AppOptions _options = options.Value;
    public string DatabaseType => "PostgreSql";

    public async Task TestConnectionAsync(string connectionString, CancellationToken ct)
    {
        // TODO: Implement Npgsql SELECT 1
        await Task.CompletedTask;
    }

    public async Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct)
    {
        var containerName = await GetPostgresContainerNameAsync(ct);
        logger.LogInformation("Contenedor PostgreSQL detectado: {ContainerName}", containerName);

        // Simple connection string parsing for username
        var username = "postgres"; // default fallback
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && (kv[0].Trim().Equals("Username", StringComparison.OrdinalIgnoreCase) || kv[0].Trim().Equals("User ID", StringComparison.OrdinalIgnoreCase)))
            {
                username = kv[1].Trim();
            }
        }

        foreach (var db in dbList)
        {
            var isLinuxPath = backupFolder.StartsWith('/') || (!backupFolder.Contains('\\') && backupFolder.Contains('/'));
            var separator = isLinuxPath ? "/" : "\\";
            var cleanFolder = backupFolder.TrimEnd('/', '\\');

            var mkdirInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {containerName} mkdir -p {cleanFolder}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var mkdirProcess = Process.Start(mkdirInfo))
            {
                if (mkdirProcess != null)
                {
                    await mkdirProcess.WaitForExitAsync(ct);
                    if (mkdirProcess.ExitCode != 0)
                    {
                        var err = await mkdirProcess.StandardError.ReadToEndAsync(ct);
                        logger.LogWarning("No se pudo asegurar la creación de la carpeta '{Folder}' en el contenedor. Error: {Error}", cleanFolder, err);
                    }
                }
            }

            var backupFile = $"{cleanFolder}{separator}{db}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";

            var processInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"exec {containerName} pg_dump -U {username} -d {db} -F c -f {backupFile}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            logger.LogInformation("Ejecutando backup en Docker para la base de datos {Db}...", db);
            
            using var process = Process.Start(processInfo);
            if (process == null) throw new InvalidOperationException("No se pudo iniciar el proceso de Docker.");

            await process.WaitForExitAsync(ct);
            
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync(ct);
                throw new Exception($"pg_dump falló en Docker (Exit Code {process.ExitCode}): {error}");
            }
            
            logger.LogInformation("Backup completado para {Db} en {BackupFile}", db, backupFile);
        }
    }

    private async Task<string> GetPostgresContainerNameAsync(CancellationToken ct)
    {
        // Auto-discovery of the postgres container
        // If the user specified it in appsettings, use it.
        if (!string.IsNullOrWhiteSpace(_options.PostgreSql?.DockerContainerName))
        {
            return _options.PostgreSql.DockerContainerName;
        }

        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = "ps --filter \"ancestor=postgres\" --format \"{{.Names}}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null) throw new InvalidOperationException("No se pudo ejecutar docker ps para auto-descubrimiento.");
        
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var containers = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        if (containers.Length == 0)
            throw new InvalidOperationException("No se encontró ningún contenedor de PostgreSQL en ejecución y no se especificó DockerContainerName en la configuración.");
        
        if (containers.Length > 1)
            throw new InvalidOperationException("Hay múltiples contenedores de PostgreSQL en ejecución. Por favor especifica 'DockerContainerName' en la configuración de PostgreSql.");

        return containers[0];
    }
}
