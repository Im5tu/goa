using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.AspNetCore;

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