using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutoBus.Tests;

public class AdvancedFeatureTests
{
    public AdvancedFeatureTests()
    {
        AdvancedRecordingConsumer.ConsumedOrderIds.Clear();
        MiddlewareProbe.Calls.Clear();
        DedupeProbe.SeenKeys.Clear();
        DeadLetterProbe.Failures.Clear();
    }

    [Fact]
    public async Task Middleware_Is_Invoked_Around_Consumer()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<AdvancedRecordingConsumer>();
            cfg.AddMiddleware<MiddlewareProbe>();
        });

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();

        await bus.PublishAsync(new AdvancedOrderCreated(5));

        Assert.Contains("before", MiddlewareProbe.Calls);
        Assert.Contains("after", MiddlewareProbe.Calls);
    }

    [Fact]
    public async Task DeliveryFailure_Goes_To_DeadLetterSink()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDeadLetterSink, DeadLetterProbe>();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<AlwaysFailingConsumer>();
            cfg.UseRetry(0);
        });

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new OrderCreated(1)));

        Assert.Single(DeadLetterProbe.Failures);
        Assert.Equal(typeof(OrderCreated), DeadLetterProbe.Failures[0].MessageType);
    }

    [Fact]
    public async Task Deduplicator_Can_Skip_Delivery()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageDeduplicator, DedupeProbe>();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<AdvancedRecordingConsumer>();
        });

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();

        await bus.PublishAsync(new AdvancedOrderCreated(1));
        await bus.PublishAsync(new AdvancedOrderCreated(2));

        Assert.Empty(AdvancedRecordingConsumer.ConsumedOrderIds);
    }

    [Fact]
    public async Task Scheduler_Can_DeferredPublish()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg => cfg.AddConsumer<AdvancedRecordingConsumer>());
        await using var provider = services.BuildServiceProvider();

        var scheduler = provider.GetRequiredService<IMessageScheduler>();
        await scheduler.SchedulePublishAsync(new AdvancedOrderCreated(10), TimeSpan.FromMilliseconds(10));

        Assert.Equal([10], AdvancedRecordingConsumer.ConsumedOrderIds);
    }

    private sealed class MiddlewareProbe : IConsumeMiddleware
    {
        public static readonly ConcurrentQueue<string> Calls = new();

        public async Task InvokeAsync(ConsumeExecutionContext context, Func<Task> next)
        {
            Calls.Enqueue("before");
            await next();
            Calls.Enqueue("after");
        }
    }

    private sealed class DeadLetterProbe : IDeadLetterSink
    {
        public static readonly List<(Type MessageType, Type ConsumerType, Exception Exception)> Failures = [];

        public Task WriteAsync(ConsumeExecutionContext context, Exception exception)
        {
            Failures.Add((context.MessageType, context.ConsumerType, exception));
            return Task.CompletedTask;
        }
    }

    private sealed class DedupeProbe : IMessageDeduplicator
    {
        public static readonly ConcurrentDictionary<string, byte> SeenKeys = new();

        public Task<bool> HasProcessedAsync(string key, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public Task MarkProcessedAsync(string key, CancellationToken cancellationToken)
        {
            SeenKeys[key] = 1;
            return Task.CompletedTask;
        }
    }

    public sealed record AdvancedOrderCreated(int OrderId);

    public sealed class AdvancedRecordingConsumer : IConsumer<AdvancedOrderCreated>
    {
        public static readonly List<int> ConsumedOrderIds = [];

        public Task Consume(ConsumeContext<AdvancedOrderCreated> context)
        {
            ConsumedOrderIds.Add(context.Message.OrderId);
            return Task.CompletedTask;
        }
    }
}
