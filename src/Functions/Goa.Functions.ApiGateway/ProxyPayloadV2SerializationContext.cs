using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway;

[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProxyPayloadV2Request))]
[JsonSerializable(typeof(ProxyPayloadV2Response))]
internal partial class ProxyPayloadV2SerializationContext : JsonSerializerContext;