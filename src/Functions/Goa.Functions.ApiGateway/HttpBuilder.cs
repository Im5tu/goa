namespace Goa.Functions.ApiGateway;

internal sealed class HttpBuilder : IHttpBuilder
{
    private readonly List<Func<HttpRequestContext, Task, CancellationToken, Task>> _middleware = new();
    private readonly Dictionary<string, Dictionary<string, List<Func<HttpRequestContext, Task, CancellationToken, Task>>>> _paths = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Func<HttpRequestContext, Task, CancellationToken, Task>> CreatePipeline()
    {
        return new List<Func<HttpRequestContext, Task, CancellationToken, Task>>();
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

        if (!_paths.TryGetValue(method, out var pathDictionary))
        {
            pathDictionary = new Dictionary<string, List<Func<HttpRequestContext, Task, CancellationToken, Task>>>(StringComparer.OrdinalIgnoreCase);
        }

        if (!pathDictionary.TryGetValue(path, out var list))
        {
            throw new ArgumentException($"The specified route has already been added. Method: {method} Path: {path}");
        }

        var pipeline = new PipelineBuilder();
        builder(pipeline);
        list.AddRange(pipeline.Create());
    }
}
