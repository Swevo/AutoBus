namespace AutoBus;

/// <summary>
/// Thrown when a request/response exchange does not complete before the configured timeout.
/// </summary>
public sealed class RequestTimeoutException : TimeoutException
{
    public RequestTimeoutException(Guid requestId, Type requestType, Type responseType, TimeSpan timeout)
        : base($"Timed out waiting {timeout} for {responseType.Name} in response to {requestType.Name} request '{requestId}'.")
    {
        RequestId = requestId;
        RequestType = requestType;
        ResponseType = responseType;
        Timeout = timeout;
    }

    /// <summary>The request correlation identifier that timed out.</summary>
    public Guid RequestId { get; }

    /// <summary>The request message type.</summary>
    public Type RequestType { get; }

    /// <summary>The expected response message type.</summary>
    public Type ResponseType { get; }

    /// <summary>The effective timeout that elapsed.</summary>
    public TimeSpan Timeout { get; }
}
