namespace AutoBus;

/// <summary>
/// Wraps a delivered message with delivery metadata for a <see cref="IConsumer{TMessage}"/>.
/// </summary>
public sealed class ConsumeContext<TMessage>(
    TMessage message,
    CancellationToken cancellationToken = default,
    Guid? correlationId = null)
    where TMessage : class
{
    /// <summary>The delivered message.</summary>
    public TMessage Message { get; } = message;

    /// <summary>Cancellation token for the current dispatch/consume operation.</summary>
    public CancellationToken CancellationToken { get; } = cancellationToken;

    /// <summary>
    /// Correlation identifier for the current delivery, when one exists (for example request/response).
    /// </summary>
    public Guid? CorrelationId { get; } = correlationId;
}
