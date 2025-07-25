namespace Goa.Functions.ApiGateway;

/// <summary>
///     Base class to help implement middleware components in the API Gateway pipeline.
///     Provides a simplified way to return a response or delegate to the next middleware.
/// </summary>
public abstract class Middleware : IMiddleware
{
    /// <summary>
    ///     Executes the middleware logic. If the derived middleware returns a non-null result,
    ///     it is assigned to the response and short-circuits the pipeline. Otherwise, continues to the next middleware.
    /// </summary>
    /// <param name="context">The current invocation context.</param>
    /// <param name="next">A delegate that invokes the next middleware in the pipeline.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InvokeAsync(InvocationContext context, Func<Task> next, CancellationToken cancellationToken)
    {
        var response = await InvokeAsync(context, cancellationToken);
        if (response is null)
            await next();
        else
            context.Response.Result = response;
    }

    /// <summary>
    ///     Override this method to implement custom middleware logic.
    ///     Return a <see cref="HttpResult"/> to short-circuit the pipeline,
    ///     or <c>null</c> to continue to the next middleware.
    /// </summary>
    /// <param name="context">The current invocation context.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the task to complete.</param>
    /// <returns>
    ///     A task that represents the asynchronous operation, containing either an <see cref="HttpResult"/>
    ///     to end the pipeline or <c>null</c> to continue.
    /// </returns>
    protected abstract Task<HttpResult?> InvokeAsync(InvocationContext context, CancellationToken cancellationToken);
}
