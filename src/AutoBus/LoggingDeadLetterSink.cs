namespace AutoBus;

internal sealed class LoggingDeadLetterSink : IDeadLetterSink
{
    public Task WriteAsync(ConsumeExecutionContext context, Exception exception)
    {
        // default no-op sink with predictable behavior when logging is not configured
        return Task.CompletedTask;
    }
}
