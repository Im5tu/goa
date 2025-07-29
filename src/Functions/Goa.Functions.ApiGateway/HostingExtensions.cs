using Goa.Functions.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.ApiGateway;

/// <summary>
/// Extensions for hosting
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures the Lambda builder to use ASP.NET Core with the specified endpoint configuration.
    /// </summary>
    /// <param name="builder">The Lambda builder instance to configure.</param>
    /// <param name="endpointDelegate">Action to configure the application's request pipeline and endpoints.</param>
    /// <param name="webBuilderDelegate">Optional action to configure additional web host settings.</param>
    /// <param name="apiGatewayType">The type of API Gateway to use. Defaults to HttpV2.</param>
    /// <returns>The configured Lambda builder for method chaining.</returns>
    public static ILambdaBuilder ForAspNetCore(this ILambdaBuilder builder, Action<IApplicationBuilder> endpointDelegate, Action<IWebHostBuilder>? webBuilderDelegate = null, ApiGatewayType apiGatewayType = ApiGatewayType.HttpV2)
    {
        builder.WithServices(services =>
        {
            services.AddRoutingCore();
        });
        builder.WithLoggingSerializationContext(LoggingSerializationContext.Default);
        builder.Host.ConfigureSlimWebHost(webBuilder =>
        {
            webBuilderDelegate?.Invoke(webBuilder);
            webBuilder.UseGoaServer(apiGatewayType, builder.LambdaRuntime);
            webBuilder.Configure((_, app) => endpointDelegate(app));
        }, _ => { });
        return builder;
    }
}
