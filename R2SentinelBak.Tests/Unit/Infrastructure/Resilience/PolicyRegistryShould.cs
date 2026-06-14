using Amazon.Runtime;
using FluentAssertions;
using R2SentinelBak.Infrastructure.Resilience;
using System.Net;
using Xunit;

namespace R2SentinelBak.Tests.Unit.Infrastructure.Resilience;

public class PolicyRegistryShould
{
    [Theory]
    [InlineData(HttpStatusCode.RequestTimeout)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public void IdentifyTransientAwsErrors(HttpStatusCode statusCode)
    {
        // Arrange
        var exception = new AmazonServiceException("Transient error")
        {
            StatusCode = statusCode
        };

        // Act
        var result = PolicyRegistry.IsTransientAwsError(exception);

        // Assert
        result.Should().BeTrue($"Status code {statusCode} should be treated as transient");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadRequest)]
    public void IdentifyNonTransientAwsErrors(HttpStatusCode statusCode)
    {
        // Arrange
        var exception = new AmazonServiceException("Permanent error")
        {
            StatusCode = statusCode
        };

        // Act
        var result = PolicyRegistry.IsTransientAwsError(exception);

        // Assert
        result.Should().BeFalse($"Status code {statusCode} should not be treated as transient");
    }
}
