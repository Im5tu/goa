using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Serialization;

namespace Goa.Clients.Bedrock.Enums;

/// <summary>
/// The reason why the model stopped generating output.
/// </summary>
[JsonConverter(typeof(StopReasonConverter))]
public enum StopReason
{
    /// <summary>
    /// The model reached a natural stopping point and finished the turn.
    /// </summary>
    EndTurn,

    /// <summary>
    /// The model is invoking a tool.
    /// </summary>
    ToolUse,

    /// <summary>
    /// The model reached the maximum number of tokens.
    /// </summary>
    MaxTokens,

    /// <summary>
    /// The model encountered a stop sequence.
    /// </summary>
    StopSequence,

    /// <summary>
    /// A guardrail intervened to stop the generation.
    /// </summary>
    GuardrailIntervened,

    /// <summary>
    /// Content was filtered by the model's content filter.
    /// </summary>
    ContentFiltered,

    /// <summary>
    /// The model exceeded the context window limit.
    /// </summary>
    ModelContextWindowExceeded
}
