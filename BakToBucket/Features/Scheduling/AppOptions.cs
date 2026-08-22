using BakToBucket.Features.Abstractions;

namespace BakToBucket.Features.Scheduling;

public class AppOptions
{
    public string BackupHostName { get; set; } = string.Empty;
    public string? ZipOutputPath { get; set; }
    public bool LocalOnly { get; set; }
    public int BackupIntervalHours { get; set; } 
    public ScheduleOptions Schedule { get; set; } = new();
    public Dictionary<DatabaseEngine, EngineOptions> Engines { get; set; } = [];
}

public class EngineOptions
{
    public bool Enabled { get; set; }
    public string EngineBackupPath { get; set; } = string.Empty;
    public string? LocalBackupPath { get; set; }
    public List<string> IncludedDatabases { get; set; } = [];
    public string? DockerContainerName { get; set; }
}