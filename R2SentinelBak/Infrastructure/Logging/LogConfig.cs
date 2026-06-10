using Serilog.Events;

namespace R2SentinelBak.Infrastructure.Logging;

public class LogConfig
{
    public const string SectionName = "LogConfig";

    public string FolderPath { get; init; } = "Logs";

    public string FileName { get; init; } = "log.txt";

    public LogEventLevel MinimumLevel { get; init; } = LogEventLevel.Information;

    public string GetDirectoryPath()
    {
        Validate();

        var baseFolder = Path.IsPathRooted(FolderPath)
            ? FolderPath
            : Path.Combine(AppContext.BaseDirectory, FolderPath);

        return Path.GetFullPath(baseFolder);
    }

    public string GetFilePath() => Path.Combine(GetDirectoryPath(), FileName);

    public void EnsureDirectoryExists()
    {
        Directory.CreateDirectory(GetDirectoryPath());
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(FolderPath))
        {
            throw new InvalidOperationException("LogConfig.FolderPath cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(FileName))
        {
            throw new InvalidOperationException("LogConfig.FileName cannot be empty.");
        }

        if (FolderPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            throw new InvalidOperationException("LogConfig.FolderPath contains invalid characters.");
        }

        if (FileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException("LogConfig.FileName contains invalid characters.");
        }

        if (!string.Equals(Path.GetFileName(FileName), FileName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("LogConfig.FileName must not include directory separators.");
        }
    }
}
