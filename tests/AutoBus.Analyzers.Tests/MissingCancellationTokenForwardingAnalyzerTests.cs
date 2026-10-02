using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class MissingCancellationTokenForwardingAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Token_Is_Available_But_Not_Forwarded()
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

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new MissingCancellationTokenForwardingAnalyzer());
        Assert.Contains(diagnostics, d => d.Id == "ABUS004");
    }

    [Fact]
    public async Task Does_Not_Report_When_Token_Is_Forwarded()
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
                    await bus.PublishAsync("hello", cancellationToken);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new MissingCancellationTokenForwardingAnalyzer());
        Assert.DoesNotContain(diagnostics, d => d.Id == "ABUS004");
    }
}
