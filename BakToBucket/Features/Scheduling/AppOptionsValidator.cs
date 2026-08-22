using Microsoft.Extensions.Configuration;
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

        if (options.SqlServer?.Enabled == true)
        {
            if (string.IsNullOrWhiteSpace(options.SqlServer.EngineBackupPath))
                return ValidateOptionsResult.Fail("AppOptions:SqlServer:EngineBackupPath is required when SqlServer is enabled.");
                
            if (string.IsNullOrWhiteSpace(connStrings["SqlServer"]))
                return ValidateOptionsResult.Fail("ConnectionStrings:SqlServer is required when SqlServer is enabled.");
                
            if (options.SqlServer.IncludedDatabases == null || options.SqlServer.IncludedDatabases.Count == 0)
                return ValidateOptionsResult.Fail("AppOptions:SqlServer:IncludedDatabases must contain at least one database.");
        }

        if (options.PostgreSql?.Enabled == true)
        {
            if (string.IsNullOrWhiteSpace(options.PostgreSql.EngineBackupPath))
                return ValidateOptionsResult.Fail("AppOptions:PostgreSql:EngineBackupPath is required when PostgreSql is enabled.");
                
            if (string.IsNullOrWhiteSpace(connStrings["PostgreSql"]))
                return ValidateOptionsResult.Fail("ConnectionStrings:PostgreSql is required when PostgreSql is enabled.");
                
            if (options.PostgreSql.IncludedDatabases == null || options.PostgreSql.IncludedDatabases.Count == 0)
                return ValidateOptionsResult.Fail("AppOptions:PostgreSql:IncludedDatabases must contain at least one database.");
        }

        if (options.SqlServer?.Enabled != true && options.PostgreSql?.Enabled != true)
        {
            return ValidateOptionsResult.Fail("At least one database engine (SqlServer or PostgreSql) must be enabled in AppOptions.");
        }

        return ValidateOptionsResult.Success;
    }
}
