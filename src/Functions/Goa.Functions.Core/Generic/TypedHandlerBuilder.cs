using Goa.Core;
using Goa.Functions.Core.Bootstrapping;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Generic;

/// <summary>
/// Handler builder for typed Lambda functions with custom request and response types.
/// Can be extended by event-specific handlers.
/// </summary>
/// <typeparam name="TRequest">The type of the request payload</typeparam>
/// <typeparam name="TResponse">The type of the response payload</typeparam>
public class TypedHandlerBuilder<TRequest, TResponse> : ITypedHandlerBuilder<TRequest, TResponse>
{
    /// <summary>
    /// The Lambda builder for configuring services
    /// </summary>
    protected readonly ILambdaBuilder Builder;

    /// <summary>
    /// The JSON serialization context for AOT-compatible serialization
    /// </summary>
    protected readonly JsonSerializerContext SerializationContext;

    /// <summary>
    /// Creates a new TypedHandlerBuilder
    /// </summary>
    public TypedHandlerBuilder(ILambdaBuilder builder, JsonSerializerContext serializationContext)
    {
        Builder = builder;
        SerializationContext = serializationContext;
    }

    /// <summary>
    /// Gets the logger category name for this handler
    /// </summary>
    protected virtual string GetLoggerName()
        => $"TypedHandler<{typeof(TRequest).Name},{typeof(TResponse).Name}>";

    /// <inheritdoc />
    public virtual IRunnable HandleWith<THandler>(Func<THandler, TRequest, CancellationToken, Task<TResponse>> handler)
        where THandler : class
    {
        return HandleWithLogger<THandler>(async (h, request, logger, ct) =>
        {
            try
            {
                return await handler(h, request, ct);
            }
            catch (Exception e)
            {
                logger.LogException(e);
                throw;
            }
        });
    }

    /// <summary>
    /// Registers a handler with access to the logger for custom exception handling.
    /// Use this when derived handlers need to log exceptions without rethrowing.
    /// </summary>
    protected IRunnable HandleWithLogger<THandler>(Func<THandler, TRequest, ILogger, CancellationToken, Task<TResponse>> handler)
        where THandler : class
    {
        Builder.WithServices(services =>
        {
            services.AddSingleton<Func<TRequest, InvocationRequest, CancellationToken, Task<TResponse>>>(sp =>
            {
                var invocationHandler = sp.GetRequiredService<THandler>();
                var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(GetLoggerName());

                Func<TRequest, InvocationRequest, CancellationToken, Task<TResponse>> result = (request, _, cancellationToken) =>
                    handler(invocationHandler, request, logger, cancellationToken);

                return result;
            });

            services.AddHostedService(sp =>
                ActivatorUtilities.CreateInstance<LambdaBootstrapService<TRequest, TResponse>>(
                    sp, SerializationContext));
        });

        return new Runnable(Builder);
    }
}
