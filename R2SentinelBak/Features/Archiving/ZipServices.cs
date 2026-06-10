using System.IO.Compression;

namespace R2SentinelBak.Features.Archiving;

public interface IZipServices
{
    Task<string> CreateZipAsync(string sourcePath, string? outputDirectory = null, CancellationToken cancellationToken = default);
}

public sealed class ZipServices : IZipServices
{
    public Task<string> CreateZipAsync(string sourcePath, string? outputDirectory = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            throw new FileNotFoundException("Source path not found.", sourcePath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sourceDirectory = File.Exists(sourcePath)
            ? Path.GetDirectoryName(sourcePath) ?? AppContext.BaseDirectory
            : Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(sourcePath)) ?? AppContext.BaseDirectory;

        var targetDirectory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Path.GetTempPath()
            : outputDirectory;

        Directory.CreateDirectory(targetDirectory);

        var zipFileName = File.Exists(sourcePath)
            ? $"{Path.GetFileNameWithoutExtension(sourcePath)}.zip"
            : $"{Path.GetFileName(Path.TrimEndingDirectorySeparator(sourcePath))}.zip";

        var zipFilePath = Path.Combine(targetDirectory, zipFileName);

        if (File.Exists(zipFilePath))
        {
            File.Delete(zipFilePath);
        }

        if (File.Exists(sourcePath))
        {
            using var zipStream = new FileStream(zipFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);
            archive.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), CompressionLevel.Optimal);
        }
        else
        {
            ZipFile.CreateFromDirectory(
                sourceDirectoryName: sourcePath,
                destinationArchiveFileName: zipFilePath,
                compressionLevel: CompressionLevel.Optimal,
                includeBaseDirectory: false);
        }

        return Task.FromResult(zipFilePath);
    }
}
