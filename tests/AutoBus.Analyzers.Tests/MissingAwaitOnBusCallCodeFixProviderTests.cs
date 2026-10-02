using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class MissingAwaitOnBusCallCodeFixProviderTests
{
    [Fact]
    public async Task Adds_Await_For_FireAndForget_Bus_Call()
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

        var fixedSource = await CodeFixTestHelper.ApplyFirstCodeFixAsync(
            source,
            new MissingAwaitOnBusCallAnalyzer(),
            new MissingAwaitOnBusCallCodeFixProvider(),
            "ABUS001");

        Assert.Contains("await bus.PublishAsync(\"hello\");", fixedSource);
    }
}
