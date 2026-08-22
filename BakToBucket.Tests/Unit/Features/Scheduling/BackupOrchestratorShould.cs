using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using BakToBucket.Infrastructure.Diagnostics;
using BakToBucket.Features.Scheduling;
using BakToBucket.Infrastructure.Resilience;
using BakToBucket.Features.Archiving;
using BakToBucket.Features.CloudflareR2;
using BakToBucket.Features.SqlServerBackup;
using BakToBucket.Features.Abstractions;

namespace BakToBucket.Tests.Unit.Features.Scheduling;

public class BackupOrchestratorShould
{
    private readonly BackupOrchestrator _orchestrator;

    public BackupOrchestratorShould()
    {
        var appOptions = Options.Create(new AppOptions());
        var connOptions = Options.Create(new ConnectionStringsOptions { { DatabaseEngine.sqlserver, "Server=test" } });
        var retentionOptions = Options.Create(new RetentionOptions { MaxBucketSizeGB = 10 });
        var storageOptions = Options.Create(new StorageOptions { BucketName = "test-bucket", AccessKey = "k", SecretKey = "s", Endpoint = "e" });
        
        var mockProvider = new FakeBackupProvider(DatabaseEngine.sqlserver);
        
        // Explicit instantiation to debug and resolve CS1503
        var r2ClientFactory = new R2ClientFactory(storageOptions);
        var policyRegistry = new PolicyRegistry(new LoggerFactory().CreateLogger<PolicyRegistry>());
        var uploader = new Uploader(r2ClientFactory, policyRegistry, storageOptions, new LoggerFactory().CreateLogger<Uploader>());

        _orchestrator = new BackupOrchestrator(
            new[] { mockProvider },
            new FakeZipServices(),
            uploader,
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
        var result = _orchestrator.GetConnectionString(DatabaseEngine.sqlserver);
        result.Should().Be("Server=test");
    }

    [Fact]
    public void GetBackupProvider_ReturnsCorrectProvider()
    {
        var provider = _orchestrator.GetBackupProvider(DatabaseEngine.sqlserver);
        provider.DatabaseType.Should().Be(DatabaseEngine.sqlserver);
    }

    private class FakeBackupProvider(DatabaseEngine dbType) : IBackupProvider
    {
        public DatabaseEngine DatabaseType => dbType;
        public Task TestConnectionAsync(string connectionString, CancellationToken ct) => Task.CompletedTask;
        public Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct) => Task.CompletedTask;
    }

    private class FakeZipServices : IZipServices
    {
        public Task<string> CreateZipAsync(string sourcePath, string hostName, string databaseType, string? outputDirectory, CancellationToken cancellationToken = default) 
            => Task.FromResult("test.zip");
    }

    private class FakeBucketSizeChecker : IBucketSizeChecker
    {
        public Task<long> GetTotalBucketSizeAsync(CancellationToken ct) => Task.FromResult(0L);
    }
}
