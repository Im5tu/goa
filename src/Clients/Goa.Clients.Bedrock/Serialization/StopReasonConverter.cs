using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Enums;

namespace Goa.Clients.Bedrock.Serialization;

/// <summary>
/// Custom JSON converter for StopReason that serializes to snake_case
/// values as required by the Bedrock API.
/// </summary>
public sealed class StopReasonConverter : JsonConverter<StopReason>
{
    /// <inheritdoc />
    public override StopReason Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "end_turn" => StopReason.EndTurn,
            "tool_use" => StopReason.ToolUse,
            "max_tokens" => StopReason.MaxTokens,
            "stop_sequence" => StopReason.StopSequence,
            "guardrail_intervened" => StopReason.GuardrailIntervened,
            "content_filtered" => StopReason.ContentFiltered,
            "model_context_window_exceeded" => StopReason.ModelContextWindowExceeded,
            _ => throw new JsonException($"Unknown StopReason: {value}")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, StopReason value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            StopReason.EndTurn => "end_turn",
            StopReason.ToolUse => "tool_use",
            StopReason.MaxTokens => "max_tokens",
            StopReason.StopSequence => "stop_sequence",
            StopReason.GuardrailIntervened => "guardrail_intervened",
            StopReason.ContentFiltered => "content_filtered",
            StopReason.ModelContextWindowExceeded => "model_context_window_exceeded",
            _ => throw new JsonException($"Unknown StopReason: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}
