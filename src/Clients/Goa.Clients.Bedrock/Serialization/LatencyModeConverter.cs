using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Enums;
using Goa.Clients.Core;

namespace Goa.Clients.Bedrock.Serialization;

/// <summary>
/// Custom JSON converter for LatencyMode that serializes to lowercase
/// values as required by the Bedrock API.
/// </summary>
public sealed class LatencyModeConverter : JsonConverter<LatencyMode>
{
    /// <inheritdoc />
    public override LatencyMode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "standard" => LatencyMode.Standard,
            "optimized" => LatencyMode.Optimized,
            _ => Throw.JsonException<LatencyMode>($"Unknown LatencyMode: {value}")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, LatencyMode value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            LatencyMode.Standard => "standard",
            LatencyMode.Optimized => "optimized",
            _ => Throw.JsonException<string>($"Unknown LatencyMode: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}
