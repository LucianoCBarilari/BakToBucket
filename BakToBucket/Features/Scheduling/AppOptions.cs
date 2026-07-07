namespace BakToBucket.Features.Scheduling;

public class AppOptions
{
    public string DatabaseType { get; set; } = string.Empty;
    public string BackupHostName { get; set; } = string.Empty;
    public string BackupFolder { get; set; } = string.Empty;
    public string? BackupReadPath { get; set; }
    public int BackupIntervalHours { get; set; } 
    public ScheduleOptions Schedule { get; set; } = new();
    public List<string> IncludedDatabases { get; set; } = [];
}