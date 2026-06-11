using System.IO.Compression;

namespace R2SentinelBak.Features.Archiving;

public class ZipServices : IZipServices
{
    public Task<string> CreateZipAsync(string sourcePath,string hostName,string? outputDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
            throw new FileNotFoundException("Source path not found.", sourcePath);

        var targetDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.GetTempPath()
            : outputDirectory;

        Directory.CreateDirectory(targetDirectory);

        var zipFilePath = Path.Combine(targetDirectory, $"Backup_{hostName}_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

        if (File.Exists(sourcePath))
        {
            using var archive = ZipFile.Open(zipFilePath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), CompressionLevel.Optimal);
        }
        else
        {
            ZipFile.CreateFromDirectory(sourcePath, zipFilePath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }

        return Task.FromResult(zipFilePath);
    }
}