using Serilog;
using Serilog.Events;

namespace BakToBucket.Infrastructure.Logging;

public static class LoggingExtensions
{
    private static readonly string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}";

    public static HostApplicationBuilder AddLoggingCore(this HostApplicationBuilder builder)
    {
        var logConfig = builder.Configuration
            .GetSection(LogConfig.SectionName)
            .Get<LogConfig>() ?? new LogConfig();

        logConfig.Validate();
        logConfig.EnsureDirectoryExists();

        var loggerConfiguration = new LoggerConfiguration()
            .MinimumLevel.Is(logConfig.MinimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                path: logConfig.GetFilePath(),
                outputTemplate: OutputTemplate,
                rollingInterval: RollingInterval.Infinite,
                retainedFileCountLimit: 1,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1));

        if (builder.Environment.IsDevelopment())
        {
            loggerConfiguration.WriteTo.Console(outputTemplate: OutputTemplate);
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);
        builder.Services.AddSingleton(logConfig);

        return builder;
    }
}
