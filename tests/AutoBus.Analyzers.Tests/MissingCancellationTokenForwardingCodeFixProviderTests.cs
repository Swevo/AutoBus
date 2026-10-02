using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class MissingCancellationTokenForwardingCodeFixProviderTests
{
    [Fact]
    public async Task Appends_CancellationToken_Argument()
    {
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;

            namespace AutoBus
            {
                public interface IMessageBus
                {
                    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);
                }
            }

            public sealed class Demo
            {
                public async Task RunAsync(AutoBus.IMessageBus bus, CancellationToken cancellationToken)
                {
                    await bus.PublishAsync("hello");
                }
            }
            """;

        var fixedSource = await CodeFixTestHelper.ApplyFirstCodeFixAsync(
            source,
            new MissingCancellationTokenForwardingAnalyzer(),
            new MissingCancellationTokenForwardingCodeFixProvider(),
            "ABUS004");

        Assert.Contains("await bus.PublishAsync(\"hello\", cancellationToken);", fixedSource);
    }
}
