using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Generic;

/// <summary>
/// Extension methods for configuring generic Lambda handlers
/// </summary>
public static class GenericHandlerExtensions
{
    /// <summary>
    /// Configures the Lambda to handle string payloads (input and output)
    /// </summary>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <returns>A builder for configuring string-based handlers</returns>
    public static IStringHandlerBuilder ForString(this ILambdaBuilder builder)
        => new StringHandlerBuilder(builder);

    /// <summary>
    /// Configures the Lambda to handle typed requests and responses
    /// </summary>
    /// <typeparam name="TRequest">The type of the request payload</typeparam>
    /// <typeparam name="TResponse">The type of the response payload</typeparam>
    /// <param name="builder">The Lambda builder to configure</param>
    /// <param name="serializationContext">JSON serialization context containing type information for TRequest and TResponse</param>
    /// <returns>A builder for configuring typed handlers</returns>
    public static ITypedHandlerBuilder<TRequest, TResponse> ForType<TRequest, TResponse>(
        this ILambdaBuilder builder,
        JsonSerializerContext serializationContext)
        => new TypedHandlerBuilder<TRequest, TResponse>(builder, serializationContext);
}
