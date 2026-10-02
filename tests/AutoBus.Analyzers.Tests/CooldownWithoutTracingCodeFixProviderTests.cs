using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class CooldownWithoutTracingCodeFixProviderTests
{
    [Fact]
    public async Task Adds_EnableDeliveryTracing_After_Cooldown_Call()
    {
        const string source = """
            using System;
            namespace Microsoft.Extensions.DependencyInjection
            {
                public interface IServiceCollection {}
            }

            namespace AutoBus
            {
                public sealed class AutoBusConfigurator
                {
                    public AutoBusConfigurator AddConsumer<T>() => this;
                    public AutoBusConfigurator UseConsumerFailureCooldown(TimeSpan cooldown) => this;
                    public AutoBusConfigurator EnableDeliveryTracing() => this;
                }

                public static class Extensions
                {
                    public static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAutoBus(this Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<AutoBusConfigurator> configure)
                    {
                        configure(new AutoBusConfigurator());
                        return services;
                    }
                }

                public static class Setup
                {
                    public static void Configure(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
                    {
                        services.AddAutoBus(cfg =>
                        {
                            cfg.AddConsumer<object>();
                            cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
                        });
                    }
                }
            }
            """;

        var fixedSource = await CodeFixTestHelper.ApplyFirstCodeFixAsync(
            source,
            new CooldownWithoutTracingAnalyzer(),
            new CooldownWithoutTracingCodeFixProvider(),
            "ABUS005");

        Assert.Contains("cfg.EnableDeliveryTracing();", fixedSource);
    }
}
