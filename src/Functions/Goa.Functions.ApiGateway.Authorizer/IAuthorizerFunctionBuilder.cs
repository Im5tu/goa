using Goa.Functions.Core;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Builder interface for configuring API Gateway Lambda authorizer functions
/// </summary>
public interface IAuthorizerFunctionBuilder : ILambdaBuilder
{
    /// <summary>
    /// Configures the authorizer to handle TOKEN type authorization requests
    /// </summary>
    /// <returns>A builder for configuring TOKEN authorizer handlers</returns>
    ITokenAuthorizerHandlerBuilder ForTokenAuthorizer();

    /// <summary>
    /// Configures the authorizer to handle REQUEST type authorization requests
    /// </summary>
    /// <returns>A builder for configuring REQUEST authorizer handlers</returns>
    IRequestAuthorizerHandlerBuilder ForRequestAuthorizer();
}
