using System.Collections.Concurrent;
using System.Threading;

namespace AutoBus.Tests;

public sealed record OrderCreated(int OrderId);
public sealed record OrderCancelled(int OrderId);
public sealed record OrderStatusRequest(int OrderId, int DelayMilliseconds = 0, bool Fail = false);
public sealed record OrderStatusResponse(int OrderId, string Status);

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

public sealed class OrderStatusRequestHandler : IRequestHandler<OrderStatusRequest, OrderStatusResponse>
{
    public static readonly ConcurrentDictionary<int, Guid> CorrelationIdsByOrderId = new();

    public async Task<OrderStatusResponse> Consume(ConsumeContext<OrderStatusRequest> context)
    {
        if (context.Message.DelayMilliseconds > 0)
        {
            await Task.Delay(context.Message.DelayMilliseconds, context.CancellationToken);
        }

        if (context.Message.Fail)
        {
            throw new InvalidOperationException($"Request failed for order {context.Message.OrderId}.");
        }

        CorrelationIdsByOrderId[context.Message.OrderId] = context.CorrelationId!.Value;
        return new OrderStatusResponse(context.Message.OrderId, $"Processed-{context.Message.OrderId}");
    }
}

public sealed class FlakyOrderStatusRequestHandler : IRequestHandler<OrderStatusRequest, OrderStatusResponse>
{
    public static int Attempts;

    public Task<OrderStatusResponse> Consume(ConsumeContext<OrderStatusRequest> context)
    {
        var attempt = Interlocked.Increment(ref Attempts);
        if (attempt < 3)
        {
            throw new InvalidOperationException("Transient request handler failure.");
        }

        return Task.FromResult(new OrderStatusResponse(context.Message.OrderId, $"Recovered-{context.Message.OrderId}"));
    }
}

public sealed class NeverRespondingRequestHandler : IRequestHandler<OrderStatusRequest, OrderStatusResponse>
{
    public Task<OrderStatusResponse> Consume(ConsumeContext<OrderStatusRequest> context)
        => WaitForCancellationAsync(context.CancellationToken);

    private static async Task<OrderStatusResponse> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new InvalidOperationException("Request should have timed out before completion.");
    }
}
