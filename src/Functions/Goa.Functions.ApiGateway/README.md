# Goa.Functions.ApiGateway

Build high-performance AWS Lambda functions with ASP.NET Core integration for API Gateway HTTP/REST APIs. This package provides seamless integration between AWS API Gateway and ASP.NET Core, supporting both V1 and V2 payload formats with native AOT compilation for optimal cold start performance.

## Quick Start

```bash
dotnet new install Goa.Templates
dotnet new goa.apigw -n "MyApiFunction"
```

## Features

- **ASP.NET Core Integration**: Full support for ASP.NET Core middleware, routing, and endpoints
- **Multiple API Gateway Types**: Support for HTTP API V2, HTTP API V1, and REST API formats
- **Native AOT Ready**: Optimized for ahead-of-time compilation with minimal cold starts
- **High Performance**: Pre-compiled serialization contexts and optimized request/response handling
- **OpenAPI Support**: Optional OpenAPI/Swagger integration with Scalar UI
- **Flexible Routing**: Standard ASP.NET Core routing with full endpoint mapping capabilities
- **Authentication Support**: Built-in support for JWT, IAM, and custom authorizers
- **Minimal Boilerplate**: Simple, functional-style configuration with sensible defaults

## Basic Usage

```csharp
using Goa.Functions.ApiGateway;
using Goa.Functions.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForAspNetCore(app =>
    {
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/hello", () => "Hello from Lambda!");
            endpoints.MapPost("/users", async (User user, HttpContext context) => 
            {
                context.Response.StatusCode = 201;
                context.Response.Headers.Location = $"/users/{user.Id}";
                await JsonSerializer.SerializeAsync(context.Response.Body, user, AppJsonContext.Default.User);
            });
        });
    }, apiGatewayType: ApiGatewayType.HttpV2)
    .WithServices(services =>
    {
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolver = AppJsonContext.Default;
        });
    })
    .RunAsync();

public record User(int Id, string Name);

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(User))]
public partial class AppJsonContext : JsonSerializerContext;
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).