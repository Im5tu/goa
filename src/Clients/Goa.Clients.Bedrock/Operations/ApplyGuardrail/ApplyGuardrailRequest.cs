using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Serialization;

namespace Goa.Clients.Bedrock.Operations.ApplyGuardrail;

/// <summary>
/// Request for the Bedrock ApplyGuardrail API.
/// </summary>
public sealed class ApplyGuardrailRequest
{
    /// <summary>
    /// The identifier of the guardrail to apply.
    /// </summary>
    [JsonPropertyName("guardrailIdentifier")]
    public required string GuardrailIdentifier { get; init; }

    /// <summary>
    /// The version of the guardrail to apply.
    /// </summary>
    [JsonPropertyName("guardrailVersion")]
    public required string GuardrailVersion { get; init; }

    /// <summary>
    /// The source of the content. Valid values are "INPUT" or "OUTPUT".
    /// </summary>
    [JsonPropertyName("source")]
    public required string Source { get; init; }

    /// <summary>
    /// The content to apply the guardrail to.
    /// </summary>
    [JsonPropertyName("content")]
    public required List<GuardrailContentBlock> Content { get; init; }
}

/// <summary>
/// A content block for guardrail evaluation.
/// </summary>
public sealed class GuardrailContentBlock
{
    /// <summary>
    /// Text content block for guardrail evaluation.
    /// </summary>
    [JsonPropertyName("text")]
    public GuardrailTextBlock? Text { get; init; }
}

/// <summary>
/// A text block for guardrail evaluation.
/// </summary>
public sealed class GuardrailTextBlock
{
    /// <summary>
    /// The text content to evaluate.
    /// </summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>
    /// Optional qualifiers for the text content.
    /// </summary>
    [JsonPropertyName("qualifiers")]
    public List<GuardrailTextQualifier>? Qualifiers { get; init; }
}

/// <summary>
/// Qualifiers for guardrail text content.
/// </summary>
[JsonConverter(typeof(GuardrailTextQualifierConverter))]
public enum GuardrailTextQualifier
{
    /// <summary>
    /// Indicates the text is a grounding source.
    /// </summary>
    GroundingSource,

    /// <summary>
    /// Indicates the text is a query.
    /// </summary>
    Query,

    /// <summary>
    /// Indicates the text is content to guard.
    /// </summary>
    GuardContent
}
