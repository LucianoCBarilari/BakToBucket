using Xunit;
using FluentAssertions;
using Polly;

namespace BakToBucket.Tests.Integration.Features;

public class ResilienceIntegrationTests
{
    [Fact]
    public async Task RetryPolicy_ShouldTrigger_OnTransientErrors()
    {
        // Arrange
        int executionCount = 0;
        var policy = Policy
            .Handle<HttpRequestException>()
            .RetryAsync(3);

        // Act
        var result = await Record.ExceptionAsync(() => 
            policy.ExecuteAsync(async () => 
            {
                executionCount++;
                throw new HttpRequestException("Simulated network failure");
            })
        );

        // Assert
        result.Should().BeOfType<HttpRequestException>();
        executionCount.Should().Be(4); // 1 initial + 3 retries
    }
}
