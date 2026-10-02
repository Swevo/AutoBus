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

    /// <summary>Default timeout for request/response operations. Default is 30 seconds.</summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Enable jitter for retry delays. Default true.</summary>
    public bool RetryUseJitter { get; set; } = true;

    /// <summary>Controls whether dead-letter sink is invoked when retries are exhausted. Default true.</summary>
    public bool EnableDeadLettering { get; set; } = true;

    /// <summary>
    /// Optional selector for partition keys to enforce per-key sequential processing.
    /// Return null/empty to process without partitioning.
    /// </summary>
    public Func<object, Type, string?>? PartitionKeySelector { get; set; }

    /// <summary>Maximum concurrent deliveries per partition key. Default 1 when partitioning is active.</summary>
    public int MaxConcurrencyPerPartition { get; set; } = 1;

    /// <summary>
    /// When greater than zero, consumer failures can temporarily block subsequent deliveries for the same consumer.
    /// </summary>
    public TimeSpan ConsumerFailureCooldown { get; set; } = TimeSpan.Zero;

    /// <summary>Enable delivery tracing hooks. Default false (no-op sink still registered).</summary>
    public bool EnableDeliveryTracing { get; set; }
}
