namespace Goa.Functions.ApiGateway;

#pragma warning disable CS1591, CS3021
public interface IMiddleware
{
    Task InvokeAsync(InvocationContext context, Func<Task> next, CancellationToken cancellationToken);
}