using ErrorOr;
using Goa.Clients.Lambda.Operations.Invoke;
using Goa.Clients.Lambda.Operations.InvokeAsync;
using Goa.Clients.Lambda.Operations.InvokeDryRun;

namespace Goa.Clients.Lambda;

/// <summary>
/// High-performance Lambda client interface optimized for AWS Lambda usage.
/// All operations use strongly-typed request objects and return ErrorOr results.
/// </summary>
public interface ILambdaClient
{
    /// <summary>
    /// Invokes a Lambda function synchronously and waits for the response.
    /// </summary>
    /// <param name="request">The synchronous invoke request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The synchronous invoke response with payload and execution details, or an error if the operation failed.</returns>
    Task<ErrorOr<InvokeResponse>> InvokeSynchronousAsync(InvokeRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a Lambda function asynchronously without waiting for the response.
    /// </summary>
    /// <param name="request">The asynchronous invoke request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The asynchronous invoke response indicating if the invocation was queued, or an error if the operation failed.</returns>
    Task<ErrorOr<InvokeAsyncResponse>> InvokeAsynchronousAsync(InvokeAsyncRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates parameters and permissions for a Lambda function invocation without executing it.
    /// </summary>
    /// <param name="request">The dry run invoke request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The dry run invoke response indicating validation success, or an error if the operation failed.</returns>
    Task<ErrorOr<InvokeDryRunResponse>> InvokeDryRunAsync(InvokeDryRunRequest request, CancellationToken cancellationToken = default);
}