namespace Goa.Functions.Core.Bootstrapping;

/// <summary>
///     Interface for interacting with the AWS Lambda Runtime API to handle Lambda function invocations, report errors, and send responses.
/// </summary>
public interface ILambdaRuntimeClient
{
    /// <summary>
    ///     Retrieves the next Lambda invocation from the AWS Lambda Runtime API.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation if necessary.</param>
    Task<Result<InvocationRequest>> GetNextInvocationAsync(CancellationToken cancellationToken = default);
    /// <summary>
    ///     Reports an initialization error to the AWS Lambda Runtime API.
    ///     This should be used when the function encounters an error during startup or initialization.
    /// </summary>
    /// <param name="errorPayload">The payload containing details about the initialization error.</param>
    /// <param name="cancellationToken">Token to cancel the operation if necessary.</param>
    Task<Result> ReportInitializationErrorAsync(InitializationErrorPayload errorPayload, CancellationToken cancellationToken = default);
    /// <summary>
    ///     Reports an invocation error to the AWS Lambda Runtime API.
    ///     This should be used when the function encounters an error while processing a specific invocation.
    /// </summary>
    /// <param name="awsRequestId">The AWS request ID of the invocation that encountered the error.</param>
    /// <param name="errorPayload">The payload containing details about the invocation error.</param>
    /// <param name="cancellationToken">Token to cancel the operation if necessary.</param>
    Task<Result> ReportInvocationErrorAsync(string awsRequestId, InvocationErrorPayload errorPayload, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Sends the response for a completed invocation to the AWS Lambda Runtime API.
    /// </summary>
    /// <param name="awsRequestId">The AWS request ID of the invocation to send the response for.</param>
    /// <param name="content">The content of the response to send back to the runtime.</param>
    /// <param name="cancellationToken">Token to cancel the operation if necessary.</param>
    Task<Result> SendResponseAsync(string awsRequestId, HttpContent content, CancellationToken cancellationToken = default);
}
