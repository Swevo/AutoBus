using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
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
        CooldownConsumer.Attempts = 0;
        CooldownConsumer.ConsumedOrderIds.Clear();
        PartitionProbe.MaxConcurrentObserved = 0;
        PartitionProbe.InFlight = 0;
        PartitionProbe.ProcessedOrderIds.Clear();
        AlwaysFailingAdvancedConsumer.Attempts = 0;
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
    public async Task DeadLetterSink_Is_Invoked_After_Retry_Exhaustion()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDeadLetterSink, DeadLetterProbe>();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<AlwaysFailingAdvancedConsumer>();
            cfg.UseRetry(2, TimeSpan.FromMilliseconds(1));
        });

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new AdvancedOrderCreated(22)));

        Assert.Equal(3, AlwaysFailingAdvancedConsumer.Attempts);
        Assert.Single(DeadLetterProbe.Failures);
        Assert.Equal(typeof(AlwaysFailingAdvancedConsumer), DeadLetterProbe.Failures[0].ConsumerType);
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

    [Fact]
    public async Task Partitioning_Enforces_Sequential_Execution_Per_Key()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<PartitionProbe>();
            cfg.UsePartitioning((message, _) =>
                message is AdvancedOrderCreated created ? $"order:{created.OrderId}" : null);
        });

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();

        await Task.WhenAll(
            bus.PublishAsync(new AdvancedOrderCreated(42)),
            bus.PublishAsync(new AdvancedOrderCreated(42)),
            bus.PublishAsync(new AdvancedOrderCreated(42)));

        Assert.Equal(1, PartitionProbe.MaxConcurrentObserved);
        Assert.Equal(3, PartitionProbe.ProcessedOrderIds.Count);
    }

    [Fact]
    public async Task ConsumerFailureCooldown_Blocks_Then_Allows_Subsequent_Delivery()
    {
        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<CooldownConsumer>();
            cfg.UseRetry(0);
            cfg.UseConsumerFailureCooldown(TimeSpan.FromMilliseconds(80));
            cfg.EnableDeliveryTracing();
        });

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();

        await Assert.ThrowsAsync<InvalidOperationException>(() => bus.PublishAsync(new AdvancedOrderCreated(1)));
        await bus.PublishAsync(new AdvancedOrderCreated(2)); // blocked by cooldown, should be skipped
        await Task.Delay(100);
        await bus.PublishAsync(new AdvancedOrderCreated(3));

        Assert.Equal(2, CooldownConsumer.Attempts);
        Assert.Equal([3], CooldownConsumer.ConsumedOrderIds);
    }

    [Fact]
    public async Task Telemetry_Emits_Started_Succeeded_And_Duration()
    {
        var started = 0L;
        var succeeded = 0L;
        var durationMeasurements = 0;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "Swevo.AutoBus")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            if (instrument.Name == "autobus.deliveries.started")
            {
                Interlocked.Add(ref started, measurement);
            }
            else if (instrument.Name == "autobus.deliveries.succeeded")
            {
                Interlocked.Add(ref succeeded, measurement);
            }
        });
        listener.SetMeasurementEventCallback<double>((instrument, _, _, _) =>
        {
            if (instrument.Name == "autobus.deliveries.duration.ms")
            {
                Interlocked.Increment(ref durationMeasurements);
            }
        });
        listener.Start();

        var services = new ServiceCollection();
        services.AddAutoBus(cfg =>
        {
            cfg.AddConsumer<AdvancedRecordingConsumer>();
        });

        await using var provider = services.BuildServiceProvider();
        var bus = provider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new AdvancedOrderCreated(12));

        Assert.True(started >= 1);
        Assert.True(succeeded >= 1);
        Assert.True(durationMeasurements >= 1);
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

    private sealed class AlwaysFailingAdvancedConsumer : IConsumer<AdvancedOrderCreated>
    {
        public static int Attempts;

        public Task Consume(ConsumeContext<AdvancedOrderCreated> context)
        {
            Attempts++;
            throw new InvalidOperationException("always fails advanced");
        }
    }

    private sealed class CooldownConsumer : IConsumer<AdvancedOrderCreated>
    {
        public static int Attempts;
        public static readonly List<int> ConsumedOrderIds = [];

        public Task Consume(ConsumeContext<AdvancedOrderCreated> context)
        {
            Attempts++;
            if (Attempts == 1)
            {
                throw new InvalidOperationException("first failure");
            }

            ConsumedOrderIds.Add(context.Message.OrderId);
            return Task.CompletedTask;
        }
    }

    private sealed class PartitionProbe : IConsumer<AdvancedOrderCreated>
    {
        public static int InFlight;
        public static int MaxConcurrentObserved;
        public static readonly List<int> ProcessedOrderIds = [];

        public async Task Consume(ConsumeContext<AdvancedOrderCreated> context)
        {
            var concurrent = Interlocked.Increment(ref InFlight);
            if (concurrent > MaxConcurrentObserved)
            {
                MaxConcurrentObserved = concurrent;
            }

            try
            {
                await Task.Delay(15, context.CancellationToken);
                lock (ProcessedOrderIds)
                {
                    ProcessedOrderIds.Add(context.Message.OrderId);
                }
            }
            finally
            {
                Interlocked.Decrement(ref InFlight);
            }
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
