namespace Goa.Clients.Lambda.Operations.InvokeDryRun;

/// <summary>
/// Response from the dry run Invoke operation.
/// </summary>
public sealed class InvokeDryRunResponse
{
    /// <summary>
    /// The HTTP status code for the dry run invocation (typically 204 No Content).
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Whether the dry run validation was successful.
    /// </summary>
    public bool IsSuccess => StatusCode == 204;
}