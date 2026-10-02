namespace AutoBus;

/// <summary>
/// Schedules deferred message publication.
/// </summary>
public interface IMessageScheduler
{
    Task<Guid> SchedulePublishAsync<TMessage>(TMessage message, TimeSpan delay, CancellationToken cancellationToken = default)
        where TMessage : class;
}
