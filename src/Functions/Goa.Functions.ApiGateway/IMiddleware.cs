namespace Goa.Functions.ApiGateway;

#pragma warning disable CS1591, CS3021
public interface IMiddleware
{
    Task InvokeAsync(InvocationContext context, Func<Task> next, CancellationToken cancellationToken);
}

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
            context.Response = response;
    }

    protected abstract Task<HttpResult?> InvokeAsync(InvocationContext context, CancellationToken cancellationToken);
}
