namespace Goa.Functions.ApiGateway;

#pragma warning disable CS1591, CS3021
public interface IHttpBuilder
{
    IEnumerable<Func<InvocationContext, Func<Task>, CancellationToken, Task>> CreatePipeline();

    IHttpBuilder MapGet(string path, Action<IPipelineBuilder> builder);
    IHttpBuilder MapGet(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) => MapGet(path, pipeline => pipeline.Use(builder));
    IHttpBuilder MapGet(string path, Func<InvocationContext, CancellationToken, Task> builder) => MapGet(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));
    IHttpBuilder MapPost(string path, Action<IPipelineBuilder> builder);
    IHttpBuilder MapPost(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) => MapPost(path, pipeline => pipeline.Use(builder));
    IHttpBuilder MapPost(string path, Func<InvocationContext, CancellationToken, Task> builder) => MapPost(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));
    IHttpBuilder MapPut(string path, Action<IPipelineBuilder> builder);
    IHttpBuilder MapPut(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) => MapPut(path, pipeline => pipeline.Use(builder));
    IHttpBuilder MapPut(string path, Func<InvocationContext, CancellationToken, Task> builder) => MapPut(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    IHttpBuilder MapPatch(string path, Action<IPipelineBuilder> builder);
    IHttpBuilder MapPatch(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) => MapPatch(path, pipeline => pipeline.Use(builder));
    IHttpBuilder MapPatch(string path, Func<InvocationContext, CancellationToken, Task> builder) => MapPatch(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    IHttpBuilder MapDelete(string path, Action<IPipelineBuilder> builder);
    IHttpBuilder MapDelete(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) => MapDelete(path, pipeline => pipeline.Use(builder));
    IHttpBuilder MapDelete(string path, Func<InvocationContext, CancellationToken, Task> builder) => MapDelete(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    IHttpBuilder MapOptions(string path, Action<IPipelineBuilder> builder);
    IHttpBuilder MapOptions(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) => MapOptions(path, pipeline => pipeline.Use(builder));
    IHttpBuilder MapOptions(string path, Func<InvocationContext, CancellationToken, Task> builder) => MapOptions(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));

    IHttpBuilder MapHead(string path, Action<IPipelineBuilder> builder);
    IHttpBuilder MapHead(string path, Func<InvocationContext, Func<Task>, CancellationToken, Task> builder) => MapHead(path, pipeline => pipeline.Use(builder));
    IHttpBuilder MapHead(string path, Func<InvocationContext, CancellationToken, Task> builder) => MapHead(path, pipeline => pipeline.Use((context, _, token) => builder(context, token)));


    IHttpBuilder UseMiddleware(Action<IPipelineBuilder> builder);
}
