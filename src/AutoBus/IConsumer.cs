namespace AutoBus;

/// <summary>
/// Handles messages of type <typeparamref name="TMessage"/> delivered via <see cref="IMessageBus"/>.
/// Register implementations with <c>AutoBusConfigurator.AddConsumer&lt;T&gt;()</c>.
/// </summary>
public interface IConsumer<TMessage>
    where TMessage : class
{
    /// <summary>Handles a single delivered message.</summary>
    Task Consume(ConsumeContext<TMessage> context);
}
