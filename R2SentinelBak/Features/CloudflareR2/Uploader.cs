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
    private const long PartSize = 64 * 1024 * 1024; // 64 MB

    public async Task UploadBackupAsync(string backupFilePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(backupFilePath))
            throw new ArgumentException("Backup file path is required.", nameof(backupFilePath));

        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("Backup file not found.", backupFilePath);

        var bucketName = configuration["Sentinel:R2BucketName"];
        if (string.IsNullOrWhiteSpace(bucketName))
            throw new InvalidOperationException("Sentinel:R2BucketName is required.");

        var objectKey = Path.GetFileName(backupFilePath);
        var fileSize = new FileInfo(backupFilePath).Length;

        await policyRegistry.UploadRetryPipeline.ExecuteAsync(
            async token =>
            {
                logger.LogInformation("Starting upload of {ObjectKey} ({Size} bytes) to R2...", objectKey, fileSize);

                using var client = clientFactory.CreateClient();

                if (fileSize <= PartSize)
                {
                    await PutObjectAsync(client, bucketName, objectKey, backupFilePath, token);
                }
                else
                {
                    await MultipartUploadAsync(client, bucketName, objectKey, backupFilePath, fileSize, token);
                }

                logger.LogInformation(
                    "Uploaded {BackupFilePath} to R2 bucket {BucketName} as {ObjectKey}.",
                    backupFilePath, bucketName, objectKey);
            },
            cancellationToken);
    }

    private static async Task PutObjectAsync(IAmazonS3 client, string bucket, string key, string filePath, CancellationToken token)
    {
        const int bufferSize = 1 * 1024 * 1024; // 1 MB
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = stream,
            ContentType = "application/octet-stream",
            DisablePayloadSigning = true,   // R2 doesn't support AWS chunked signing
            UseChunkEncoding = false,    // Prevents streaming-signature frames R2 rejects
        };

        await client.PutObjectAsync(request, token).ConfigureAwait(false);
    }

    private async Task MultipartUploadAsync(IAmazonS3 client, string bucket, string key, string filePath, long fileSize, CancellationToken token)
    {
        // Initiate
        var initResponse = await client.InitiateMultipartUploadAsync(new InitiateMultipartUploadRequest
        {
            BucketName = bucket,
            Key = key,
            ContentType = "application/octet-stream",
        }, token).ConfigureAwait(false);

        var uploadId = initResponse.UploadId;
        var etags = new List<PartETag>();

        try
        {
            const int bufferSize = 1 * 1024 * 1024; // 1 MB
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);

            int partNumber = 1;
            long bytesLeft = fileSize;

            while (bytesLeft > 0)
            {
                var thisPartSize = (int)Math.Min(PartSize, bytesLeft);

                logger.LogDebug("Uploading part {Part}, size {Size} bytes...", partNumber, thisPartSize);

                var partResponse = await client.UploadPartAsync(new UploadPartRequest
                {
                    BucketName = bucket,
                    Key = key,
                    UploadId = uploadId,
                    PartNumber = partNumber,
                    PartSize = thisPartSize,
                    InputStream = stream,
                    DisablePayloadSigning = true,  // Critical for R2
                    UseChunkEncoding = false, // Critical for R2
                }, token).ConfigureAwait(false);

                etags.Add(new PartETag(partNumber, partResponse.ETag));
                bytesLeft -= thisPartSize;
                partNumber++;
            }

            // Complete
            await client.CompleteMultipartUploadAsync(new CompleteMultipartUploadRequest
            {
                BucketName = bucket,
                Key = key,
                UploadId = uploadId,
                PartETags = etags,
            }, token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Multipart upload failed, aborting upload {UploadId}...", uploadId);
            await AbortMultipartUploadAsync(client, bucket, key, uploadId);
            throw;
        }
    }
    private async Task AbortMultipartUploadAsync(IAmazonS3 client, string bucket, string key, string uploadId)
    {
        await client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
        {
            BucketName = bucket,
            Key = key,
            UploadId = uploadId,
        }, CancellationToken.None).ConfigureAwait(false);
    }
}