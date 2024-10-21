namespace Goa.Functions.ApiGateway;

#pragma warning disable CS1591, CS3021
public interface IMiddleware
{
    Task InvokeAsync(HttpRequestContext context, Task next, CancellationToken cancellationToken);
}
