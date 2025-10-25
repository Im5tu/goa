using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.Hosting;

namespace Goa.Functions.ApiGateway.Authorizer;

internal sealed class AuthorizerFunctionBuilder : LambdaBuilder, IAuthorizerFunctionBuilder
{
    public AuthorizerFunctionBuilder(IHostBuilder builder, ILambdaRuntimeClient? lambdaRuntimeClient)
        : base(builder, lambdaRuntimeClient)
    {
    }

    public ITokenAuthorizerHandlerBuilder ForTokenAuthorizer()
    {
        return new HandlerBuilder(this);
    }

    public IRequestAuthorizerHandlerBuilder ForRequestAuthorizer()
    {
        return new HandlerBuilder(this);
    }
}
