using Goa.Core;
using Goa.Functions.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Internal implementation of the authorizer handler builder
/// </summary>
internal sealed class HandlerBuilder : ITokenAuthorizerHandlerBuilder, IRequestAuthorizerHandlerBuilder
{
    private readonly ILambdaBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the HandlerBuilder class
    /// </summary>
    /// <param name="builder">The Lambda builder</param>
    public HandlerBuilder(ILambdaBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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
