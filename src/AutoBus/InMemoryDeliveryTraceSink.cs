using System.Collections.Concurrent;

namespace AutoBus;

/// <summary>
/// Simple in-memory trace sink mainly intended for diagnostics and tests.
/// </summary>
public sealed class InMemoryDeliveryTraceSink : IDeliveryTraceSink
{
    private readonly ConcurrentQueue<DeliveryTraceRecord> _records = new();

    public IReadOnlyCollection<DeliveryTraceRecord> Snapshot() => _records.ToArray();

    public void OnDeliveryStarted(ConsumeExecutionContext context)
        => _records.Enqueue(DeliveryTraceRecord.Started(context.MessageId, context.MessageType, context.ConsumerType, context.CorrelationId));

    public void OnDeliverySucceeded(ConsumeExecutionContext context, TimeSpan duration)
        => _records.Enqueue(DeliveryTraceRecord.Succeeded(context.MessageId, context.MessageType, context.ConsumerType, context.CorrelationId, duration));

    public void OnDeliveryFailed(ConsumeExecutionContext context, Exception exception, TimeSpan duration)
        => _records.Enqueue(DeliveryTraceRecord.Failed(context.MessageId, context.MessageType, context.ConsumerType, context.CorrelationId, duration, exception.GetType().FullName, exception.Message));
}

public sealed record DeliveryTraceRecord(
    string Stage,
    Guid MessageId,
    Type MessageType,
    Type ConsumerType,
    Guid? CorrelationId,
    TimeSpan? Duration,
    string? ExceptionType,
    string? ExceptionMessage,
    DateTimeOffset Timestamp)
{
    public static DeliveryTraceRecord Started(Guid messageId, Type messageType, Type consumerType, Guid? correlationId) =>
        new("started", messageId, messageType, consumerType, correlationId, null, null, null, DateTimeOffset.UtcNow);

    public static DeliveryTraceRecord Succeeded(Guid messageId, Type messageType, Type consumerType, Guid? correlationId, TimeSpan duration) =>
        new("succeeded", messageId, messageType, consumerType, correlationId, duration, null, null, DateTimeOffset.UtcNow);

    public static DeliveryTraceRecord Failed(Guid messageId, Type messageType, Type consumerType, Guid? correlationId, TimeSpan duration, string? exceptionType, string? exceptionMessage) =>
        new("failed", messageId, messageType, consumerType, correlationId, duration, exceptionType, exceptionMessage, DateTimeOffset.UtcNow);
}
