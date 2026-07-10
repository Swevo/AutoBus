namespace AutoBus;

internal sealed class RequestHandlerConsumer<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> handler,
    RequestResponseRegistry responses) : IConsumer<RequestMessage<TRequest>>
    where TRequest : class
    where TResponse : class
{
    public async Task Consume(ConsumeContext<RequestMessage<TRequest>> context)
    {
        var requestMessage = context.Message;
        var response = await handler.Consume(new ConsumeContext<TRequest>(
            requestMessage.Message,
            context.CancellationToken,
            requestMessage.RequestId)).ConfigureAwait(false);

        responses.TrySetResponse(requestMessage.RequestId, response);
    }
}
