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
        context.Response.Set(HttpResult.Ok());
        return next;
    });

await Lambda.RunAsync(http);

