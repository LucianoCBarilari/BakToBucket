using Amazon.S3.Model;
using BakToBucket.Features.Abstractions;
using BakToBucket.Features.CloudflareR2;
using Microsoft.Extensions.Options;

namespace BakToBucket.Infrastructure.Diagnostics;

public class R2BucketSizeChecker(
    R2ClientFactory clientFactory,
    IOptions<StorageOptions> storageOptions) : IBucketSizeChecker
{
    public async Task<long> GetTotalBucketSizeAsync(CancellationToken ct)
    {
        using var client = clientFactory.CreateClient();
        long totalSize = 0;
        string? continuationToken = null;

        do
        {
            var request = new ListObjectsV2Request
            {
                BucketName = storageOptions.Value.BucketName,
                ContinuationToken = continuationToken
            };

            var response = await client.ListObjectsV2Async(request, ct);
            
            foreach (var obj in response.S3Objects)
            {
                totalSize += obj.Size ?? 0;
            }

            continuationToken = (response.IsTruncated ?? false) ? response.NextContinuationToken : null;
        } 
        while (continuationToken != null);

        return totalSize;
    }
}
