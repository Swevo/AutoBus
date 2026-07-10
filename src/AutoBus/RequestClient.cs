namespace AutoBus;

internal sealed class RequestClient<TRequest, TResponse>(
    ConsumerDispatcher dispatcher,
    RequestHandlerRegistry handlerRegistry,
    RequestResponseRegistry responses,
    AutoBusOptions options) : IRequestClient<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    public async Task<TResponse> GetResponseAsync(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var handlerCount = handlerRegistry.GetHandlerCount(typeof(TRequest), typeof(TResponse));
        if (handlerCount != 1)
        {
            throw new InvalidOperationException(
                $"GetResponseAsync requires exactly one registered request handler for {typeof(TRequest).Name} -> {typeof(TResponse).Name}, but found {handlerCount}.");
        }

        var effectiveTimeout = timeout ?? options.RequestTimeout;
        if (effectiveTimeout <= TimeSpan.Zero && effectiveTimeout != Timeout.InfiniteTimeSpan)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive or Timeout.InfiniteTimeSpan.");
        }

        var requestId = Guid.NewGuid();
        var responseCompletion = responses.Register(requestId);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var dispatchTask = dispatcher.DispatchAsync(
            new RequestMessage<TRequest>(requestId, request),
            typeof(RequestMessage<TRequest>),
            linkedCancellation.Token);

        try
        {
            var completedTask = effectiveTimeout == Timeout.InfiniteTimeSpan
                ? await Task.WhenAny(responseCompletion.Task, dispatchTask).WaitAsync(cancellationToken).ConfigureAwait(false)
                : await Task.WhenAny(responseCompletion.Task, dispatchTask).WaitAsync(effectiveTimeout, cancellationToken).ConfigureAwait(false);

            if (completedTask == responseCompletion.Task)
            {
                await dispatchTask.ConfigureAwait(false);
                return (TResponse)(await responseCompletion.Task.ConfigureAwait(false))!;
            }

            await dispatchTask.ConfigureAwait(false);
            if (responseCompletion.Task.IsCompletedSuccessfully)
            {
                return (TResponse)(await responseCompletion.Task.ConfigureAwait(false))!;
            }

            throw new InvalidOperationException(
                $"Request handler for {typeof(TRequest).Name} completed without producing a {typeof(TResponse).Name} response.");
        }
        catch (TimeoutException) when (!cancellationToken.IsCancellationRequested)
        {
            linkedCancellation.Cancel();
            ObserveFaultedDispatch(dispatchTask);
            throw new RequestTimeoutException(requestId, typeof(TRequest), typeof(TResponse), effectiveTimeout);
        }
        finally
        {
            responses.Remove(requestId);
        }
    }

    private static void ObserveFaultedDispatch(Task dispatchTask)
    {
        _ = dispatchTask.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
