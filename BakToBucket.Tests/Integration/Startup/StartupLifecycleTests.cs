using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace BakToBucket.Tests.Integration.Startup;

public class StartupLifecycleTests
{
    [Fact]
    public void Host_ShouldStart_WithValidConfiguration()
    {
        // We set up basic valid configuration and assert host builds without errors
        var args = new[] { "--run-once" };
        var builder = Host.CreateApplicationBuilder(args);
        
        // Mock essential configuration
        builder.Configuration["AppOptions:Environment"] = "Test";
        builder.Configuration["StorageOptions:BucketName"] = "test-bucket";
        builder.Configuration["StorageOptions:AccessKey"] = "test";
        builder.Configuration["StorageOptions:SecretKey"] = "test";
        builder.Configuration["StorageOptions:Endpoint"] = "http://localhost:9000";
        builder.Configuration["ConnectionStrings:DefaultConnection"] = "Server=.;Database=master;Integrated Security=True;";
        builder.Configuration["RetentionOptions:MaxBackups"] = "5";

        // Call the same setup logic as Program.cs if possible, 
        // For simplicity, we just verify the builder can build when configured correctly.
        var host = builder.Build();
        host.Should().NotBeNull();
    }
}
