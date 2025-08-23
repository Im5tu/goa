using Goa.Clients.Lambda.Models;

namespace Goa.Clients.Lambda.Operations.Invoke;

/// <summary>
/// Request for the Invoke operation.
/// </summary>
public sealed class InvokeRequest
{
    /// <summary>
    /// The name or ARN of the Lambda function to invoke.
    /// </summary>
    public string FunctionName { get; set; } = "";

    /// <summary>
    /// The invocation type.
    /// </summary>
    public InvocationType InvocationType { get; set; } = InvocationType.RequestResponse;

    /// <summary>
    /// The log type for the invocation.
    /// </summary>
    public LogType LogType { get; set; } = LogType.None;

    /// <summary>
    /// Up to 3583 bytes of base64-encoded data about the invoking client to pass to the function.
    /// </summary>
    public string? ClientContext { get; set; }

    /// <summary>
    /// The qualifier (version or alias) to invoke.
    /// </summary>
    public string? Qualifier { get; set; }

    /// <summary>
    /// The JSON payload to send to the function.
    /// </summary>
    public string? Payload { get; set; }
}
