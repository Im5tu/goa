using SampleApi;

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
    })
    .MapGet("/hello/{id}", (context, next, ct) =>
    {
        context.Response.Result = HttpResult.Ok(new Pong($"hello {context.Request.RouteValues!["id"]}"));
        return next();
    });

await Lambda.RunAsync(http, SampleApi.HttpSerializerContext.Default);