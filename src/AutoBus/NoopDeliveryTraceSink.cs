namespace AutoBus;

internal sealed class NoopDeliveryTraceSink : IDeliveryTraceSink
{
    public void OnDeliveryStarted(ConsumeExecutionContext context) { }
    public void OnDeliverySucceeded(ConsumeExecutionContext context, TimeSpan duration) { }
    public void OnDeliveryFailed(ConsumeExecutionContext context, Exception exception, TimeSpan duration) { }
}
