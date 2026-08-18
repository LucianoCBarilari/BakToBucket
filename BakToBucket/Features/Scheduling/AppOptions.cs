namespace BakToBucket.Features.Scheduling;

public class AppOptions
{
    public string DatabaseType { get; set; } = string.Empty;
    public string BackupHostName { get; set; } = string.Empty;
    public string EngineBackupPath { get; set; } = string.Empty;
    public string? LocalBackupPath { get; set; }
    public string? ZipOutputPath { get; set; }
    public bool LocalOnly { get; set; }
    public int BackupIntervalHours { get; set; } 
    public ScheduleOptions Schedule { get; set; } = new();
    public List<string> IncludedDatabases { get; set; } = [];
}