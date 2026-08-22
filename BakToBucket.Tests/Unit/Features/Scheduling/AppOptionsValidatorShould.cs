using BakToBucket.Features.Abstractions;
using BakToBucket.Features.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace BakToBucket.Tests.Unit.Features.Scheduling;

public class AppOptionsValidatorShould
{
    [Fact]
    public void Fail_When_SqlServer_Is_Enabled_But_ConnectionString_Is_Missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:sqlserver", ""} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            Engines = {
                { DatabaseEngine.sqlserver, new EngineOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = ["Db1"] } }
            }
        };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ConnectionStrings:sqlserver");
    }

    [Fact]
    public void Fail_When_PostgreSql_Is_Enabled_But_ConnectionString_Is_Missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:postgresql", ""} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            Engines = {
                { DatabaseEngine.postgresql, new EngineOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = ["Db1"] } }
            }
        };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ConnectionStrings:postgresql");
    }

    [Fact]
    public void Fail_When_No_Engine_Is_Enabled()
    {
        var config = new ConfigurationBuilder().Build();
        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            Engines = {
                { DatabaseEngine.sqlserver, new EngineOptions { Enabled = false } },
                { DatabaseEngine.postgresql, new EngineOptions { Enabled = false } }
            }
        };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("At least one database engine");
    }

    [Fact]
    public void Fail_When_No_Databases_Included_For_SqlServer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:sqlserver", "Server=test"} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24, 
            Engines = {
                { DatabaseEngine.sqlserver, new EngineOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = [] } }
            }
        };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("IncludedDatabases");
    }

    [Fact]
    public void Success_When_Configuration_Is_Valid_For_SqlServer()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:sqlserver", "Server=test"} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            Engines = {
                { DatabaseEngine.sqlserver, new EngineOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = ["Db1"] } }
            }
        };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeFalse();
    }

    [Fact]
    public void Success_When_Configuration_Is_Valid_For_Both()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:sqlserver", "Server=test1"},
                {"ConnectionStrings:postgresql", "Server=test2"} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            Engines = {
                { DatabaseEngine.sqlserver, new EngineOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = ["Db1"] } },
                { DatabaseEngine.postgresql, new EngineOptions { Enabled = true, EngineBackupPath = "/backup2", IncludedDatabases = ["Db2"] } }
            }
        };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeFalse();
    }
}
