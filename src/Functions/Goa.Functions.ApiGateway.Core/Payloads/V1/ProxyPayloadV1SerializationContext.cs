using System.Text.Json.Serialization;

namespace Goa.Functions.ApiGateway.Core.Payloads.V1;

/// <summary>
///     System.Text.Json.Serialization.JsonSerializationContext for ProxyPayloadV1
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProxyPayloadV1Request))]
[JsonSerializable(typeof(ProxyPayloadV1Response))]
public partial class ProxyPayloadV1SerializationContext : JsonSerializerContext;
