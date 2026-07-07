using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;

namespace AutoBus;

/// <summary>
/// DI registration for AutoBus.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers AutoBus: <see cref="IMessageBus"/>, the default <see cref="InMemoryTransport"/>,
    /// and any consumers registered via <paramref name="configure"/>.
    /// </summary>
    public static IServiceCollection AddAutoBus(this IServiceCollection services, Action<AutoBusConfigurator> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AutoBusOptions();
        var registry = new ConsumerRegistry();
        var configurator = new AutoBusConfigurator(services, registry, options);
        configure(configurator);

        services.TryAddSingleton(registry);
        services.TryAddSingleton(options);
        services.TryAddSingleton(sp => RetryPipelineFactory.Create(sp.GetRequiredService<AutoBusOptions>()));
        services.TryAddSingleton(sp => new ConsumerDispatcher(
            sp,
            sp.GetRequiredService<ResiliencePipeline>(),
            sp.GetService<ILogger<ConsumerDispatcher>>() ?? NullLogger<ConsumerDispatcher>.Instance));
        services.TryAddSingleton<IBusTransport, InMemoryTransport>();
        services.TryAddSingleton<IMessageBus, MessageBus>();

        return services;
    }
}
