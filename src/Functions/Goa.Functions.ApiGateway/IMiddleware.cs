namespace Goa.Functions.ApiGateway;

/// <summary>
/// Represents a middleware component in the API Gateway pipeline.
/// </summary>
public interface IMiddleware
{
    /// <summary>
    /// Executes the middleware logic.
    /// </summary>
    /// <param name="context">The invocation context containing the request and other metadata.</param>
    /// <param name="next">
    /// A delegate representing the next middleware in the pipeline.
    /// Call this to continue processing the pipeline.
    /// </param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task InvokeAsync(InvocationContext context, Func<Task> next, CancellationToken cancellationToken);
}
