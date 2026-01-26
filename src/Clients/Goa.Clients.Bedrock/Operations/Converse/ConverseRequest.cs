using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Operations.Converse;

/// <summary>
/// Request for the Bedrock Converse API.
/// </summary>
public class ConverseRequest
{
    /// <summary>
    /// The identifier of the model to use.
    /// </summary>
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// The conversation messages.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<Message> Messages { get; set; } = new();

    /// <summary>
    /// System prompts to provide context to the model.
    /// </summary>
    [JsonPropertyName("system")]
    public List<SystemContentBlock>? System { get; set; }

    /// <summary>
    /// Inference configuration parameters.
    /// </summary>
    [JsonPropertyName("inferenceConfig")]
    public InferenceConfiguration? InferenceConfig { get; set; }

    /// <summary>
    /// Tool configuration for function calling.
    /// </summary>
    [JsonPropertyName("toolConfig")]
    public ToolConfiguration? ToolConfig { get; set; }

    /// <summary>
    /// Guardrail configuration.
    /// </summary>
    [JsonPropertyName("guardrailConfig")]
    public GuardrailConfiguration? GuardrailConfig { get; set; }

    /// <summary>
    /// Additional model-specific request fields.
    /// </summary>
    [JsonPropertyName("additionalModelRequestFields")]
    public JsonElement? AdditionalModelRequestFields { get; set; }

    /// <summary>
    /// Performance configuration.
    /// </summary>
    [JsonPropertyName("performanceConfig")]
    public PerformanceConfiguration? PerformanceConfig { get; set; }

    /// <summary>
    /// The service tier for request processing.
    /// </summary>
    [JsonPropertyName("requestMetadata")]
    public RequestMetadata? RequestMetadata { get; set; }
}

/// <summary>
/// A system content block for providing context.
/// </summary>
public class SystemContentBlock
{
    /// <summary>
    /// Text content for the system prompt.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// Request metadata including service tier.
/// </summary>
public class RequestMetadata
{
    /// <summary>
    /// The service tier for request processing.
    /// </summary>
    [JsonPropertyName("serviceTier")]
    public ServiceTier? ServiceTier { get; set; }
}
