namespace AutoBus;

/// <summary>
/// Sends a request message to the single registered <see cref="IRequestHandler{TRequest,TResponse}"/>
/// and asynchronously waits for its correlated response.
/// </summary>
public interface IRequestClient<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>
    /// Sends <paramref name="request"/> and waits for the corresponding <typeparamref name="TResponse"/>.
    /// Throws <see cref="RequestTimeoutException"/> if no response arrives before the effective timeout.
    /// </summary>
    Task<TResponse> GetResponseAsync(
        TRequest request,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
