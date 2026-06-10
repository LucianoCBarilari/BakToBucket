using Amazon.S3;
using Amazon.S3.Model;
using R2SentinelBak.Infrastructure.Resilience;

namespace R2SentinelBak.Features.CloudflareR2;

public sealed class Uploader(
    R2ClientFactory clientFactory,
    PolicyRegistry policyRegistry,
    IConfiguration configuration,
    ILogger<Uploader> logger)
{
    public async Task UploadBackupAsync(string backupFilePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath))
        {
            throw new ArgumentException("Backup file path is required.", nameof(backupFilePath));
        }

        if (!File.Exists(backupFilePath))
        {
            throw new FileNotFoundException("Backup file not found.", backupFilePath);
        }

        var bucketName = configuration["Sentinel:R2BucketName"];
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            throw new InvalidOperationException("Sentinel:R2BucketName is required.");
        }

        var objectKey = Path.GetFileName(backupFilePath);

        using var client = clientFactory.CreateClient();

        await policyRegistry.UploadRetryPipeline.ExecuteAsync(
            async token =>
            {
                await using var fileStream = File.OpenRead(backupFilePath);

                var request = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectKey,
                    InputStream = fileStream,
                    AutoCloseStream = false,
                    ContentType = "application/octet-stream"
                };

                await client.PutObjectAsync(request, token).ConfigureAwait(false);
            },
            cancellationToken);

        logger.LogInformation("Uploaded backup {BackupFilePath} to R2 bucket {BucketName} as {ObjectKey}.", backupFilePath, bucketName, objectKey);
    }
}
