namespace AutoBus;

/// <summary>
/// Entry point for publishing (fan-out, zero-or-more consumers) and sending
/// (point-to-point, exactly-one consumer) messages through AutoBus.
/// </summary>
public interface IMessageBus
{
    /// <summary>
    /// Publishes <paramref name="message"/> to every registered <see cref="IConsumer{TMessage}"/>
    /// for <typeparamref name="TMessage"/>. It is not an error for zero consumers to be registered.
    /// </summary>
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;

    /// <summary>
    /// Publishes <paramref name="message"/> using its runtime <paramref name="messageType"/>.
    /// Mirrors MassTransit's <c>IPublishEndpoint.Publish(object, Type, CancellationToken)</c>
    /// signature so AutoBus can be dropped into outbox processors that were written against it.
    /// </summary>
    Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends <paramref name="message"/> to exactly one registered <see cref="IConsumer{TMessage}"/>.
    /// Throws <see cref="InvalidOperationException"/> if zero or more than one consumer is registered
    /// for <typeparamref name="TMessage"/>.
    /// </summary>
    Task SendAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class;
}
