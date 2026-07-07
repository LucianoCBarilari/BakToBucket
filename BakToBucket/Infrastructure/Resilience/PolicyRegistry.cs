using Amazon.Runtime;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Sockets;

namespace BakToBucket.Infrastructure.Resilience;

public sealed class PolicyRegistry
{
    public ResiliencePipeline UploadRetryPipeline { get; }

    public PolicyRegistry(ILogger<PolicyRegistry> logger)
    {
        UploadRetryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(5),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder()
                    .Handle<HttpRequestException>()
                    .Handle<IOException>()
                    .Handle<SocketException>()
                    .Handle<AmazonServiceException>(IsTransientAwsError),
                OnRetry = args =>
                {
                    logger.LogWarning(args.Outcome.Exception, "Upload attempt {Attempt} failed. Retrying in {Delay}...", args.AttemptNumber + 1, args.RetryDelay);
                    return default;
                }
            })
            .Build();
    }

    public static bool IsTransientAwsError(AmazonServiceException exception)
    {
        return exception.StatusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }
}
