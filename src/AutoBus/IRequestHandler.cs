namespace AutoBus;

/// <summary>
/// Handles a request message and returns its correlated response.
/// Register implementations with <c>AutoBusConfigurator.AddRequestHandler&lt;T&gt;()</c>.
/// </summary>
public interface IRequestHandler<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    /// <summary>Handles a single request and returns its response.</summary>
    Task<TResponse> Consume(ConsumeContext<TRequest> context);
}
