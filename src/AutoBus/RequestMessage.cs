namespace AutoBus;

internal sealed record RequestMessage<TRequest>(Guid RequestId, TRequest Message)
    where TRequest : class;
