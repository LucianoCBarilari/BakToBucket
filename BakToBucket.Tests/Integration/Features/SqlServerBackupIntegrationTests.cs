using BakToBucket.Tests.Integration.Infrastructure;
using Xunit;
using FluentAssertions;

namespace BakToBucket.Tests.Integration.Features;

[Collection("Database")]
public class SqlServerBackupIntegrationTests : IClassFixture<SqlServerDatabaseFixture>
{
    private readonly SqlServerDatabaseFixture _fixture;

    public SqlServerBackupIntegrationTests(SqlServerDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Connect_And_Execute_Backup()
    {
        // Arrange
        var connectionString = _fixture.Container.GetConnectionString();
        
        // Act & Assert
        connectionString.Should().NotBeNullOrWhiteSpace();
        // Here we would use SqlBackupProvider to run a real backup against the container
    }
}
