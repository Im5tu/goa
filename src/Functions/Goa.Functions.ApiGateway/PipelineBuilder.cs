namespace Goa.Functions.ApiGateway;

internal sealed class PipelineBuilder : IPipelineBuilder
{
    private readonly List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> _middleware = new();

    public IPipelineBuilder Use<T>() where T : IMiddleware, new() => Use<T>(() => new T());
    public IPipelineBuilder Use<T>(Func<T> factory) where T : IMiddleware
    {
        _middleware.Add(async (context, next, cancellation) =>
        {
            var middleware = factory();
            try
            {
#pragma warning disable VSTHRD003 // Task is invoked by the next middleware if required
                await middleware.InvokeAsync(context, next, cancellation);
#pragma warning restore VSTHRD003
            }
            finally
            {
                (middleware as IDisposable)?.Dispose();
            }
        });
        return this;
    }
    public IPipelineBuilder Use(Func<InvocationContext, Func<Task>, CancellationToken, Task> handler)
    {
        _middleware.Add(handler);
        return this;
    }

    internal IEnumerable<Func<InvocationContext, Func<Task>, CancellationToken, Task>> Create() => _middleware;
}
