using Polly;
using Polly.Retry;

namespace AutoBus;

internal static class RetryPipelineFactory
{
    public static ResiliencePipeline Create(AutoBusOptions options)
    {
        if (options.RetryCount <= 0)
        {
            return ResiliencePipeline.Empty;
        }

        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = options.RetryCount,
                Delay = options.RetryBaseDelay,
                BackoffType = DelayBackoffType.Exponential,
            })
            .Build();
    }
}
