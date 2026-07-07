using Microsoft.Extensions.Options;

namespace BakToBucket.Features.CloudflareR2;

public class RetentionOptionsValidator : IValidateOptions<RetentionOptions>
{
    public ValidateOptionsResult Validate(string? name, RetentionOptions options)
    {
        if (options.MaxBucketSizeGB < 1 || options.MaxBucketSizeGB > 5000)
            return ValidateOptionsResult.Fail("RetentionOptions:MaxBucketSizeGB must be between 1 and 5000.");

        return ValidateOptionsResult.Success;
    }
}
