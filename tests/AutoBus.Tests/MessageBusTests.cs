using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoBus.Tests;

public class MessageBusTests
{
    public MessageBusTests()
    {
        RecordingConsumer.ConsumedOrderIds.Clear();
        SecondRecordingConsumer.ConsumedOrderIds.Clear();
        SingleConsumerForSend.ConsumedOrderIds.Clear();
        FlakyConsumer.Attempts = 0;
        AlwaysFailingConsumer.Attempts = 0;
    }

    [Fact]
    public async Task PublishAsync_Delivers_To_Single_Registered_Consumer()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg => cfg.AddConsumer<RecordingConsumer>());
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new OrderCreated(42));

        Assert.Equal([42], RecordingConsumer.ConsumedOrderIds);
    }

    [Fact]
    public async Task PublishAsync_Delivers_To_All_Registered_Consumers_FanOut()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<RecordingConsumer>();
            cfg.AddConsumer<SecondRecordingConsumer>();
        });
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new OrderCreated(7));

        Assert.Equal([7], RecordingConsumer.ConsumedOrderIds);
        Assert.Equal([7], SecondRecordingConsumer.ConsumedOrderIds);
    }

    [Fact]
    public async Task PublishAsync_WithNoConsumers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg => cfg.AddConsumer<RecordingConsumer>());
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // OrderCancelled has no consumer registered in this container.
        await bus.PublishAsync(new OrderCancelled(1));
    }

    [Fact]
    public async Task PublishAsync_NonGeneric_Overload_Resolves_By_RuntimeType()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg => cfg.AddConsumer<RecordingConsumer>());
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();
        object message = new OrderCreated(99);
        await bus.PublishAsync(message, message.GetType());

        Assert.Equal([99], RecordingConsumer.ConsumedOrderIds);
    }

    [Fact]
    public async Task SendAsync_WithExactlyOneConsumer_Delivers()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg => cfg.AddConsumer<SingleConsumerForSend>());
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();
        await bus.SendAsync(new OrderCancelled(5));

        Assert.Equal([5], SingleConsumerForSend.ConsumedOrderIds);
    }

    [Fact]
    public async Task SendAsync_WithZeroConsumers_Throws()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg => cfg.AddConsumer<RecordingConsumer>());
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.SendAsync(new OrderCancelled(1)));
    }

    [Fact]
    public async Task SendAsync_WithMultipleConsumers_Throws()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<RecordingConsumer>();
            cfg.AddConsumer<SecondRecordingConsumer>();
        });
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.SendAsync(new OrderCreated(1)));
    }

    [Fact]
    public async Task Consumer_TransientFailure_IsRetried_UntilSuccess()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<FlakyConsumer>();
            cfg.UseRetry(retryCount: 5, baseDelay: TimeSpan.FromMilliseconds(1));
        });
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new OrderCreated(1));

        Assert.Equal(3, FlakyConsumer.Attempts);
    }

    [Fact]
    public async Task Consumer_PersistentFailure_ExhaustsRetries_AndThrows()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<AlwaysFailingConsumer>();
            cfg.UseRetry(retryCount: 2, baseDelay: TimeSpan.FromMilliseconds(1));
        });
        await using var provider = services.BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new OrderCreated(1)));

        // Initial attempt + 2 retries = 3 total attempts.
        Assert.Equal(3, AlwaysFailingConsumer.Attempts);
    }

    [Fact]
    public void AddConsumer_ForTypeNotImplementingIConsumer_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddAutoBus(cfg => cfg.AddConsumer<NotAConsumer>()));
    }

    private sealed class NotAConsumer;
}
