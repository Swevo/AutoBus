using Microsoft.Extensions.DependencyInjection;

namespace AutoBus;

/// <summary>
/// Fluent configuration surface passed to <c>services.AddAutoBus(cfg =&gt; ...)</c>.
/// </summary>
public sealed class AutoBusConfigurator
{
    private readonly IServiceCollection _services;
    private readonly ConsumerRegistry _registry;
    private readonly RequestHandlerRegistry _requestHandlerRegistry;
    private readonly AutoBusOptions _options;

    internal AutoBusConfigurator(
        IServiceCollection services,
        ConsumerRegistry registry,
        RequestHandlerRegistry requestHandlerRegistry,
        AutoBusOptions options)
    {
        _services = services;
        _registry = registry;
        _requestHandlerRegistry = requestHandlerRegistry;
        _options = options;
    }

    /// <summary>
    /// Registers <typeparamref name="TConsumer"/> (scoped lifetime) for every
    /// <see cref="IConsumer{TMessage}"/> interface it implements.
    /// </summary>
    public AutoBusConfigurator AddConsumer<TConsumer>()
        where TConsumer : class
    {
        var consumerInterfaces = typeof(TConsumer).GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConsumer<>))
            .ToList();

        if (consumerInterfaces.Count == 0)
        {
            throw new InvalidOperationException(
                $"{typeof(TConsumer).Name} does not implement IConsumer<TMessage> and cannot be registered as an AutoBus consumer.");
        }

        foreach (var consumerInterface in consumerInterfaces)
        {
            _services.AddScoped(consumerInterface, typeof(TConsumer));
            var messageType = consumerInterface.GetGenericArguments()[0];
            _registry.RegisterConsumer(messageType);
        }

        return this;
    }

    /// <summary>
    /// Registers <typeparamref name="THandler"/> (scoped lifetime) for every
    /// <see cref="IRequestHandler{TRequest,TResponse}"/> interface it implements.
    /// </summary>
    public AutoBusConfigurator AddRequestHandler<THandler>()
        where THandler : class
    {
        var handlerInterfaces = typeof(THandler).GetInterfaces()
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
            .ToList();

        if (handlerInterfaces.Count == 0)
        {
            throw new InvalidOperationException(
                $"{typeof(THandler).Name} does not implement IRequestHandler<TRequest, TResponse> and cannot be registered as an AutoBus request handler.");
        }

        foreach (var handlerInterface in handlerInterfaces)
        {
            _services.AddScoped(handlerInterface, typeof(THandler));

            var genericArguments = handlerInterface.GetGenericArguments();
            var requestType = genericArguments[0];
            var responseType = genericArguments[1];

            var requestMessageType = typeof(RequestMessage<>).MakeGenericType(requestType);
            var consumerInterface = typeof(IConsumer<>).MakeGenericType(requestMessageType);
            var adapterType = typeof(RequestHandlerConsumer<,>).MakeGenericType(requestType, responseType);

            _services.AddScoped(consumerInterface, adapterType);
            _requestHandlerRegistry.RegisterHandler(requestType, responseType);
        }

        return this;
    }

    /// <summary>Overrides the default retry behavior (3 attempts, 200ms exponential base delay).</summary>
    public AutoBusConfigurator UseRetry(int retryCount, TimeSpan? baseDelay = null)
    {
        _options.RetryCount = retryCount;
        if (baseDelay.HasValue)
        {
            _options.RetryBaseDelay = baseDelay.Value;
        }

        return this;
    }

    /// <summary>Overrides the default request/response timeout (30 seconds).</summary>
    public AutoBusConfigurator UseRequestTimeout(TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive or Timeout.InfiniteTimeSpan.");
        }

        _options.RequestTimeout = timeout;
        return this;
    }

    /// <summary>Configures retry jitter usage.</summary>
    public AutoBusConfigurator UseRetryJitter(bool enabled = true)
    {
        _options.RetryUseJitter = enabled;
        return this;
    }

    /// <summary>Registers middleware that wraps each consumer invocation.</summary>
    public AutoBusConfigurator AddMiddleware<TMiddleware>() where TMiddleware : class, IConsumeMiddleware
    {
        _services.AddScoped<IConsumeMiddleware, TMiddleware>();
        return this;
    }

    /// <summary>Overrides dead-letter sink implementation.</summary>
    public AutoBusConfigurator UseDeadLetterSink<TDeadLetterSink>()
        where TDeadLetterSink : class, IDeadLetterSink
    {
        _services.AddSingleton<IDeadLetterSink, TDeadLetterSink>();
        return this;
    }

    /// <summary>Overrides message deduplicator implementation.</summary>
    public AutoBusConfigurator UseDeduplicator<TDeduplicator>()
        where TDeduplicator : class, IMessageDeduplicator
    {
        _services.AddSingleton<IMessageDeduplicator, TDeduplicator>();
        return this;
    }

    /// <summary>Enables delivery trace snapshots via the built-in in-memory sink.</summary>
    public AutoBusConfigurator EnableDeliveryTracing()
    {
        _options.EnableDeliveryTracing = true;
        _services.AddSingleton<IDeliveryTraceSink, InMemoryDeliveryTraceSink>();
        return this;
    }

    /// <summary>Configures message partitioning and per-partition concurrency.</summary>
    public AutoBusConfigurator UsePartitioning(Func<object, Type, string?> partitionKeySelector, int maxConcurrencyPerPartition = 1)
    {
        ArgumentNullException.ThrowIfNull(partitionKeySelector);
        if (maxConcurrencyPerPartition <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxConcurrencyPerPartition), "Value must be greater than zero.");
        }

        _options.PartitionKeySelector = partitionKeySelector;
        _options.MaxConcurrencyPerPartition = maxConcurrencyPerPartition;
        return this;
    }

    /// <summary>Temporarily pauses deliveries for a consumer after a failure.</summary>
    public AutoBusConfigurator UseConsumerFailureCooldown(TimeSpan cooldown)
    {
        if (cooldown < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(cooldown), "Cooldown cannot be negative.");
        }

        _options.ConsumerFailureCooldown = cooldown;
        return this;
    }

    /// <summary>Enables/disables dead-letter sink invocation when a delivery fails permanently.</summary>
    public AutoBusConfigurator UseDeadLettering(bool enabled = true)
    {
        _options.EnableDeadLettering = enabled;
        return this;
    }
}
