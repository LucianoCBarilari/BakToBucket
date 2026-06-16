using Microsoft.Extensions.Options;

namespace R2SentinelBak.Features.CloudflareR2;

public class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.AccessKey))
            return ValidateOptionsResult.Fail("StorageOptions:AccessKey is required.");

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            return ValidateOptionsResult.Fail("StorageOptions:SecretKey is required.");

        if (string.IsNullOrWhiteSpace(options.Endpoint))
            return ValidateOptionsResult.Fail("StorageOptions:Endpoint is required.");

        if (string.IsNullOrWhiteSpace(options.BucketName))
            return ValidateOptionsResult.Fail("StorageOptions:BucketName is required.");

        return ValidateOptionsResult.Success;
    }
}
