using BakToBucket.Features.Archiving;
using BakToBucket.Features.CloudflareR2;
using BakToBucket.Features.Scheduling;
using BakToBucket.Features.SqlBackup;
using BakToBucket.Infrastructure.Diagnostics;
using BakToBucket.Infrastructure.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BakToBucket.Tests.Unit.Features.Scheduling;

public class BackupOrchestratorLocalOnlyShould
{
    private readonly BackupOrchestrator _orchestrator;
    private readonly FakeZipServices _fakeZipServices;
    private readonly FakeBackupProvider _fakeBackupProvider;
    private readonly AppOptions _appOptions;

    public BackupOrchestratorLocalOnlyShould()
    {
        _appOptions = new AppOptions
        {
            DatabaseType = "SqlServer",
            LocalOnly = true,
            IncludedDatabases = ["TestDb"],
            EngineBackupPath = Path.Combine(Path.GetTempPath(), "TestEngineBackup"),
            LocalBackupPath = Path.Combine(Path.GetTempPath(), "TestLocalBackup")
        };

        var appOptionsWrapper = Options.Create(_appOptions);
        var connOptions = Options.Create(new ConnectionStringsOptions { SqlServer = "Server=localhost;Database=master;" });
        var retentionOptions = Options.Create(new RetentionOptions { MaxBucketSizeGB = 10 });
        var storageOptions = Options.Create(new StorageOptions());

        _fakeBackupProvider = new FakeBackupProvider("SqlServer");
        _fakeZipServices = new FakeZipServices();
        var r2ClientFactory = new R2ClientFactory(storageOptions);
        var policyRegistry = new PolicyRegistry(new LoggerFactory().CreateLogger<PolicyRegistry>());
        var uploader = new Uploader(r2ClientFactory, policyRegistry, storageOptions, new LoggerFactory().CreateLogger<Uploader>());

        _orchestrator = new BackupOrchestrator(
            [_fakeBackupProvider],
            _fakeZipServices,
            uploader,
            appOptionsWrapper,
            connOptions,
            retentionOptions,
            new FakeBucketSizeChecker(),
            new LoggerFactory().CreateLogger<BackupOrchestrator>()
        );
    }

    [Fact]
    public void ResolveZipOutputDirectory_Returns_CustomAbsoluteDirectory_When_Specified()
    {
        var customPath = OperatingSystem.IsWindows() ? @"C:\CustomArchive" : "/var/customarchive";
        var options = new AppOptions { ZipOutputPath = customPath, LocalOnly = true };
        var baseDir = Path.Combine(Path.GetTempPath(), "Base");

        var result = _orchestrator.ResolveZipOutputDirectory(options, baseDir);

        result.Should().Be(Path.GetFullPath(customPath));
    }

    [Fact]
    public void ResolveZipOutputDirectory_Returns_CombinedRelativeDirectory_When_RelativePath_Specified()
    {
        var options = new AppOptions { ZipOutputPath = "MyZips", LocalOnly = true };
        var baseDir = Path.Combine(Path.GetTempPath(), "Base");

        var result = _orchestrator.ResolveZipOutputDirectory(options, baseDir);

        result.Should().Be(Path.GetFullPath(Path.Combine(baseDir, "MyZips")));
    }

    [Fact]
    public void ResolveZipOutputDirectory_Returns_ArchivesSubdirectory_When_LocalOnly_And_EmptyPath()
    {
        var options = new AppOptions { ZipOutputPath = "", LocalOnly = true };
        var baseDir = Path.Combine(Path.GetTempPath(), "Base");

        var result = _orchestrator.ResolveZipOutputDirectory(options, baseDir);

        result.Should().Be(Path.GetFullPath(Path.Combine(baseDir, "Archives")));
    }

    [Fact]
    public void ResolveZipOutputDirectory_Returns_TempPath_When_Not_LocalOnly_And_EmptyPath()
    {
        var options = new AppOptions { ZipOutputPath = "", LocalOnly = false };
        var baseDir = Path.Combine(Path.GetTempPath(), "Base");

        var result = _orchestrator.ResolveZipOutputDirectory(options, baseDir);

        result.Should().Be(Path.GetTempPath());
    }

    [Fact]
    public async Task RunAsync_Preserves_ZipFile_And_Deletes_BakFiles_When_LocalOnly_Is_True()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), "BakToBucketTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempFolder);

        try
        {
            var dummyBak = Path.Combine(tempFolder, "dummy.bak");
            await File.WriteAllTextAsync(dummyBak, "dummy bak content", TestContext.Current.CancellationToken);

            var dummyZip = Path.Combine(tempFolder, "backup_result.zip");
            await File.WriteAllTextAsync(dummyZip, "dummy zip content bigger than 22 bytes header size", TestContext.Current.CancellationToken);

            _appOptions.LocalBackupPath = tempFolder;
            _appOptions.EngineBackupPath = tempFolder;
            _appOptions.LocalOnly = true;
            _fakeZipServices.ReturnedZipPath = dummyZip;

            await _orchestrator.RunAsync(TestContext.Current.CancellationToken);

            _fakeBackupProvider.BackupDatabasesCalled.Should().BeTrue();
            File.Exists(dummyZip).Should().BeTrue("Zip file must be preserved in LocalOnly mode");
            File.Exists(dummyBak).Should().BeFalse("Raw .bak file should be cleaned up after packaging");
        }
        finally
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
        }
    }

    private class FakeBackupProvider(string dbType) : IBackupProvider
    {
        public string DatabaseType => dbType;
        public bool BackupDatabasesCalled { get; private set; }
        public Task TestConnectionAsync(string connectionString, CancellationToken ct) => Task.CompletedTask;
        public Task BackupDatabasesAsync(string connectionString, string backupFolder, List<string> dbList, CancellationToken ct)
        {
            BackupDatabasesCalled = true;
            return Task.CompletedTask;
        }
    }

    private class FakeZipServices : IZipServices
    {
        public string ReturnedZipPath { get; set; } = "test.zip";

        public Task<string> CreateZipAsync(string sourcePath, string hostName, string? outputDirectory, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReturnedZipPath);
        }
    }

    private class FakeBucketSizeChecker : IBucketSizeChecker
    {
        public Task<long> GetTotalBucketSizeAsync(CancellationToken ct) => Task.FromResult(0L);
    }
}
