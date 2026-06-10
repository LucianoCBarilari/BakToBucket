using Amazon.Runtime;
using Amazon.S3;

namespace R2SentinelBak.Features.CloudflareR2;

public sealed class R2ClientFactory(IConfiguration configuration)
{
    public IAmazonS3 CreateClient()
    {
        var endpoint = configuration["Sentinel:R2Endpoint"];
        var accessKey = configuration["Sentinel:R2AccessKey"];
        var secretKey = configuration["Sentinel:R2SecretKey"];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new InvalidOperationException("Sentinel:R2Endpoint is required.");
        }

        if (string.IsNullOrWhiteSpace(accessKey))
        {
            throw new InvalidOperationException("Sentinel:R2AccessKey is required.");
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("Sentinel:R2SecretKey is required.");
        }

        var credentials = new BasicAWSCredentials(accessKey, secretKey);
        var config = new AmazonS3Config
        {
            ServiceURL = endpoint,
            ForcePathStyle = true,
            Timeout = TimeSpan.FromMinutes(10),
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED
        };

        return new AmazonS3Client(credentials, config);
    }
}
