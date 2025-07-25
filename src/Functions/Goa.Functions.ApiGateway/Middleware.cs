namespace Goa.Functions.ApiGateway;

/// <summary>
///     Base class to help implement middleware
/// </summary>
public abstract class Middleware : IMiddleware
{
    public async Task InvokeAsync(InvocationContext context, Func<Task> next, CancellationToken cancellationToken)
    {
        var response = await InvokeAsync(context, cancellationToken);
        if (response is null)
            await next();
        else
            context.Response.Result = response;
    }

    protected abstract Task<HttpResult?> InvokeAsync(InvocationContext context, CancellationToken cancellationToken);
}