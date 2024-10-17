using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway;

/// <summary>
///     Custom JSON converter to handle the deserialization and serialization of a space-delimited string into a list of strings.
/// </summary>
public class SpaceDelimitedStringConverter : JsonConverter<List<string>>
{
    /// <inheritDoc />
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value?.Split(' ')?.ToList() ?? new List<string>();
    }

    /// <inheritDoc />
    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(string.Join(' ', value));
    }
}