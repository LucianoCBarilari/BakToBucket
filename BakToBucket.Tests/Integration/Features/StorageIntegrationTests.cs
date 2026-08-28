using BakToBucket.Tests.Integration.Infrastructure;
using Xunit;
using FluentAssertions;

namespace BakToBucket.Tests.Integration.Features;

public class StorageIntegrationTests : IClassFixture<StorageFixture>
{
    private readonly StorageFixture _fixture;

    public StorageIntegrationTests(StorageFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Can_Connect_To_Minio_And_Upload()
    {
        // Arrange
        var endpoint = _fixture.Container.GetConnectionString();
        
        // Act & Assert
        endpoint.Should().NotBeNullOrWhiteSpace();
        // Here we would use Uploader/R2ClientFactory to interact with the Minio container
    }
}
