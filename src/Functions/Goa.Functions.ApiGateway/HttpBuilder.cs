using Goa.Functions.Core;
using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Logging;

namespace Goa.Functions.ApiGateway;

internal sealed class HttpBuilder : IHttpBuilder
{
    private static readonly JsonLogger _logger = new("Http", LogLevel.Information); // TODO :: Extract from environment variable
    private readonly List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> _middleware = new();
    private readonly Dictionary<string, Dictionary<string, List<Func<InvocationContext, Func<Task>, CancellationToken, Task>>>> _paths = new(StringComparer.OrdinalIgnoreCase);

    public IEnumerable<Func<InvocationContext, Func<Task>, CancellationToken, Task>> CreatePipeline()
    {
        yield return (context, next, cancellationToken) =>
        {
            IEnumerable<string>? contentTypes = null;
            if (context.Request.Headers?.TryGetValue("Accept", out contentTypes) == false || contentTypes is null)
            {
                context.Response.Result = HttpResult.UnsupportedMediaType();
                return Task.CompletedTask;
            }
            return next();
        };

        // Execute the middleware in the order defined
        foreach (var middleware in _middleware)
            yield return middleware;

        yield return async (ctx, _, cancellationToken) =>
        {
            // This is a terminal method, so we don't need the next parameter
            var request = ctx.Request;

            if (string.IsNullOrEmpty(request.Path))
            {
                ctx.Response.Result = HttpResult.BadRequest();
                return;
            }

            // Check for routes based on HTTP method and path
            if (_paths.TryGetValue(request.Path, out var methods))
            {
                if (methods.TryGetValue(request.HttpMethod.Method, out var routeMiddleware))
                {
                    // Add the route-specific middleware to the pipeline
                    await InvokeNextAsync(routeMiddleware, 0, ctx, cancellationToken);
                    return;
                }

                ctx.Response.Result = HttpResult.MethodNotAllowed();
                return;
            }

            // If no match found, return 404
            ctx.Response.Result = HttpResult.NotFound();
        };
    }

    private static async Task InvokeNextAsync(List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> pipeline, int index, InvocationContext ctx, CancellationToken token)
    {
        if (index >= pipeline.Count)
        {
            return; // End of the pipeline, return completed task
        }

        try
        {
            // Get the current pipeline step and pass in the next step as the Func<Task>
            Task NextAsync() => InvokeNextAsync(pipeline, index + 1, ctx, token);
            await pipeline[index].Invoke(ctx, NextAsync, token);
        }
        catch (Exception ex)
        {
            _logger.LogFunctionError(ex);
            ctx.Response.Result = HttpResult.InternalServerError();
            ctx.Response.Exception = ex;
            ctx.Response.ExceptionHandled = true;
        }
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
            _paths[path] = methods = new(StringComparer.OrdinalIgnoreCase);
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
