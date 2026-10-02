using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class CooldownWithoutTracingAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Cooldown_Is_Configured_Without_Tracing()
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
                    public static IServiceCollection AddAutoBus(this IServiceCollection services, Action<AutoBusConfigurator> configure)
                    {
                        configure(new AutoBusConfigurator());
                        return services;
                    }
                }
            }

            public sealed class Consumer {}

            public sealed class Demo
            {
                public void Configure(IServiceCollection services)
                {
                    services.AddAutoBus(cfg =>
                    {
                        cfg.AddConsumer<Consumer>();
                        cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
                    });
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new CooldownWithoutTracingAnalyzer());
        Assert.Contains(diagnostics, d => d.Id == "ABUS005");
    }

    [Fact]
    public async Task Does_Not_Report_When_Cooldown_Has_Tracing()
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
                    public static IServiceCollection AddAutoBus(this IServiceCollection services, Action<AutoBusConfigurator> configure)
                    {
                        configure(new AutoBusConfigurator());
                        return services;
                    }
                }
            }

            public sealed class Consumer {}

            public sealed class Demo
            {
                public void Configure(IServiceCollection services)
                {
                    services.AddAutoBus(cfg =>
                    {
                        cfg.AddConsumer<Consumer>();
                        cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
                        cfg.EnableDeliveryTracing();
                    });
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new CooldownWithoutTracingAnalyzer());
        Assert.DoesNotContain(diagnostics, d => d.Id == "ABUS005");
    }
}
