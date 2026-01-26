namespace Goa.Clients.Bedrock.Models;

/// <summary>
/// Configuration for guardrails to apply to the conversation.
/// </summary>
public class GuardrailConfiguration
{
    /// <summary>
    /// The identifier of the guardrail to apply.
    /// </summary>
    public string GuardrailIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The version of the guardrail to use.
    /// </summary>
    public string GuardrailVersion { get; set; } = string.Empty;

    /// <summary>
    /// Trace configuration for guardrail processing (e.g., "enabled", "disabled").
    /// </summary>
    public string? Trace { get; set; }
}
