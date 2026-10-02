using Microsoft.Extensions.DependencyInjection;

namespace AutoBus;

/// <summary>
/// Runtime context for middleware/dead-letter/diagnostic extensions around consumer invocation.
/// </summary>
public sealed class ConsumeExecutionContext
{
    internal ConsumeExecutionContext(
        IServiceProvider serviceProvider,
        object message,
        Type messageType,
        Type consumerType,
        Guid messageId,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        ServiceProvider = serviceProvider;
        Message = message;
        MessageType = messageType;
        ConsumerType = consumerType;
        MessageId = messageId;
        CorrelationId = correlationId;
        CancellationToken = cancellationToken;
    }

    public IServiceProvider ServiceProvider { get; }
    public object Message { get; }
    public Type MessageType { get; }
    public Type ConsumerType { get; }
    public Guid MessageId { get; }
    public Guid? CorrelationId { get; }
    public CancellationToken CancellationToken { get; }
}
