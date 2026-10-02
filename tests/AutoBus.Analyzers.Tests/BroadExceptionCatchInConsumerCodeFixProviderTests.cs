using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class BroadExceptionCatchInConsumerCodeFixProviderTests
{
    [Fact]
    public async Task Replaces_Exception_With_InvalidOperationException()
    {
        const string source = """
            using System;
            using System.Threading.Tasks;

            namespace AutoBus
            {
                public interface IConsumer<T>
                {
                    Task Consume(T message);
                }
            }

            public sealed class ExampleConsumer : AutoBus.IConsumer<string>
            {
                public async Task Consume(string message)
                {
                    try
                    {
                        await Task.Delay(1);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            """;

        var fixedSource = await CodeFixTestHelper.ApplyFirstCodeFixAsync(
            source,
            new BroadExceptionCatchInConsumerAnalyzer(),
            new BroadExceptionCatchInConsumerCodeFixProvider(),
            "ABUS002");

        Assert.Contains("catch (InvalidOperationException)", fixedSource);
    }
}
