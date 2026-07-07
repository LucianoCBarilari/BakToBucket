using BakToBucket.Features.Scheduling;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace BakToBucket.Tests.Unit.Features.Scheduling;

public class AppOptionsValidatorShould
{
    [Fact]
    public void Fail_When_DatabaseType_Is_SqlServer_And_ConnectionString_Is_Missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:SqlServer", ""} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { DatabaseType = "SqlServer", BackupFolder = "/backup", BackupIntervalHours = 24 };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ConnectionStrings:SqlServer");
    }

    [Fact]
    public void Fail_When_DatabaseType_Is_PostgreSql_And_ConnectionString_Is_Missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:PostgreSql", ""} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { DatabaseType = "PostgreSql", BackupFolder = "/backup", BackupIntervalHours = 24 };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("ConnectionStrings:PostgreSql");
    }

    [Fact]
    public void Fail_When_DatabaseType_Is_Unsupported()
    {
        var config = new ConfigurationBuilder().Build();
        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { DatabaseType = "Oracle", BackupFolder = "/backup", BackupIntervalHours = 24 };
        
        var result = validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("not supported");
    }

    [Fact]
    public void Fail_When_No_Databases_Included()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { 
                {"ConnectionStrings:SqlServer", "Server=test"} 
            }!)
            .Build();

        var validator = new AppOptionsValidator(config);
        var options = new AppOptions { DatabaseType = "SqlServer", BackupFolder = "/backup", BackupIntervalHours = 24, IncludedDatabases = [] };
        
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
            DatabaseType = "SqlServer", 
            BackupFolder = "/backup", 
            BackupIntervalHours = 24,
            IncludedDatabases = ["Db1"] 
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
            DatabaseType = "PostgreSql", 
            BackupFolder = "/backup", 
            BackupIntervalHours = 24,
            IncludedDatabases = ["Db1"] 
        };
        
        var result = validator.Validate(null, options);
        result.Succeeded.Should().BeTrue();
    }
}
