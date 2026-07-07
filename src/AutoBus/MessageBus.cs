namespace AutoBus;

internal sealed class MessageBus(IBusTransport transport, ConsumerRegistry registry) : IMessageBus
{
    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
        => transport.PublishAsync(message, typeof(TMessage), cancellationToken);

    public Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default)
        => transport.PublishAsync(message, messageType, cancellationToken);

    public Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        var consumerCount = registry.GetConsumerCount(typeof(TMessage));
        if (consumerCount != 1)
        {
            throw new InvalidOperationException(
                $"SendAsync requires exactly one registered consumer for {typeof(TMessage).Name}, but found {consumerCount}. " +
                "Use PublishAsync for fan-out to zero-or-more consumers.");
        }

        return transport.PublishAsync(message, typeof(TMessage), cancellationToken);
    }
}
