using Goa.Functions.ApiGateway.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateEmptyBuilder(new()).UseGoa();
builder.Services.AddRoutingCore();
var app = builder.Build();
app.MapGet("/", () => "Hello World!");
app.MapGet("/hi", () => "hihi!");

await app.RunAsync();
