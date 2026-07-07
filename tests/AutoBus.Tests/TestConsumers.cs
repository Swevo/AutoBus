namespace AutoBus.Tests;

public sealed record OrderCreated(int OrderId);
public sealed record OrderCancelled(int OrderId);

public sealed class RecordingConsumer : IConsumer<OrderCreated>
{
    public static readonly List<int> ConsumedOrderIds = new();

    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        ConsumedOrderIds.Add(context.Message.OrderId);
        return Task.CompletedTask;
    }
}

public sealed class SecondRecordingConsumer : IConsumer<OrderCreated>
{
    public static readonly List<int> ConsumedOrderIds = new();

    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        ConsumedOrderIds.Add(context.Message.OrderId);
        return Task.CompletedTask;
    }
}

public sealed class SingleConsumerForSend : IConsumer<OrderCancelled>
{
    public static readonly List<int> ConsumedOrderIds = new();

    public Task Consume(ConsumeContext<OrderCancelled> context)
    {
        ConsumedOrderIds.Add(context.Message.OrderId);
        return Task.CompletedTask;
    }
}

public sealed class FlakyConsumer : IConsumer<OrderCreated>
{
    public static int Attempts;

    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        Attempts++;
        if (Attempts < 3)
        {
            throw new InvalidOperationException("Simulated transient failure.");
        }

        return Task.CompletedTask;
    }
}

public sealed class AlwaysFailingConsumer : IConsumer<OrderCreated>
{
    public static int Attempts;

    public Task Consume(ConsumeContext<OrderCreated> context)
    {
        Attempts++;
        throw new InvalidOperationException("Always fails.");
    }
}
