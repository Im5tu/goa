using Goa.Functions.ApiGateway;
using Goa.Functions.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using System.Text.Json.Serialization;
//#if (includeOpenApi)
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
//#endif

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForAspNetCore(app =>
    {
        // TODO :: Add any custom middleware you want here

//#if (includeOpenApi)
        app.UseOpenApi();
        app.MapScalarApiReference();

//#endif
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            // TODO :: Map your endpoints here
            endpoints.MapGet("/ping", static async context =>
            {
                var pong = new Pong("PONG!");
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, pong, HttpSerializerContext.Default.Pong);
            })
//#if (includeOpenApi)
            .WithName("Ping")
            .WithSummary("Health check endpoint")
            .WithDescription("Returns a simple PONG response to verify the API is running")
            .WithOpenApi()
//#endif
            ;
        });
//#if (functionType == 'httpv2')
    }, apiGatewayType: ApiGatewayType.HttpV2)
//#else
    }, apiGatewayType: ApiGatewayType.HttpV1)
//#endif
    .WithServices(services =>
    {
        // TODO :: Configure your services here

//#if (includeOpenApi)
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "AwsApiGw API",
                    Version = "v1",
                    Description = "AWS Lambda API Gateway function built with Goa framework"
                };
                return Task.CompletedTask;
            });
        });

//#endif
        // For performance, we've pre-registered a custom JsonSerializationContext
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolver = HttpSerializerContext.Default;
        });
    })
    .RunAsync();


[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Pong))]
public partial class HttpSerializerContext : JsonSerializerContext
{
}

public record Pong(string Message);
