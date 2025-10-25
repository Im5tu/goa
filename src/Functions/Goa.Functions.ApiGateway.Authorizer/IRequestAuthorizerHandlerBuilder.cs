using Goa.Functions.Core;

namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Builder interface for configuring REQUEST authorizer handlers
/// </summary>
public interface IRequestAuthorizerHandlerBuilder
{
    /// <summary>
    /// Specifies the handler function to process REQUEST authorization requests
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service</typeparam>
    /// <param name="handler">Function that processes the REQUEST authorizer event and returns an authorization response</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, RequestAuthorizerEvent, Task<AuthorizerResponse>> handler) where THandler : class;
}
