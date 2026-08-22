namespace BakToBucket.Features.Abstractions;

public interface IBucketSizeChecker
{
    Task<long> GetTotalBucketSizeAsync(CancellationToken ct);
}
