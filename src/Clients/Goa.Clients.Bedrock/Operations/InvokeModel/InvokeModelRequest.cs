using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Operations.InvokeModel;

/// <summary>
/// Request for the Bedrock InvokeModel API.
/// </summary>
public sealed class InvokeModelRequest
{
    /// <summary>
    /// The identifier of the model to invoke.
    /// </summary>
    [JsonIgnore]
    public required string ModelId { get; init; }

    /// <summary>
    /// The raw JSON payload to send to the model (model-specific format).
    /// </summary>
    [JsonIgnore]
    public required string Body { get; init; }

    /// <summary>
    /// The MIME type of the input data in the request body.
    /// </summary>
    [JsonIgnore]
    public string ContentType { get; init; } = "application/json";

    /// <summary>
    /// The desired MIME type of the inference body in the response.
    /// </summary>
    [JsonIgnore]
    public string Accept { get; init; } = "application/json";

    /// <summary>
    /// The unique identifier for the guardrail to apply.
    /// </summary>
    [JsonIgnore]
    public string? GuardrailIdentifier { get; init; }

    /// <summary>
    /// The version of the guardrail to apply.
    /// </summary>
    [JsonIgnore]
    public string? GuardrailVersion { get; init; }

    /// <summary>
    /// The latency mode for model inference performance.
    /// </summary>
    [JsonIgnore]
    public LatencyMode? PerformanceConfigLatency { get; init; }

    /// <summary>
    /// The service tier for request processing.
    /// </summary>
    [JsonIgnore]
    public ServiceTier? ServiceTier { get; init; }
}
