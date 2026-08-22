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
                {"ConnectionStrings:SqlServer", ""} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            SqlServer = new EngineOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = ["Db1"] } 
        };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ConnectionStrings:SqlServer");
    }

    [Fact]
    public void Fail_When_PostgreSql_Is_Enabled_But_ConnectionString_Is_Missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:PostgreSql", ""} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            PostgreSql = new PostgreSqlOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = ["Db1"] } 
        };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ConnectionStrings:PostgreSql");
    }

    [Fact]
    public void Fail_When_No_Engine_Is_Enabled()
    {
        var config = new ConfigurationBuilder().Build();
        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            SqlServer = new EngineOptions { Enabled = false },
            PostgreSql = new PostgreSqlOptions { Enabled = false }
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
                {"ConnectionStrings:SqlServer", "Server=test"} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24, 
            SqlServer = new EngineOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = [] } 
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
                {"ConnectionStrings:SqlServer", "Server=test"} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            SqlServer = new EngineOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = ["Db1"] } 
        };
        
        var result = validator.Validate(null, options);
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Success_When_Configuration_Is_Valid_For_PostgreSql()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:PostgreSql", "Server=test"} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { 
            BackupIntervalHours = 24,
            PostgreSql = new PostgreSqlOptions { Enabled = true, EngineBackupPath = "/backup", IncludedDatabases = ["Db1"] } 
        };
        
        var result = validator.Validate(null, options);
        result.Succeeded.Should().BeTrue();
    }
}
