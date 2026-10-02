using AutoBus.Analyzers;
using Xunit;

namespace AutoBus.Analyzers.Tests;

public class BroadExceptionCatchInConsumerAnalyzerTests
{
    [Fact]
    public async Task Reports_When_Consumer_Catches_Exception()
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

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new BroadExceptionCatchInConsumerAnalyzer());

        Assert.Contains(diagnostics, d => d.Id == "ABUS002");
    }

    [Fact]
    public async Task Does_Not_Report_When_Specific_Exception_Is_Caught()
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
                    catch (InvalidOperationException)
                    {
                    }
                }
            }
            """;

        var diagnostics = await AnalyzerTestHelper.GetDiagnosticsAsync(source, new BroadExceptionCatchInConsumerAnalyzer());

        Assert.DoesNotContain(diagnostics, d => d.Id == "ABUS002");
    }
}
