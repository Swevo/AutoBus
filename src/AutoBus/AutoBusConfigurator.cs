using Microsoft.Extensions.DependencyInjection;

namespace AutoBus;

/// <summary>
/// Fluent configuration surface passed to <c>services.AddAutoBus(cfg =&gt; ...)</c>.
/// </summary>
public sealed class AutoBusConfigurator
{
    private readonly IServiceCollection _services;
    private readonly ConsumerRegistry _registry;
    private readonly AutoBusOptions _options;

    internal AutoBusConfigurator(IServiceCollection services, ConsumerRegistry registry, AutoBusOptions options)
    {
        _services = services;
        _registry = registry;
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
}
