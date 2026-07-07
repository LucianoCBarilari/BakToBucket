using FluentAssertions;
using BakToBucket.Features.CloudflareR2;

namespace BakToBucket.Tests.Unit.Features.CloudflareR2;

public class StorageOptionsValidatorShould
{
    private readonly StorageOptionsValidator _validator = new();

    [Theory]
    [InlineData(null, "secret", "endpoint", "bucket")]
    [InlineData("", "secret", "endpoint", "bucket")]
    [InlineData("   ", "secret", "endpoint", "bucket")]
    public void Fail_When_AccessKey_Is_Missing_Or_Empty(string? key, string secret, string endpoint, string bucket)
    {
        var options = new StorageOptions { AccessKey = key!, SecretKey = secret, Endpoint = endpoint, BucketName = bucket };
        var result = _validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("AccessKey");
    }

    [Theory]
    [InlineData("key", null, "endpoint", "bucket")]
    [InlineData("key", "", "endpoint", "bucket")]
    [InlineData("key", "   ", "endpoint", "bucket")]
    public void Fail_When_SecretKey_Is_Missing_Or_Empty(string key, string? secret, string endpoint, string bucket)
    {
        var options = new StorageOptions { AccessKey = key, SecretKey = secret!, Endpoint = endpoint, BucketName = bucket };
        var result = _validator.Validate(null, options);
        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("SecretKey");
    }

    [Fact]
    public void Success_When_All_Fields_Are_Provided()
    {
        var options = new StorageOptions { AccessKey = "key", SecretKey = "secret", Endpoint = "endpoint", BucketName = "bucket" };
        var result = _validator.Validate(null, options);
        result.Succeeded.Should().BeTrue();
    }
}
