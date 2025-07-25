namespace Goa.Functions.ApiGateway;

/// <summary>
/// Provides methods to configure HTTP route handlers and middleware pipelines.
/// </summary>
public interface IHttpBuilder
{
    /// <summary>
    /// Builds and returns the middleware pipeline for all configured routes.
    /// </summary>
    /// <returns>A sequence of middleware delegates.</returns>
    IEnumerable<Func<InvocationContext, Func<Task>, CancellationToken, Task>> CreatePipeline();

    /// <summary>
    /// Maps a GET route to a middleware pipeline.
    /// </summary>
    /// <param name="path">The route path.</param>
    /// <param name="builder">A delegate to configure the pipeline for the route.</param>
    IHttpBuilder MapGet(string path, Action<IPipelineBuilder> builder);

    /// <inheritdoc cref="MapGet(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapGet(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) =>
        MapGet(path, pipeline => pipeline.Use(builder));

    /// <inheritdoc cref="MapGet(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapGet(string path, Func<InvocationContext, CancellationToken, Task> builder) =>
        MapGet(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    /// <summary>Maps a POST route to a middleware pipeline.</summary>
    IHttpBuilder MapPost(string path, Action<IPipelineBuilder> builder);

    /// <inheritdoc cref="MapPost(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapPost(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) =>
        MapPost(path, pipeline => pipeline.Use(builder));

    /// <inheritdoc cref="MapPost(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapPost(string path, Func<InvocationContext, CancellationToken, Task> builder) =>
        MapPost(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    /// <summary>Maps a PUT route to a middleware pipeline.</summary>
    IHttpBuilder MapPut(string path, Action<IPipelineBuilder> builder);

    /// <inheritdoc cref="MapPut(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapPut(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) =>
        MapPut(path, pipeline => pipeline.Use(builder));

    /// <inheritdoc cref="MapPut(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapPut(string path, Func<InvocationContext, CancellationToken, Task> builder) =>
        MapPut(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    /// <summary>Maps a PATCH route to a middleware pipeline.</summary>
    IHttpBuilder MapPatch(string path, Action<IPipelineBuilder> builder);

    /// <inheritdoc cref="MapPatch(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapPatch(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) =>
        MapPatch(path, pipeline => pipeline.Use(builder));

    /// <inheritdoc cref="MapPatch(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapPatch(string path, Func<InvocationContext, CancellationToken, Task> builder) =>
        MapPatch(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    /// <summary>Maps a DELETE route to a middleware pipeline.</summary>
    IHttpBuilder MapDelete(string path, Action<IPipelineBuilder> builder);

    /// <inheritdoc cref="MapDelete(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapDelete(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) =>
        MapDelete(path, pipeline => pipeline.Use(builder));

    /// <inheritdoc cref="MapDelete(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapDelete(string path, Func<InvocationContext, CancellationToken, Task> builder) =>
        MapDelete(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    /// <summary>Maps an OPTIONS route to a middleware pipeline.</summary>
    IHttpBuilder MapOptions(string path, Action<IPipelineBuilder> builder);

    /// <inheritdoc cref="MapOptions(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapOptions(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) =>
        MapOptions(path, pipeline => pipeline.Use(builder));

    /// <inheritdoc cref="MapOptions(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapOptions(string path, Func<InvocationContext, CancellationToken, Task> builder) =>
        MapOptions(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    /// <summary>Maps a HEAD route to a middleware pipeline.</summary>
    IHttpBuilder MapHead(string path, Action<IPipelineBuilder> builder);

    /// <inheritdoc cref="MapHead(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapHead(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) =>
        MapHead(path, pipeline => pipeline.Use(builder));

    /// <inheritdoc cref="MapHead(string, Action{IPipelineBuilder})"/>
    IHttpBuilder MapHead(string path, Func<InvocationContext, CancellationToken, Task> builder) =>
        MapHead(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    /// <summary>
    /// Adds middleware that applies to all routes.
    /// </summary>
    /// <param name="builder">A delegate to configure the shared middleware pipeline.</param>
    IHttpBuilder UseMiddleware(Action<IPipelineBuilder> builder);
}
