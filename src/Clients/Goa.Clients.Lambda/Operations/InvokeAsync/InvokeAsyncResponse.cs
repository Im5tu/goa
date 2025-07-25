namespace Goa.Clients.Lambda.Operations.InvokeAsync;

/// <summary>
/// Response from the asynchronous Invoke operation.
/// </summary>
public sealed class InvokeAsyncResponse
{
    /// <summary>
    /// The HTTP status code for the invocation (typically 202 Accepted).
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Whether the asynchronous invocation was successfully queued.
    /// </summary>
    public bool IsSuccess => StatusCode == 202;
}