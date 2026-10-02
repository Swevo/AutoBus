namespace AutoBus;

/// <summary>
/// Middleware that wraps each consumer invocation.
/// </summary>
public interface IConsumeMiddleware
{
    Task InvokeAsync(ConsumeExecutionContext context, Func<Task> next);
}
