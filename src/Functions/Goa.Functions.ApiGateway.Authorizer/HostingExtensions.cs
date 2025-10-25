using Goa.Functions.Core;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Extensions for hosting API Gateway authorizer functions
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    /// Configures the Lambda builder to handle API Gateway authorizer events
    /// </summary>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <returns>An authorizer function builder for further configuration</returns>
    public static IAuthorizerFunctionBuilder ForAPIGatewayAuthorizer(this ILambdaBuilder builder)
    {
        return new AuthorizerFunctionBuilder(builder.Host, builder.LambdaRuntime);
    }
}
