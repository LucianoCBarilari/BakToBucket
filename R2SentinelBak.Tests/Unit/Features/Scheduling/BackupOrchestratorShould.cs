using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using R2SentinelBak.Features.Archiving;
using R2SentinelBak.Features.CloudflareR2;
using R2SentinelBak.Features.Scheduling;
using R2SentinelBak.Features.SqlBackup;
using R2SentinelBak.Infrastructure.Diagnostics;
using R2SentinelBak.Infrastructure.Resilience;
using Microsoft.Extensions.Configuration;

namespace R2SentinelBak.Tests.Unit.Features.Scheduling;

public class BackupOrchestratorShould
{
    private readonly BackupOrchestrator _orchestrator;

    public BackupOrchestratorShould()
    {
        var appOptions = Options.Create(new AppOptions { DatabaseType = "SqlServer" });
        var connOptions = Options.Create(new ConnectionStringsOptions { SqlServer = "Server=test" });
        var retentionOptions = Options.Create(new RetentionOptions { MaxBucketSizeGB = 10 });
        
        var mockProvider = new FakeBackupProvider("SqlServer");
        
        _orchestrator = new BackupOrchestrator(
            new[] { mockProvider },
            new FakeZipServices(),
            new Uploader(new R2ClientFactory(new ConfigurationBuilder().Build()), new PolicyRegistry(new LoggerFactory().CreateLogger<PolicyRegistry>()), new ConfigurationBuilder().Build(), new LoggerFactory().CreateLogger<Uploader>()),
            appOptions,
            connOptions,
            retentionOptions,
            new FakeBucketSizeChecker(),
            new LoggerFactory().CreateLogger<BackupOrchestrator>()
        );
    }

    [Fact]
    public void GetConnectionString_ReturnsCorrectString_ForSqlServer()
    {
        var options = new AppOptions { DatabaseType = "SqlServer" };
        var result = _orchestrator.GetConnectionString(options);
        result.Should().Be("Server=test");
    }

    [Fact]
    public void GetBackupProvider_ReturnsCorrectProvider()
    {
        var provider = _orchestrator.GetBackupProvider("SqlServer");
        provider.DatabaseType.Should().Be("SqlServer");
    }

    private class FakeBackupProvider(string dbType) : IBackupProvider
    {
        public string DatabaseType => dbType;
        public Task TestConnectionAsync(string connectionString, CancellationToken ct) => Task.CompletedTask;
        public Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct) => Task.CompletedTask;
    }

    private class FakeZipServices : IZipServices
    {
        public Task<string> CreateZipAsync(string folder, string host, string tag, CancellationToken cancellationToken) => Task.FromResult("test.zip");
    }

    private class FakeBucketSizeChecker : IBucketSizeChecker
    {
        public Task<long> GetTotalBucketSizeAsync(CancellationToken ct) => Task.FromResult(0L);
    }
}
