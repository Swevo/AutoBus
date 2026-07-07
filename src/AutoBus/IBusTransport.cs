namespace AutoBus;

/// <summary>
/// Delivery mechanism used by <see cref="IMessageBus"/>. The default
/// <see cref="InMemoryTransport"/> dispatches directly to in-process consumers; other
/// transports (e.g. AutoBus.RabbitMQ's <c>RabbitMqTransport</c>) publish to a broker and rely
/// on a receive-side host to call back into <see cref="ConsumerDispatcher"/>.
/// </summary>
public interface IBusTransport
{
    /// <summary>Delivers <paramref name="message"/> (of runtime type <paramref name="messageType"/>).</summary>
    Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken);
}
