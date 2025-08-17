using Goa.Functions.ApiGateway.Features.V1;
using Goa.Functions.ApiGateway.Features.V2;
using Goa.Functions.Core.Bootstrapping;
using Goa.Functions.Core.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway;

/*
    Consider:
        - IHttpConnectionFeature
        - ITlsConnectionFeature
        - IHttpSysRequestTimingFeature
 */

/// <summary>
/// Provides extension methods for configuring Goa server and logging in a WebApplicationBuilder.
/// </summary>
public static class ServerExtensions
{
    /// <summary>
    /// Configures the <see cref="WebApplicationBuilder"/> to use the Goa Lambda server and structured JSON logging.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <param name="apiGatewayType">The API Gateway version to target (HttpV1 or HttpV2).</param>
    /// <param name="jsonSerializerContext">Optional custom serializer context for JSON logging.</param>
    /// <param name="lambdaRuntimeClient">Optional Lambda runtime client override.</param>
    /// <returns>The configured <see cref="WebApplicationBuilder"/> instance.</returns>
    public static WebApplicationBuilder UseGoa(this WebApplicationBuilder builder, ApiGatewayType apiGatewayType = default, JsonSerializerContext? jsonSerializerContext = null, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        builder.WebHost.UseGoaServer(apiGatewayType, lambdaRuntimeClient);
        builder.Logging.AddGoaJsonLogging(jsonSerializerContext ?? LoggingSerializationContext.Default);
        return builder;
    }

    /// <summary>
    /// Configures the web host to use the Goa Lambda server implementation based on the specified API Gateway type.
    /// </summary>
    /// <param name="builder">The web host builder.</param>
    /// <param name="apiGatewayType">The API Gateway version to target (HttpV1 or HttpV2).</param>
    /// <param name="lambdaRuntimeClient">Optional Lambda runtime client override.</param>
    /// <returns>The configured <see cref="ConfigureWebHostBuilder"/> instance.</returns>
    public static IWebHostBuilder UseGoaServer(this IWebHostBuilder builder, ApiGatewayType apiGatewayType = default, ILambdaRuntimeClient? lambdaRuntimeClient = null)
    {
        if (apiGatewayType == ApiGatewayType.HttpV2)
            builder.UseServer(new LambdaHTTPV2Server(lambdaRuntimeClient));
        else
            builder.UseServer(new LambdaHTTPV1Server(lambdaRuntimeClient));

        return builder;
    }
}
