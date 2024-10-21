var http = Http.UseHttpV2()
    .MapGet("/ping", (context, next, ct) =>
    {
        context.Response.Set(HttpResult.Ok());
        return next;
    });

await Lambda.RunAsync(http);
