namespace AutoBus;

/// <summary>
/// Pluggable inbox/idempotency boundary for consumer execution.
/// </summary>
public interface IMessageDeduplicator
{
    Task<bool> HasProcessedAsync(string key, CancellationToken cancellationToken);
    Task MarkProcessedAsync(string key, CancellationToken cancellationToken);
}
