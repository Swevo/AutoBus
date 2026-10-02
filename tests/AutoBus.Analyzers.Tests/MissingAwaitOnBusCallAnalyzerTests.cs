using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class MissingAwaitOnBusCallAnalyzerTests
{
    [Fact]
    public async Task Reports_When_PublishAsync_Is_FireAndForget()
    {
        const string source = """
            using System.Threading.Tasks;

            namespace AutoBus
            {
                public interface IMessageBus
                {
                    Task PublishAsync<T>(T message);
                }
            }

            public sealed class Demo
            {
                public async Task RunAsync(AutoBus.IMessageBus bus)
                {
                    bus.PublishAsync("hello");
                    await Task.Delay(1);
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new MissingAwaitOnBusCallAnalyzer());

        Assert.Contains(diagnostics, d => d.Id == "ABUS001");
    }

    [Fact]
    public async Task Does_Not_Report_When_PublishAsync_Is_Awaited()
    {
        const string source = """
            using System.Threading.Tasks;

            namespace AutoBus
            {
                public interface IMessageBus
                {
                    Task PublishAsync<T>(T message);
                }
            }

            public sealed class Demo
            {
                public async Task RunAsync(AutoBus.IMessageBus bus)
                {
                    await bus.PublishAsync("hello");
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new MissingAwaitOnBusCallAnalyzer());

        Assert.DoesNotContain(diagnostics, d => d.Id == "ABUS001");
    }
}
