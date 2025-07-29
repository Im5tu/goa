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
        // TODO :: Add any custom middleware you want here

        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            // TODO :: Map your endpoints here
            endpoints.MapGet("/ping", static async context =>
            {
                var pong = new Pong("PONG!");
                context.Response.ContentType = "application/json";
                await JsonSerializer.SerializeAsync(context.Response.Body, pong, HttpSerializerContext.Default.Pong);
            });
        });
//#if (functionType == 'httpv2')
    }, apiGatewayType: ApiGatewayType.HttpV2)
//#else
    }, apiGatewayType: ApiGatewayType.HttpV1)
//#endif
    .WithServices(services =>
    {
        // TODO :: Configure your services here

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
