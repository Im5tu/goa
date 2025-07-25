using System.Text.Json.Serialization;

namespace Goa.Clients.Lambda.Operations.InvokeDryRun;

/// <summary>
/// Request for dry run Lambda function invocation (validation only).
/// </summary>
public sealed class InvokeDryRunRequest
{
    /// <summary>
    /// The name of the Lambda function to validate.
    /// </summary>
    [JsonIgnore]
    public string FunctionName { get; set; } = string.Empty;

    /// <summary>
    /// The function qualifier (version or alias). Optional.
    /// </summary>
    [JsonIgnore]
    public string? Qualifier { get; set; }

    /// <summary>
    /// Client context information to validate. Optional.
    /// </summary>
    [JsonIgnore]
    public string? ClientContext { get; set; }

    /// <summary>
    /// The JSON payload to validate.
    /// </summary>
    public string? Payload { get; set; }
}