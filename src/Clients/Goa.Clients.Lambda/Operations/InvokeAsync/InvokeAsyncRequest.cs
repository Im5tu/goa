using System.Text.Json.Serialization;

namespace Goa.Clients.Lambda.Operations.InvokeAsync;

/// <summary>
/// Request for asynchronous Lambda function invocation.
/// </summary>
public sealed class InvokeAsyncRequest
{
    /// <summary>
    /// The name of the Lambda function to invoke.
    /// </summary>
    [JsonIgnore]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// The function qualifier (version or alias). Optional.
    /// </summary>
    [JsonIgnore]
    public string? Qualifier { get; set; }

    /// <summary>
    /// Client context information to pass to the function. Optional.
    /// </summary>
    [JsonIgnore]
    public string? ClientContext { get; set; }

    /// <summary>
    /// The JSON payload to send to the function.
    /// </summary>
    public string? Payload { get; set; }
}