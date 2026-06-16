using DotNetEnv;
using Microsoft.Extensions.Options;
using R2SentinelBak.Features.Archiving;
using R2SentinelBak.Features.CloudflareR2;
using R2SentinelBak.Features.Scheduling;
using R2SentinelBak.Features.SqlBackup;
using R2SentinelBak.Infrastructure.Diagnostics;
using R2SentinelBak.Infrastructure.Logging;
using R2SentinelBak.Infrastructure.Resilience;
using Serilog;

Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();
try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "R2SentinelBak";
    });
    builder.Services.AddSystemd();

    var isDevelopment = builder.Environment.IsDevelopment();
    var runOnce = args.Any(arg => string.Equals(arg, "--run-once", StringComparison.OrdinalIgnoreCase));    

    if (isDevelopment)
    {
        Env.Load();
    }  

    builder.AddLoggingCore();

    //IOptions pattern for configuration
    builder.Services.AddOptions<AppOptions>()
        .BindConfiguration("AppOptions")
        .ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<AppOptions>, AppOptionsValidator>();

    builder.Services.AddOptions<StorageOptions>()
        .BindConfiguration("StorageOptions")
        .ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();

    builder.Services.AddOptions<ConnectionStringsOptions>()
        .BindConfiguration("ConnectionStrings")
        .ValidateOnStart();


    // Register Diagnostics
    builder.Services.AddSingleton<IDatabasePing, SqlDatabasePinger>();
    builder.Services.AddSingleton<StartupSanityCheck>();

    builder.Services.AddSingleton<PolicyRegistry>();
    builder.Services.AddSingleton<R2ClientFactory>();
    builder.Services.AddSingleton<ISqlBackupServices, SqlBackupServices>();
    builder.Services.AddTransient<Uploader>();
    builder.Services.AddTransient<IZipServices, ZipServices>();
    builder.Services.AddTransient<BackupOrchestrator>();
    builder.Services.AddSingleton(new BackupRunOptions(runOnce));
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();

    // Run Sanity Checks
    using (var scope = host.Services.CreateScope())
    {
        var sanityCheck = scope.ServiceProvider.GetRequiredService<StartupSanityCheck>();
        await sanityCheck.RunAllChecksAsync(CancellationToken.None);
    }

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
