using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Operations.ApplyGuardrail;

namespace Goa.Clients.Bedrock.Serialization;

/// <summary>
/// Custom JSON converter for GuardrailTextQualifier that serializes to snake_case
/// values as required by the Bedrock API.
/// </summary>
public sealed class GuardrailTextQualifierConverter : JsonConverter<GuardrailTextQualifier>
{
    /// <inheritdoc />
    public override GuardrailTextQualifier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "grounding_source" => GuardrailTextQualifier.GroundingSource,
            "query" => GuardrailTextQualifier.Query,
            "guard_content" => GuardrailTextQualifier.GuardContent,
            _ => throw new JsonException($"Unknown GuardrailTextQualifier: {value}")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, GuardrailTextQualifier value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            GuardrailTextQualifier.GroundingSource => "grounding_source",
            GuardrailTextQualifier.Query => "query",
            GuardrailTextQualifier.GuardContent => "guard_content",
            _ => throw new JsonException($"Unknown GuardrailTextQualifier: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}
