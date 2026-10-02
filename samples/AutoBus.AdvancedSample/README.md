# AutoBus Advanced Sample

This sample shows how to wire middleware, deduplication, dead-lettering, and deferred scheduling together.

```csharp
using Microsoft.Extensions.DependencyInjection;
using AutoBus;

var services = new ServiceCollection();

services.AddSingleton<IMessageDeduplicator, RedisMessageDeduplicator>();   // your implementation
services.AddSingleton<IDeadLetterSink, SqlDeadLetterSink>();               // your implementation
services.AddScoped<IConsumeMiddleware, AuditMiddleware>();                 // your implementation

services.AddAutoBus(cfg =>
{
    cfg.AddConsumer<OrderCreatedConsumer>();
    cfg.UseRetry(retryCount: 5, baseDelay: TimeSpan.FromMilliseconds(100));
    cfg.UseRetryJitter();
    cfg.UsePartitioning((message, _) =>
        message is OrderCreated m ? $"order:{m.OrderId}" : null);
    cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(10));
    cfg.EnableDeliveryTracing();
});

await using var provider = services.BuildServiceProvider();

var bus = provider.GetRequiredService<IMessageBus>();
var scheduler = provider.GetRequiredService<IMessageScheduler>();

await bus.PublishAsync(new OrderCreated(42));
await scheduler.SchedulePublishAsync(new OrderCreated(43), TimeSpan.FromMinutes(5));
```

## What this demonstrates

- **Retries with jitter** on consumer failure
- **Dead-letter sink invocation** when retries are exhausted
- **Deduplication boundary** before consumer invocation
- **Middleware execution** around every consumer
- **Deferred scheduling** for later publication
- **Partitioned processing** for per-order sequential handling
- **Trace snapshots** available via `InMemoryDeliveryTraceSink` when enabled
