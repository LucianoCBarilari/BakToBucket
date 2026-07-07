using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BakToBucket.Features.Scheduling;

public class AppOptionsValidator(IConfiguration configuration) : IValidateOptions<AppOptions>
{
    public ValidateOptionsResult Validate(string? name, AppOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.DatabaseType))
            return ValidateOptionsResult.Fail("AppOptions:DatabaseType is required.");

        if (string.IsNullOrWhiteSpace(options.BackupFolder))
            return ValidateOptionsResult.Fail("AppOptions:BackupFolder is required.");

        if (options.BackupIntervalHours < 1 || options.BackupIntervalHours > 744)
            return ValidateOptionsResult.Fail("AppOptions:BackupIntervalHours must be between 1 and 744.");

        if (options.Schedule.RunAtHour < 0 || options.Schedule.RunAtHour > 23)
            return ValidateOptionsResult.Fail("AppOptions:Schedule:RunAtHour must be between 0 and 23.");

        if (options.Schedule.RunAtMinute < 0 || options.Schedule.RunAtMinute > 59)
            return ValidateOptionsResult.Fail("AppOptions:Schedule:RunAtMinute must be between 0 and 59.");

        var connStrings = configuration.GetSection("ConnectionStrings");

        if (options.DatabaseType.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(connStrings["SqlServer"]))
                return ValidateOptionsResult.Fail("ConnectionStrings:SqlServer is required when DatabaseType is 'SqlServer'.");
        }
        else if (options.DatabaseType.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(connStrings["PostgreSql"]))
                return ValidateOptionsResult.Fail("ConnectionStrings:PostgreSql is required when DatabaseType is 'PostgreSql'.");
        }
        else
        {
            return ValidateOptionsResult.Fail($"DatabaseType '{options.DatabaseType}' is not supported. Use 'SqlServer' or 'PostgreSql'.");
        }

        if (options.IncludedDatabases == null || options.IncludedDatabases.Count == 0)
            return ValidateOptionsResult.Fail("AppOptions:IncludedDatabases must contain at least one database.");

        return ValidateOptionsResult.Success;
    }
}
