using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoBus.RabbitMQ;

/// <summary>
/// DI registration for the RabbitMQ AutoBus transport. Call <b>after</b>
/// <c>services.AddAutoBus(...)</c> so this replaces the default in-memory transport.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Replaces AutoBus's default in-memory <see cref="IBusTransport"/> with
    /// <see cref="RabbitMqTransport"/>, and hosts <see cref="RabbitMqConsumerHost"/> to receive
    /// messages for any locally-registered consumers.
    /// </summary>
    public static IServiceCollection AddAutoBusRabbitMq(this IServiceCollection services, Action<RabbitMqOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<RabbitMqOptions>(configure ?? (_ => { }));
        services.TryAddSingleton<RabbitMqConnectionManager>();
        services.Replace(ServiceDescriptor.Singleton<IBusTransport, RabbitMqTransport>());
        services.AddHostedService<RabbitMqConsumerHost>();

        return services;
    }
}
