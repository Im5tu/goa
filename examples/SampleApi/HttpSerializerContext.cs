using System.Text.Json.Serialization;

namespace SampleApi;

[JsonSourceGenerationOptions(WriteIndented = false, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(Pong))]
public partial class HttpSerializerContext : JsonSerializerContext
{
}