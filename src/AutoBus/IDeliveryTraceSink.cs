namespace AutoBus;

/// <summary>
/// Diagnostic hook for delivery start/completion/failure snapshots.
/// </summary>
public interface IDeliveryTraceSink
{
    void OnDeliveryStarted(ConsumeExecutionContext context);
    void OnDeliverySucceeded(ConsumeExecutionContext context, TimeSpan duration);
    void OnDeliveryFailed(ConsumeExecutionContext context, Exception exception, TimeSpan duration);
}
