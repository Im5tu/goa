using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway;

[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProxyPayloadV1Request))]
[JsonSerializable(typeof(ProxyPayloadV1Response))]
internal partial class ProxyPayloadV1SerializationContext : JsonSerializerContext;