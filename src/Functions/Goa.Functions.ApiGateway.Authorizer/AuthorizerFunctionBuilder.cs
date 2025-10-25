using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Internal implementation of the authorizer function builder
/// </summary>
internal sealed class AuthorizerFunctionBuilder : LambdaBuilder, IAuthorizerFunctionBuilder
{
    /// <summary>
    /// Initializes a new instance of the AuthorizerFunctionBuilder class
    /// </summary>
    /// <param name="builder">The host builder</param>
    /// <param name="lambdaRuntimeClient">The Lambda runtime client</param>
    public AuthorizerFunctionBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
        : base(builder, lambdaRuntimeClient)
    {
    }

    /// <inheritdoc />
    public ITokenAuthorizerHandlerBuilder ForTokenAuthorizer()
    {
        return new HandlerBuilder(this);
    }

    /// <inheritdoc />
    public IRequestAuthorizerHandlerBuilder ForRequestAuthorizer()
    {
        return new HandlerBuilder(this);
    }
}
