using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class ExcessiveRetryCountAnalyzerTests
{
    [Fact]
    public async Task Reports_When_UseRetry_Count_Is_Too_High()
    {
        const string source = """
            namespace AutoBus
            {
                public sealed class AutoBusConfigurator
                {
                    public AutoBusConfigurator UseRetry(int retryCount) => this;
                }
            }

            public sealed class Setup
            {
                public void Configure(AutoBus.AutoBusConfigurator cfg)
                {
                    cfg.UseRetry(99);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new ExcessiveRetryCountAnalyzer());

        Assert.Contains(diagnostics, d => d.Id == "ABUS003");
    }

    [Fact]
    public async Task Does_Not_Report_When_UseRetry_Count_Is_Reasonable()
    {
        const string source = """
            namespace AutoBus
            {
                public sealed class AutoBusConfigurator
                {
                    public AutoBusConfigurator UseRetry(int retryCount) => this;
                }
            }

            public sealed class Setup
            {
                public void Configure(AutoBus.AutoBusConfigurator cfg)
                {
                    cfg.UseRetry(5);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new ExcessiveRetryCountAnalyzer());

        Assert.DoesNotContain(diagnostics, d => d.Id == "ABUS003");
    }
}
