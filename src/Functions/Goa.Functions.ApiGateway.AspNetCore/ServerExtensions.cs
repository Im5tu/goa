using Goa.Functions.ApiGateway.AspNetCore.Features.V1;
using Goa.Functions.ApiGateway.AspNetCore.Features.V2;
using Goa.Functions.Core.Bootstrapping;
using Goa.Functions.Core.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.AspNetCore;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member


/*
    Consider:
        - IHttpConnectionFeature
        - ITlsConnectionFeature
        - IHttpSysRequestTimingFeature
 */

public static class ServerExtensions
{
    public static WebApplicationBuilder UseGoa(this WebApplicationBuilder builder, ApiGatewayType apiGatewayType = default, JsonSerializerContext? jsonSerializerContext = null, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        builder.WebHost.UseGoaServer(apiGatewayType, lambdaRuntimeClient);
        builder.Logging.AddGoaJsonLogging(jsonSerializerContext ?? LoggingSerializationContext.Default);
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Routing.EndpointMiddleware", LogLevel.Warning);

        return builder;
    }

    public static ConfigureWebHostBuilder UseGoaServer(this ConfigureWebHostBuilder builder, ApiGatewayType apiGatewayType = default, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        if (apiGatewayType == ApiGatewayType.HttpV2)
            builder.UseServer(new LambdaHTTPV2Server(lambdaRuntimeClient));
        else
            builder.UseServer(new LambdaHTTPV1Server(lambdaRuntimeClient));

        return builder;
    }
}
