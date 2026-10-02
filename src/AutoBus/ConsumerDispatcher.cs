using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace AutoBus;

/// <summary>
/// Resolves and invokes every registered <see cref="IConsumer{TMessage}"/> for a given runtime
/// message type. Supports retry, middleware, dead-lettering, idempotency, partitioning, health
/// cooldowns, and telemetry hooks.
/// </summary>
public sealed class ConsumerDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> DispatchMethods = new();
    private static readonly MethodInfo DispatchCoreDefinition = typeof(ConsumerDispatcher)
        .GetMethod(nameof(DispatchCoreAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly IServiceProvider _rootServiceProvider;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly IDeadLetterSink _deadLetterSink;
    private readonly IMessageDeduplicator _deduplicator;
    private readonly IDeliveryTraceSink _traceSink;
    private readonly AutoBusOptions _options;
    private readonly ConsumerHealthState _healthState;
    private readonly ILogger<ConsumerDispatcher> _logger;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _partitionLocks = new(StringComparer.Ordinal);

    public ConsumerDispatcher(
        IServiceProvider rootServiceProvider,
        ResiliencePipeline retryPipeline,
        IDeadLetterSink deadLetterSink,
        IMessageDeduplicator deduplicator,
        IDeliveryTraceSink traceSink,
        AutoBusOptions options,
        ConsumerHealthState healthState,
        ILogger<ConsumerDispatcher> logger)
    {
        _rootServiceProvider = rootServiceProvider;
        _retryPipeline = retryPipeline;
        _deadLetterSink = deadLetterSink;
        _deduplicator = deduplicator;
        _traceSink = traceSink;
        _options = options;
        _healthState = healthState;
        _logger = logger;
    }

    /// <summary>
    /// Dispatches <paramref name="message"/> to every registered consumer for
    /// <paramref name="messageType"/>. Returns the number of consumers invoked (0 if none registered).
    /// </summary>
    public Task<int> DispatchAsync(object message, Type messageType, CancellationToken cancellationToken)
    {
        var method = DispatchMethods.GetOrAdd(messageType, static t => DispatchCoreDefinition.MakeGenericMethod(t));
        return (Task<int>)method.Invoke(this, [message, cancellationToken])!;
    }

    private async Task<int> DispatchCoreAsync<TMessage>(object message, CancellationToken cancellationToken)
        where TMessage : class
    {
        using var scope = _rootServiceProvider.CreateScope();
        var consumers = scope.ServiceProvider.GetServices<IConsumer<TMessage>>().ToList();
        if (consumers.Count == 0)
        {
            _logger.LogDebug("No consumers registered for {MessageType}; message dropped.", typeof(TMessage).Name);
            return 0;
        }

        var typedMessage = (TMessage)message;
        var messageId = Guid.NewGuid();
        var partitionKey = _options.PartitionKeySelector?.Invoke(message, typeof(TMessage));
        var partitionLock = GetPartitionLock(partitionKey);

        if (partitionLock is not null)
        {
            await partitionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            foreach (var consumer in consumers)
            {
                var dedupeKey = $"{consumer.GetType().FullName}|{typeof(TMessage).FullName}|{messageId}";
                if (await _deduplicator.HasProcessedAsync(dedupeKey, cancellationToken).ConfigureAwait(false))
                {
                    _logger.LogDebug("Skipping duplicate delivery for {ConsumerType} ({MessageType}, {MessageId}).",
                        consumer.GetType().Name, typeof(TMessage).Name, messageId);
                    continue;
                }

                var healthKey = $"{consumer.GetType().FullName}|{typeof(TMessage).FullName}";
                if (_healthState.IsBlocked(healthKey, DateTimeOffset.UtcNow, out var blockedUntil))
                {
                    _logger.LogWarning(
                        "Skipping delivery for {ConsumerType} ({MessageType}) due to cooldown until {BlockedUntil}.",
                        consumer.GetType().Name,
                        typeof(TMessage).Name,
                        blockedUntil);
                    continue;
                }

                var executionContext = new ConsumeExecutionContext(
                    scope.ServiceProvider,
                    typedMessage,
                    typeof(TMessage),
                    consumer.GetType(),
                    messageId,
                    null,
                    cancellationToken);

                var startedAt = Stopwatch.StartNew();
                _traceSink.OnDeliveryStarted(executionContext);
                AutoBusTelemetry.DeliveriesStarted.Add(1);

                using var activity = AutoBusTelemetry.ActivitySource.StartActivity("autobus.consume", ActivityKind.Consumer);
                activity?.SetTag("autobus.message_type", typeof(TMessage).FullName);
                activity?.SetTag("autobus.consumer_type", consumer.GetType().FullName);
                activity?.SetTag("autobus.message_id", messageId);

                try
                {
                    await _retryPipeline.ExecuteAsync(
                        async (state, ct) =>
                        {
                            var middlewares = state.ServiceProvider.GetServices<IConsumeMiddleware>().ToList();
                            var context = new ConsumeExecutionContext(
                                state.ServiceProvider,
                                state.Message,
                                state.MessageType,
                                state.ConsumerType,
                                state.MessageId,
                                state.CorrelationId,
                                ct);

                            var consumeContext = new ConsumeContext<TMessage>(state.Message, ct, state.CorrelationId, state.MessageId);
                            await ExecuteMiddlewareChainAsync(
                                middlewares,
                                context,
                                () => state.Consumer.Consume(consumeContext)).ConfigureAwait(false);
                        },
                        (
                            Consumer: consumer,
                            Message: typedMessage,
                            MessageType: typeof(TMessage),
                            ConsumerType: consumer.GetType(),
                            ServiceProvider: scope.ServiceProvider,
                            MessageId: messageId,
                            CorrelationId: (Guid?)null),
                        cancellationToken).ConfigureAwait(false);

                    await _deduplicator.MarkProcessedAsync(dedupeKey, cancellationToken).ConfigureAwait(false);
                    _healthState.Clear(healthKey);
                    _traceSink.OnDeliverySucceeded(executionContext, startedAt.Elapsed);
                    AutoBusTelemetry.DeliveriesSucceeded.Add(1);
                    AutoBusTelemetry.DeliveryDurationMs.Record(startedAt.Elapsed.TotalMilliseconds);
                }
                catch (Exception ex)
                {
                    _traceSink.OnDeliveryFailed(executionContext, ex, startedAt.Elapsed);
                    AutoBusTelemetry.DeliveriesFailed.Add(1);
                    AutoBusTelemetry.DeliveryDurationMs.Record(startedAt.Elapsed.TotalMilliseconds);

                    if (_options.ConsumerFailureCooldown > TimeSpan.Zero)
                    {
                        _healthState.BlockFor(healthKey, _options.ConsumerFailureCooldown, DateTimeOffset.UtcNow);
                    }

                    if (_options.EnableDeadLettering)
                    {
                        await _deadLetterSink.WriteAsync(executionContext, ex).ConfigureAwait(false);
                    }

                    throw;
                }
            }
        }
        finally
        {
            partitionLock?.Release();
        }

        return consumers.Count;
    }

    private SemaphoreSlim? GetPartitionLock(string? partitionKey)
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
        {
            return null;
        }

        var maxConcurrency = _options.MaxConcurrencyPerPartition <= 0 ? 1 : _options.MaxConcurrencyPerPartition;
        return _partitionLocks.GetOrAdd(partitionKey, _ => new SemaphoreSlim(maxConcurrency, maxConcurrency));
    }

    private static Task ExecuteMiddlewareChainAsync(
        IReadOnlyList<IConsumeMiddleware> middlewares,
        ConsumeExecutionContext context,
        Func<Task> terminal)
    {
        Task Next(int index)
        {
            if (index == middlewares.Count)
            {
                return terminal();
            }

            return middlewares[index].InvokeAsync(context, () => Next(index + 1));
        }

        return Next(0);
    }
}
