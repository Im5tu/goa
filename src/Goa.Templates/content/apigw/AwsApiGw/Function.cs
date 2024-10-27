namespace AwsApiGw;

//#if (functionType == 'httpv2')
var http = Http.UseHttpV2()
//#if (functionType == 'httpv1')
var http = Http.UseHttpV1()
//#else
var http = Http.UseRestApi()
//#endif
    .MapGet("/ping", (context, next, ct) =>
    {
        context.Response.Set(HttpResult.Ok(new Pong("pong")));
        return next;
    });

await Lambda.RunAsync(http, HttpSerializerContext.Default);

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Pong))]
public partial class HttpSerializerContext : JsonSerializerContext
{
}

public record Pong(string Message);
