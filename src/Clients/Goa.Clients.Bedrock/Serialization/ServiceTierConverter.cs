using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Bedrock.Models;

namespace Goa.Clients.Bedrock.Serialization;

/// <summary>
/// Custom JSON converter for ServiceTier that serializes to lowercase
/// values as required by the Bedrock API.
/// </summary>
public sealed class ServiceTierConverter : JsonConverter<ServiceTier>
{
    /// <inheritdoc />
    public override ServiceTier Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value switch
        {
            "default" => ServiceTier.Default,
            "priority" => ServiceTier.Priority,
            "flex" => ServiceTier.Flex,
            _ => throw new JsonException($"Unknown ServiceTier: {value}")
        };
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ServiceTier value, JsonSerializerOptions options)
    {
        var stringValue = value switch
        {
            ServiceTier.Default => "default",
            ServiceTier.Priority => "priority",
            ServiceTier.Flex => "flex",
            _ => throw new JsonException($"Unknown ServiceTier: {value}")
        };
        writer.WriteStringValue(stringValue);
    }
}
