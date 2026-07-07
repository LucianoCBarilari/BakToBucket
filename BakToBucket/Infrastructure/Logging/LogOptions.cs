namespace BakToBucket.Infrastructure.Logging;

public class LogOptions
{
    public string FolderPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MinimumLevel { get; set; } = string.Empty;
}
