var http = Http.UseHttpV2()
    .MapGet("/ping", (context, _) =>
    {
        context.Response = HttpResult.Ok();
        return Task.CompletedTask;
    });

await Lambda.RunAsync(http);
