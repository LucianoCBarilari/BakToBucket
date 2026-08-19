using BakToBucket.Features.Archiving;
using BakToBucket.Features.CloudflareR2;
using BakToBucket.Features.Scheduling;
using BakToBucket.Features.SqlBackup;
using BakToBucket.Infrastructure.Diagnostics;
using BakToBucket.Infrastructure.Logging;
using BakToBucket.Infrastructure.Resilience;
using DotNetEnv;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();
try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "BakToBucket";
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

    builder.Services.AddOptions<RetentionOptions>()
        .BindConfiguration("RetentionOptions")
        .ValidateOnStart();
    builder.Services.AddSingleton<IValidateOptions<RetentionOptions>, RetentionOptionsValidator>();


    // Register Diagnostics
    builder.Services.AddSingleton<IDatabasePing, SqlDatabasePinger>();
    builder.Services.AddSingleton<IBucketSizeChecker, R2BucketSizeChecker>();
    builder.Services.AddSingleton<StartupSanityCheck>();

    builder.Services.AddSingleton<PolicyRegistry>();
    builder.Services.AddSingleton<R2ClientFactory>();
    builder.Services.AddSingleton<IBackupProvider, SqlBackupProvider>();
    builder.Services.AddSingleton<IBackupProvider, BakToBucket.Features.PostgreSqlBackup.PostgreSqlBackupProvider>();
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
