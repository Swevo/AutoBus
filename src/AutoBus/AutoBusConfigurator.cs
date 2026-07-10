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
}
