namespace AutoBus;

/// <summary>
/// Minimal saga primitives for long-running workflows.
/// </summary>
public interface ISagaStateStore<TSagaState>
    where TSagaState : class
{
    Task<TSagaState?> LoadAsync(string sagaId, CancellationToken cancellationToken);
    Task SaveAsync(string sagaId, TSagaState state, CancellationToken cancellationToken);
}

public interface ISaga<TMessage, TSagaState>
    where TMessage : class
    where TSagaState : class, new()
{
    Task HandleAsync(ConsumeContext<TMessage> context, TSagaState state);
}
