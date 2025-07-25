using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.AspNetCore;

internal sealed class DictionaryJsonConverter : JsonConverter<IDictionary<string, object>>
{
    public override IDictionary<string, object>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException("Deserialization of IDictionary<string, object> is not supported.");
    }

    public override void Write(Utf8JsonWriter writer, IDictionary<string, object> dictionary, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var (key, value) in dictionary)
        {
            writer.WritePropertyName(key);

            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                // Handle specific types explicitly
                switch (value)
                {
                    case RouteEndpoint routeEndpoint:
                        JsonSerializer.Serialize(writer, routeEndpoint, LoggingSerializationContext.Default.RouteEndpoint);
                        break;

                    default:
                        // Fallback to default serialization
                        JsonSerializer.Serialize(writer, value, LoggingSerializationContext.Default.GetTypeInfo(value.GetType()) ?? throw new JsonException($"type: {value.GetType()} is not supported by {nameof(LoggingSerializationContext)}."));
                        break;
                }
            }
        }

        writer.WriteEndObject();
    }
}