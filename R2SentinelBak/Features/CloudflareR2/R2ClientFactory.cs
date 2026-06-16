using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace R2SentinelBak.Features.CloudflareR2;

public sealed class R2ClientFactory(IOptions<StorageOptions> storageOptions)
{
    public IAmazonS3 CreateClient()
    {
        var options = storageOptions.Value;

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException("StorageOptions:Endpoint is required.");
        }

        if (string.IsNullOrWhiteSpace(options.AccessKey))
        {
            throw new InvalidOperationException("StorageOptions:AccessKey is required.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new InvalidOperationException("StorageOptions:SecretKey is required.");
        }

        var credentials = new BasicAWSCredentials(options.AccessKey, options.SecretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = options.Endpoint,
            ForcePathStyle = true,
            Timeout = TimeSpan.FromMinutes(10),
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED
        };

        return new AmazonS3Client(credentials, config);
    }
}
