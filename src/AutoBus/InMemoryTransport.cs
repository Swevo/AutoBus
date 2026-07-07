namespace AutoBus;

/// <summary>
/// Default <see cref="IBusTransport"/> that dispatches messages directly to in-process
/// consumers via <see cref="ConsumerDispatcher"/>. Suitable for modular monoliths and
/// single-process apps that don't need a broker.
/// </summary>
public sealed class InMemoryTransport(ConsumerDispatcher dispatcher) : IBusTransport
{
    /// <inheritdoc />
    public async Task PublishAsync(object message, Type messageType, CancellationToken cancellationToken)
        => await dispatcher.DispatchAsync(message, messageType, cancellationToken).ConfigureAwait(false);
}
