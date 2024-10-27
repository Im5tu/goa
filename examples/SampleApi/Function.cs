using System.Text.Json.Serialization;

var http = Http.UseHttpV2()
    .MapGet("/hi", (context, next, ct) =>
    {
        context.Response.Result = HttpResult.Ok(new Pong("hihi"));
        return next();
    })
    .MapGet("/hello", (context, next, ct) =>
    {
        context.Response.Result = HttpResult.Ok(new Pong("hello"));
        return next();
    });

await Lambda.RunAsync(http, HttpSerializerContext.Default);

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Pong))]
public partial class HttpSerializerContext : JsonSerializerContext
{
}

public record Pong(string Message);
