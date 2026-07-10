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
        OrderStatusRequestHandler.CorrelationIdsByOrderId.Clear();
        FlakyOrderStatusRequestHandler.Attempts = 0;
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

    [Fact]
    public async Task GetResponseAsync_Returns_Response_From_Single_Registered_Handler()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg => cfg.AddRequestHandler<OrderStatusRequestHandler>());
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IRequestClient<OrderStatusRequest, OrderStatusResponse>>();
        var response = await client.GetResponseAsync(new OrderStatusRequest(42));

        Assert.Equal(42, response.OrderId);
        Assert.Equal("Processed-42", response.Status);
        Assert.True(OrderStatusRequestHandler.CorrelationIdsByOrderId.TryGetValue(42, out var correlationId));
        Assert.NotEqual(Guid.Empty, correlationId);
    }

    [Fact]
    public async Task GetResponseAsync_When_Handler_Does_Not_Reply_Before_Timeout_Throws_RequestTimeoutException()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddRequestHandler<NeverRespondingRequestHandler>();
            cfg.UseRequestTimeout(TimeSpan.FromMilliseconds(50));
        });
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IRequestClient<OrderStatusRequest, OrderStatusResponse>>();

        var exception = await Assert.ThrowsAsync<RequestTimeoutException>(() =>
            client.GetResponseAsync(new OrderStatusRequest(1), timeout: TimeSpan.FromMilliseconds(50)));

        Assert.Equal(typeof(OrderStatusRequest), exception.RequestType);
        Assert.Equal(typeof(OrderStatusResponse), exception.ResponseType);
        Assert.Equal(TimeSpan.FromMilliseconds(50), exception.Timeout);
    }

    [Fact]
    public async Task GetResponseAsync_Correlates_Concurrent_Requests_Correctly()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg => cfg.AddRequestHandler<OrderStatusRequestHandler>());
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IRequestClient<OrderStatusRequest, OrderStatusResponse>>();
        var requests = Enumerable.Range(1, 20)
            .Select(orderId => client.GetResponseAsync(new OrderStatusRequest(orderId, DelayMilliseconds: 5 + ((20 - orderId) % 5) * 15)))
            .ToArray();

        var responses = await Task.WhenAll(requests);

        Assert.Equal(20, responses.Length);
        foreach (var response in responses)
        {
            Assert.Equal($"Processed-{response.OrderId}", response.Status);
        }

        Assert.Equal(20, OrderStatusRequestHandler.CorrelationIdsByOrderId.Count);
        Assert.Equal(20, OrderStatusRequestHandler.CorrelationIdsByOrderId.Values.Distinct().Count());
    }

    [Fact]
    public async Task GetResponseAsync_When_Handler_Throws_Propagates_Exception()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddRequestHandler<OrderStatusRequestHandler>();
            cfg.UseRetry(retryCount: 0);
        });
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IRequestClient<OrderStatusRequest, OrderStatusResponse>>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync(new OrderStatusRequest(9, Fail: true)));

        Assert.Equal("Request failed for order 9.", exception.Message);
    }

    [Fact]
    public async Task GetResponseAsync_RequestHandler_TransientFailure_IsRetried_UntilSuccess()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddRequestHandler<FlakyOrderStatusRequestHandler>();
            cfg.UseRetry(retryCount: 5, baseDelay: TimeSpan.FromMilliseconds(1));
        });
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<IRequestClient<OrderStatusRequest, OrderStatusResponse>>();
        var response = await client.GetResponseAsync(new OrderStatusRequest(33));

        Assert.Equal(33, response.OrderId);
        Assert.Equal("Recovered-33", response.Status);
        Assert.Equal(3, FlakyOrderStatusRequestHandler.Attempts);
    }

    [Fact]
    public void AddRequestHandler_ForTypeNotImplementingIRequestHandler_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddAutoBus(cfg => cfg.AddRequestHandler<NotARequestHandler>()));
    }

    private sealed class NotAConsumer;
    private sealed class NotARequestHandler;
}
