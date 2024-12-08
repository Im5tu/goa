using Goa.Functions.ApiGateway.AspNetCore;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args).UseGoa();
var app = builder.Build();
app.MapGet("/", () => "Hello World!");

await app.RunAsync();
