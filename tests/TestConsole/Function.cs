using Goa.Functions.ApiGateway.Payloads.V2;
using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json.Serialization;

var logger = new JsonLogger("Test", LogLevel.Information);
var http = Http.UseHttpV2()
    .UseMiddleware(pipeline =>
    {
        pipeline.Use((_, next, _) =>
        {
            logger.LogInvocation();
            return next();
        });
    })
    .MapGet("/ping", (context, _) =>
    {
        context.Response.Result = HttpResult.Ok(new Pong("Hi hi"));
        return Task.CompletedTask;
    })
    .MapGet("/ping2", (context, _) =>
    {
        context.Response.Result = HttpResult.NoContent();
        return Task.CompletedTask;
    });

var p = http.CreatePipeline().ToList();
var pipeline = new Function(p, MySerializerContext.Default);

var sw = Stopwatch.StartNew();
var result1 = await pipeline.InvokeAsync(new ProxyPayloadV2Request
{
    Headers = new Dictionary<string, string>
    {
        ["Accept"] = "application/json"
    },
    RawPath = "/ping"
}, default);

var result2 = await pipeline.InvokeAsync(new ProxyPayloadV2Request
{
    RawPath = "/ping2"
}, default);

Console.WriteLine($"Elapsed time: {sw.ElapsedMilliseconds}ms");


await Lambda.RunAsync(http, MySerializerContext.Default);

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(Pong))]
public partial class MySerializerContext : JsonSerializerContext
{
}

public static partial class FunctionLogs
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Middleware Invoked")]
    public static partial void LogInvocation(this ILogger logger);
}

public record Pong(string Message);
