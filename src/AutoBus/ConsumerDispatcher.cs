using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace AutoBus;

/// <summary>
/// Resolves and invokes every registered <see cref="IConsumer{TMessage}"/> for a given runtime
/// message type, wrapping each invocation in the configured Polly retry pipeline. Used by
/// <see cref="InMemoryTransport"/> directly, and reusable by other transports (e.g. a broker
/// receive-side handler) that only know the message type at runtime.
/// </summary>
public sealed class ConsumerDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> DispatchMethods = new();
    private static readonly MethodInfo DispatchCoreDefinition = typeof(ConsumerDispatcher)
        .GetMethod(nameof(DispatchCoreAsync), BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly IServiceProvider _rootServiceProvider;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ILogger<ConsumerDispatcher> _logger;

    public ConsumerDispatcher(IServiceProvider rootServiceProvider, ResiliencePipeline retryPipeline, ILogger<ConsumerDispatcher> logger)
    {
        _rootServiceProvider = rootServiceProvider;
        _retryPipeline = retryPipeline;
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
        var context = new ConsumeContext<TMessage>(typedMessage, cancellationToken);

        foreach (var consumer in consumers)
        {
            await _retryPipeline.ExecuteAsync(
                static async (state, ct) => await state.Consumer.Consume(new ConsumeContext<TMessage>(state.Context.Message, ct)),
                (Consumer: consumer, Context: context),
                cancellationToken).ConfigureAwait(false);
        }

        return consumers.Count;
    }
}
