using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

internal sealed class HandlerBuilder : ITokenAuthorizerHandlerBuilder, IRequestAuthorizerHandlerBuilder
{
    private readonly ILambdaBuilder _builder;

    public HandlerBuilder(ILambdaBuilder builder)
    {
        _builder = builder;
    }

    public IRunnable HandleWith<THandler>(Func<THandler, TokenAuthorizerEvent, Task<AuthorizerResponse>> handler) where THandler : class
    {
        var context = (JsonSerializerContext)AuthorizerEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<TokenAuthorizerEvent, InvocationRequest, CancellationToken, Task<AuthorizerResponse>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("TokenAuthorizerHandler");
                Func<TokenAuthorizerEvent, InvocationRequest, CancellationToken, Task<AuthorizerResponse>> result = async (authorizerEvent, _, _) =>
                {
                    try
                    {
                        return await handler(invocationHandler, authorizerEvent);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        throw;
                    }
                };
                return result;
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<TokenAuthorizerEvent, AuthorizerResponse>>(sp, context));
        });

        return new Runnable(_builder);
    }

    public IRunnable HandleWith<THandler>(Func<THandler, RequestAuthorizerEvent, Task<AuthorizerResponse>> handler) where THandler : class
    {
        var context = (JsonSerializerContext)AuthorizerEventSerializationContext.Default;
        _builder.WithServices(services =>
        {
            services.AddSingleton<Func<RequestAuthorizerEvent, InvocationRequest, CancellationToken, Task<AuthorizerResponse>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("RequestAuthorizerHandler");
                Func<RequestAuthorizerEvent, InvocationRequest, CancellationToken, Task<AuthorizerResponse>> result = async (authorizerEvent, _, _) =>
                {
                    try
                    {
                        return await handler(invocationHandler, authorizerEvent);
                    }
                    catch (Exception e)
                    {
                        logger.LogException(e);
                        throw;
                    }
                };
                return result;
            });
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<LambdaBootstrapService<RequestAuthorizerEvent, AuthorizerResponse>>(sp, context));
        });

        return new Runnable(_builder);
    }
}
