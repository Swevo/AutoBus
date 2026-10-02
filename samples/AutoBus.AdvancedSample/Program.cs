using AutoBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddSimpleConsole(options => options.SingleLine = true));

services.AddSingleton<IMessageDeduplicator, InMemoryDeduplicator>();
services.AddSingleton<IDeadLetterSink, ConsoleDeadLetterSink>();

services.AddAutoBus(cfg =>
{
    cfg.AddConsumer<WelcomeEmailConsumer>();
    cfg.AddMiddleware<LoggingMiddleware>();
    cfg.UseRetry(retryCount: 5, baseDelay: TimeSpan.FromMilliseconds(50));
    cfg.UseRetryJitter();
    cfg.UseDeadLettering();
    cfg.UsePartitioning((message, _) =>
        message is OrderCreated created ? $"order:{created.OrderId}" : null);
    cfg.UseConsumerFailureCooldown(TimeSpan.FromSeconds(5));
    cfg.EnableDeliveryTracing();
});

await using var provider = services.BuildServiceProvider();

var bus = provider.GetRequiredService<IMessageBus>();
var scheduler = provider.GetRequiredService<IMessageScheduler>();

await bus.PublishAsync(new OrderCreated(42));
await scheduler.SchedulePublishAsync(new OrderCreated(43), TimeSpan.FromMilliseconds(25));

await Task.Delay(150);
Console.WriteLine("AutoBus advanced sample completed.");

public sealed record OrderCreated(int OrderId);

public sealed class WelcomeEmailConsumer(ILogger<WelcomeEmailConsumer> logger) : IConsumer<OrderCreated>
{
    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        logger.LogInformation("Handled order {OrderId} ({MessageId})", context.Message.OrderId, context.MessageId);
        return Task.CompletedTask;
    }
}

public sealed class LoggingMiddleware(ILogger<LoggingMiddleware> logger) : IConsumeMiddleware
{
    public async Task InvokeAsync(ConsumeExecutionContext context, Func<Task> next)
    {
        logger.LogInformation("Before {Consumer} for {MessageType}", context.ConsumerType.Name, context.MessageType.Name);
        await next();
        logger.LogInformation("After {Consumer} for {MessageType}", context.ConsumerType.Name, context.MessageType.Name);
    }
}

public sealed class ConsoleDeadLetterSink(ILogger<ConsoleDeadLetterSink> logger) : IDeadLetterSink
{
    public Task WriteAsync(ConsumeExecutionContext context, Exception exception)
    {
        logger.LogError(exception, "Dead-letter for {Consumer}/{MessageType}", context.ConsumerType.Name, context.MessageType.Name);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryDeduplicator : IMessageDeduplicator
{
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    public Task<bool> HasProcessedAsync(string key, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(_seen.Contains(key));
        }
    }

    public Task MarkProcessedAsync(string key, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _seen.Add(key);
        }

        return Task.CompletedTask;
    }
}
