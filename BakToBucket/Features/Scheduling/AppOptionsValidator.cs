using BakToBucket.Features.Abstractions;
using Microsoft.Extensions.Options;

namespace BakToBucket.Features.Scheduling;

public class AppOptionsValidator(IConfiguration configuration) : IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(string? name, AppOptions options)
    {
        if (options.BackupIntervalHours < 1 || options.BackupIntervalHours > 744)
            return ValidateOptionsResult.Fail("AppOptions:BackupIntervalHours must be between 1 and 744.");

        if (options.Schedule.RunAtHour < 0 || options.Schedule.RunAtHour > 23)
            return ValidateOptionsResult.Fail("AppOptions:Schedule:RunAtHour must be between 0 and 23.");

        if (options.Schedule.RunAtMinute < 0 || options.Schedule.RunAtMinute > 59)
            return ValidateOptionsResult.Fail("AppOptions:Schedule:RunAtMinute must be between 0 and 59.");

        var connStrings = configuration.GetSection("ConnectionStrings");
        bool hasEnabledEngine = false;

        
        foreach (DatabaseEngine engine in Enum.GetValues<DatabaseEngine>())
        {            
            if (options.Engines.TryGetValue(engine, out var engineConfig))
            {
                if (engineConfig.Enabled)
                {
                    hasEnabledEngine = true;

                    if (string.IsNullOrWhiteSpace(engineConfig.EngineBackupPath))
                        return ValidateOptionsResult.Fail($"AppOptions:Engines:{engine}:EngineBackupPath is required when {engine} is enabled.");
                    
                    if (string.IsNullOrWhiteSpace(connStrings[engine.ToString()]))
                        return ValidateOptionsResult.Fail($"ConnectionStrings:{engine} is required when {engine} is enabled.");

                    if (engineConfig.IncludedDatabases == null || engineConfig.IncludedDatabases.Count == 0)
                        return ValidateOptionsResult.Fail($"AppOptions:Engines:{engine}:IncludedDatabases must contain at least one database.");
                }
            }
        }

        if (!hasEnabledEngine)
        {
            return ValidateOptionsResult.Fail("At least one database engine must be enabled in AppOptions:Engines.");
        }

        return ValidateOptionsResult.Success;
    }
}