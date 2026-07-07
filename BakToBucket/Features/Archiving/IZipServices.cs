namespace BakToBucket.Features.Archiving;

public interface IZipServices
{
    Task<string> CreateZipAsync(string sourcePath, string hostName, string? outputDirectory, CancellationToken cancellationToken = default);
}
