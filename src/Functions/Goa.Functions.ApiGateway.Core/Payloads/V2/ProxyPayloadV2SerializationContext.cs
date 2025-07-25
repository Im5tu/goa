using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Core.Payloads.V2;

/// <summary>
///     System.Text.Json.Serialization.JsonSerializationContext for ProxyPayloadV2
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProxyPayloadV2Request))]
[JsonSerializable(typeof(ProxyPayloadV2Response))]
public partial class ProxyPayloadV2SerializationContext : JsonSerializerContext;
