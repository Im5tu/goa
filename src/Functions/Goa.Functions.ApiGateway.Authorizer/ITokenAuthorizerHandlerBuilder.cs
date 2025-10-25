using Goa.Functions.Core;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Builder interface for configuring TOKEN authorizer handlers
/// </summary>
public interface ITokenAuthorizerHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process TOKEN authorization requests
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes the TOKEN authorizer event and returns an authorization response</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, TokenAuthorizerEvent, Task<AuthorizerResponse>> handler) where THandler : class;
}
