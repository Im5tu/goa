using System.Text.Json.Serialization;

namespace Goa.Clients.Bedrock.Operations.ApplyGuardrail;

/// <summary>
/// Response from the Bedrock ApplyGuardrail API.
/// </summary>
public sealed class ApplyGuardrailResponse
{
    /// <summary>
    /// The action taken by the guardrail. "NONE" or "GUARDRAIL_INTERVENED".
    /// </summary>
    [JsonPropertyName("action")]
    public required string Action { get; init; }

    /// <summary>
    /// The outputs from the guardrail evaluation.
    /// </summary>
    [JsonPropertyName("outputs")]
    public required List<GuardrailOutputContent> Outputs { get; init; }

    /// <summary>
    /// The assessments performed by the guardrail.
    /// </summary>
    [JsonPropertyName("assessments")]
    public List<GuardrailAssessment>? Assessments { get; init; }

    /// <summary>
    /// Usage information for the guardrail evaluation.
    /// </summary>
    [JsonPropertyName("usage")]
    public GuardrailUsage? Usage { get; init; }
}

/// <summary>
/// Output content from guardrail evaluation.
/// </summary>
public sealed class GuardrailOutputContent
{
    /// <summary>
    /// The text output from the guardrail.
    /// </summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }
}

/// <summary>
/// Assessment results from guardrail evaluation.
/// </summary>
public sealed class GuardrailAssessment
{
    /// <summary>
    /// Topic policy assessment results.
    /// </summary>
    [JsonPropertyName("topicPolicy")]
    public GuardrailTopicPolicyAssessment? TopicPolicy { get; init; }

    /// <summary>
    /// Content policy assessment results.
    /// </summary>
    [JsonPropertyName("contentPolicy")]
    public GuardrailContentPolicyAssessment? ContentPolicy { get; init; }

    /// <summary>
    /// Word policy assessment results.
    /// </summary>
    [JsonPropertyName("wordPolicy")]
    public GuardrailWordPolicyAssessment? WordPolicy { get; init; }

    /// <summary>
    /// Sensitive information policy assessment results.
    /// </summary>
    [JsonPropertyName("sensitiveInformationPolicy")]
    public GuardrailSensitiveInformationPolicyAssessment? SensitiveInformationPolicy { get; init; }
}

/// <summary>
/// Topic policy assessment results.
/// </summary>
public sealed class GuardrailTopicPolicyAssessment
{
    /// <summary>
    /// The topics detected during evaluation.
    /// </summary>
    [JsonPropertyName("topics")]
    public List<GuardrailTopic>? Topics { get; init; }
}

/// <summary>
/// A detected topic from guardrail evaluation.
/// </summary>
public sealed class GuardrailTopic
{
    /// <summary>
    /// The name of the topic.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The type of the topic.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// The action taken for this topic.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }
}

/// <summary>
/// Content policy assessment results.
/// </summary>
public sealed class GuardrailContentPolicyAssessment
{
    /// <summary>
    /// The content filters applied during evaluation.
    /// </summary>
    [JsonPropertyName("filters")]
    public List<GuardrailContentFilter>? Filters { get; init; }
}

/// <summary>
/// A content filter result from guardrail evaluation.
/// </summary>
public sealed class GuardrailContentFilter
{
    /// <summary>
    /// The type of content filter.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// The confidence level of the detection.
    /// </summary>
    [JsonPropertyName("confidence")]
    public string? Confidence { get; init; }

    /// <summary>
    /// The action taken for this filter.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }
}

/// <summary>
/// Word policy assessment results.
/// </summary>
public sealed class GuardrailWordPolicyAssessment
{
    /// <summary>
    /// Custom words detected during evaluation.
    /// </summary>
    [JsonPropertyName("customWords")]
    public List<GuardrailCustomWord>? CustomWords { get; init; }

    /// <summary>
    /// Managed word lists detected during evaluation.
    /// </summary>
    [JsonPropertyName("managedWordLists")]
    public List<GuardrailManagedWord>? ManagedWordLists { get; init; }
}

/// <summary>
/// A custom word detected from guardrail evaluation.
/// </summary>
public sealed class GuardrailCustomWord
{
    /// <summary>
    /// The matched word.
    /// </summary>
    [JsonPropertyName("match")]
    public string? Match { get; init; }

    /// <summary>
    /// The action taken for this word.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }
}

/// <summary>
/// A managed word detected from guardrail evaluation.
/// </summary>
public sealed class GuardrailManagedWord
{
    /// <summary>
    /// The matched word.
    /// </summary>
    [JsonPropertyName("match")]
    public string? Match { get; init; }

    /// <summary>
    /// The type of managed word list.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// The action taken for this word.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }
}

/// <summary>
/// Sensitive information policy assessment results.
/// </summary>
public sealed class GuardrailSensitiveInformationPolicyAssessment
{
    /// <summary>
    /// PII entities detected during evaluation.
    /// </summary>
    [JsonPropertyName("piiEntities")]
    public List<GuardrailPiiEntity>? PiiEntities { get; init; }

    /// <summary>
    /// Regex matches detected during evaluation.
    /// </summary>
    [JsonPropertyName("regexes")]
    public List<GuardrailRegexEntity>? Regexes { get; init; }
}

/// <summary>
/// A PII entity detected from guardrail evaluation.
/// </summary>
public sealed class GuardrailPiiEntity
{
    /// <summary>
    /// The type of PII detected.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>
    /// The matched content.
    /// </summary>
    [JsonPropertyName("match")]
    public string? Match { get; init; }

    /// <summary>
    /// The action taken for this entity.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }
}

/// <summary>
/// A regex entity detected from guardrail evaluation.
/// </summary>
public sealed class GuardrailRegexEntity
{
    /// <summary>
    /// The name of the regex pattern.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// The matched content.
    /// </summary>
    [JsonPropertyName("match")]
    public string? Match { get; init; }

    /// <summary>
    /// The action taken for this match.
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; init; }
}

/// <summary>
/// Usage information for guardrail evaluation.
/// </summary>
public sealed class GuardrailUsage
{
    /// <summary>
    /// The number of topic policy units consumed.
    /// </summary>
    [JsonPropertyName("topicPolicyUnits")]
    public int TopicPolicyUnits { get; init; }

    /// <summary>
    /// The number of content policy units consumed.
    /// </summary>
    [JsonPropertyName("contentPolicyUnits")]
    public int ContentPolicyUnits { get; init; }

    /// <summary>
    /// The number of word policy units consumed.
    /// </summary>
    [JsonPropertyName("wordPolicyUnits")]
    public int WordPolicyUnits { get; init; }

    /// <summary>
    /// The number of sensitive information policy units consumed.
    /// </summary>
    [JsonPropertyName("sensitiveInformationPolicyUnits")]
    public int SensitiveInformationPolicyUnits { get; init; }

    /// <summary>
    /// The number of free sensitive information policy units consumed.
    /// </summary>
    [JsonPropertyName("sensitiveInformationPolicyFreeUnits")]
    public int SensitiveInformationPolicyFreeUnits { get; init; }
}
