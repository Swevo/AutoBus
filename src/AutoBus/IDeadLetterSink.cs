namespace AutoBus;

/// <summary>
/// Handles messages that fail permanently after retries/circuit-breaker decisions.
/// </summary>
public interface IDeadLetterSink
{
    Task WriteAsync(ConsumeExecutionContext context, Exception exception);
}
