using Xunit;

namespace AutoBus.RabbitMQ.Tests;

public class RabbitMqTopologyTests
{
    private sealed record SampleMessage(int Id);

    [Fact]
    public void ExchangeNameFor_UsesFullTypeName()
    {
        var name = RabbitMqTopology.ExchangeNameFor(typeof(SampleMessage));
        Assert.Equal("AutoBus.RabbitMQ.Tests.RabbitMqTopologyTests.SampleMessage", name);
    }

    [Fact]
    public void QueueNameFor_CombinesPrefixAndTypeName()
    {
        var name = RabbitMqTopology.QueueNameFor(typeof(SampleMessage), "myservice");
        Assert.Equal("myservice.AutoBus.RabbitMQ.Tests.RabbitMqTopologyTests.SampleMessage", name);
    }

    [Fact]
    public void ExchangeNameFor_IsStable_AcrossCalls()
    {
        var first = RabbitMqTopology.ExchangeNameFor(typeof(SampleMessage));
        var second = RabbitMqTopology.ExchangeNameFor(typeof(SampleMessage));
        Assert.Equal(first, second);
    }

    [Fact]
    public void QueueNameFor_DifferentPrefixes_ProduceDifferentQueues()
    {
        var serviceA = RabbitMqTopology.QueueNameFor(typeof(SampleMessage), "service-a");
        var serviceB = RabbitMqTopology.QueueNameFor(typeof(SampleMessage), "service-b");
        Assert.NotEqual(serviceA, serviceB);
    }
}
