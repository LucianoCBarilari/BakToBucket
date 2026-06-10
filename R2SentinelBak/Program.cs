using DotNetEnv;
using R2SentinelBak.Features.Scheduling;
using R2SentinelBak.Features.CloudflareR2;
using R2SentinelBak.Features.Archiving;
using R2SentinelBak.Features.SqlBackup;
using R2SentinelBak.Infrastructure.Logging;
using R2SentinelBak.Infrastructure.Resilience;

using Serilog;

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "R2SentinelBak";
    });
    
    var runOnce = args.Any(arg => string.Equals(arg, "--run-once", StringComparison.OrdinalIgnoreCase));
    var isDevelopment = builder.Environment.IsDevelopment();

    builder.Configuration.Sources.Clear();
    builder.Configuration.SetBasePath(AppContext.BaseDirectory);

    if (isDevelopment)
    {
        Env.Load();
    }

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.AddLoggingCore();
    builder.Services.AddSingleton<PolicyRegistry>();
    builder.Services.AddSingleton<R2ClientFactory>();
    builder.Services.AddSingleton<ISqlBackupServices, SqlBackupServices>();
    builder.Services.AddTransient<Uploader>();
    builder.Services.AddTransient<IZipServices, ZipServices>();
    builder.Services.AddTransient<BackupOrchestrator>();
    builder.Services.AddSingleton(new BackupRunOptions { RunOnce = runOnce });
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during initialization.");
}
finally
{
    Log.CloseAndFlush();
}
