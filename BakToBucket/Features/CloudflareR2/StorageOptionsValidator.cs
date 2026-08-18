using BakToBucket.Features.Scheduling;
using Microsoft.Extensions.Options;

namespace BakToBucket.Features.CloudflareR2;

public class StorageOptionsValidator(IOptions<AppOptions>? appOptions = null) : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        if (appOptions?.Value?.LocalOnly == true)
        {
            return ValidateOptionsResult.Success;
        }

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
