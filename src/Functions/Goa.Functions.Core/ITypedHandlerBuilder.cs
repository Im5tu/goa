namespace Goa.Functions.Core;

/// <summary>
/// Base interface for all Lambda handler builders
/// </summary>
/// <typeparam name="TRequest">The type of the request payload</typeparam>
/// <typeparam name="TResponse">The type of the response payload</typeparam>
public interface ITypedHandlerBuilder<TRequest, TResponse>
{
    /// <summary>
    /// Specifies the handler function to process typed requests
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service to resolve from DI</typeparam>
    /// <param name="handler">Function that processes a typed request and returns a typed response</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, TRequest, Task<TResponse>> handler)
        where THandler : class
        => HandleWith<THandler>((h, req, _) => handler(h, req));

    /// <summary>
    /// Specifies the handler function to process typed requests with cancellation support
    /// </summary>
    /// <typeparam name="THandler">The type of the handler service to resolve from DI</typeparam>
    /// <param name="handler">Function that processes a typed request with cancellation token and returns a typed response</param>
    /// <returns>A runnable instance to execute the Lambda function</returns>
    IRunnable HandleWith<THandler>(Func<THandler, TRequest, CancellationToken, Task<TResponse>> handler)
        where THandler : class;
}
