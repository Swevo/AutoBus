namespace AutoBus;

internal sealed class NoopMessageDeduplicator : IMessageDeduplicator
{
    public Task<bool> HasProcessedAsync(string key, CancellationToken cancellationToken) => Task.FromResult(false);
    public Task MarkProcessedAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
}
