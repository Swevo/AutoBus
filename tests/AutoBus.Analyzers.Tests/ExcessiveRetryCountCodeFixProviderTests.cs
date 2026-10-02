using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class ExcessiveRetryCountCodeFixProviderTests
{
    [Fact]
    public async Task Replaces_Retry_Count_With_10()
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
                    cfg.UseRetry(42);
                }
            }
            """;

        var fixedSource = await CodeFixTestHelper.ApplyFirstCodeFixAsync(
            source,
            new ExcessiveRetryCountAnalyzer(),
            new ExcessiveRetryCountCodeFixProvider(),
            "ABUS003");

        Assert.Contains("cfg.UseRetry(10);", fixedSource);
    }
}
