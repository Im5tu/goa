namespace Goa.Clients.Lambda.Models;

/// <summary>
/// The invocation type for Lambda function calls.
/// </summary>
public enum InvocationType
{
    /// <summary>
    /// Invoke the function synchronously and wait for the response.
    /// </summary>
    RequestResponse,

    /// <summary>
    /// Invoke the function asynchronously.
    /// </summary>
    Event,

    /// <summary>
    /// Validate the request parameters and return the response without invoking the function.
    /// </summary>
    DryRun
}