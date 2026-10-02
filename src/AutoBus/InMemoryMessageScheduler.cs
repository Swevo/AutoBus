namespace AutoBus;

internal sealed class InMemoryMessageScheduler(IMessageBus messageBus) : IMessageScheduler
{
    public async Task<Guid> SchedulePublishAsync<TMessage>(TMessage message, TimeSpan delay, CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (delay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), "Delay must be non-negative.");
        }

        var scheduleId = Guid.NewGuid();
        if (delay > TimeSpan.Zero)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        await messageBus.PublishAsync(message, cancellationToken).ConfigureAwait(false);
        return scheduleId;
    }
}
