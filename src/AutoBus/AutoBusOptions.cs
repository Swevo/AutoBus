namespace AutoBus;

/// <summary>
/// Configures retry behavior and other cross-cutting AutoBus settings.
/// Retries are implemented with a Polly resilience pipeline wrapping each consumer invocation.
/// </summary>
public sealed class AutoBusOptions
{
    /// <summary>
    /// Number of retry attempts for a failing consumer invocation, in addition to the initial
    /// attempt. Set to 0 to disable retries. Default is 3.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Base delay between retry attempts (exponential backoff). Default is 200ms.</summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);
}
