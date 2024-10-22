using System.Collections;

namespace Goa.Functions.ApiGateway;

internal sealed class HttpBuilder : IHttpBuilder
{
    private readonly List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> _middleware = new();
    private readonly Dictionary<string, Dictionary<string, List<Func<InvocationContext, Func<Task>, CancellationToken, Task>>>> _paths = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Func<InvocationContext, Func<Task>, CancellationToken, Task>> CreatePipeline()
    {
        // Execute the middleware in the order defined
        foreach (var middleware in _middleware)
            yield return middleware;

        // todo :: handle paths

        // The last middleware that should execute is the fallback which just 404's
        yield return static (context, _, _) =>
        {
            context.Response = HttpResult.NotFound();
            return Task.CompletedTask;
        };;
    }

    public IHttpBuilder MapGet(string path, Action<IPipelineBuilder> builder)
    {
        MapRequest("GET", path, builder);
        return this;
    }

    public IHttpBuilder MapPost(string path, Action<IPipelineBuilder> builder)
    {
        MapRequest("POST", path, builder);
        return this;
    }

    public IHttpBuilder MapPut(string path, Action<IPipelineBuilder> builder)
    {
        MapRequest("PUT", path, builder);
        return this;
    }

    public IHttpBuilder MapPatch(string path, Action<IPipelineBuilder> builder)
    {
        MapRequest("PATCH", path, builder);
        return this;
    }

    public IHttpBuilder MapDelete(string path, Action<IPipelineBuilder> builder)
    {
        MapRequest("DELETE", path, builder);
        return this;
    }

    public IHttpBuilder MapOptions(string path, Action<IPipelineBuilder> builder)
    {
        MapRequest("OPTIONS", path, builder);
        return this;
    }

    public IHttpBuilder MapHead(string path, Action<IPipelineBuilder> builder)
    {
        MapRequest("HEAD", path, builder);
        return this;
    }

    public IHttpBuilder UseMiddleware(Action<IPipelineBuilder> builder)
    {
        var pipeline = new PipelineBuilder();
        builder(pipeline);
        _middleware.AddRange(pipeline.Create());
        return this;
    }

    private void MapRequest(string method, string path, Action<IPipelineBuilder> builder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        if (!_paths.TryGetValue(path, out var methods))
        {
            _paths[path] = methods = new (StringComparer.OrdinalIgnoreCase);
        }

        if (methods.TryGetValue(method, out _))
        {
            throw new ArgumentException($"The specified route has already been added. Method: {method} {path}");
        }

        var pipeline = new PipelineBuilder();
        builder(pipeline);
        methods[method] = pipeline.Create().ToList();
    }
}
