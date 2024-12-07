using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.AspNetCore;

[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters = [typeof(DictionaryJsonConverter), typeof(RouteEndpointJsonConverter)]
)]
[JsonSerializable(typeof(IDictionary<string, object>))]
[JsonSerializable(typeof(IDictionary<string, string>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(short))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(HostString))]
[JsonSerializable(typeof(PathString))]
[JsonSerializable(typeof(RouteEndpoint))]
internal partial class LoggingSerializationContext : JsonSerializerContext;

internal sealed class RouteEndpointJsonConverter : JsonConverter<RouteEndpoint>
{
    public override RouteEndpoint? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, RouteEndpoint value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("order", value.Order);

        if (!string.IsNullOrEmpty(value.DisplayName))
        {
            writer.WritePropertyName("displayName");
            writer.WriteStringValue(value.DisplayName);
        }
        writer.WriteEndObject();
    }
}

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
