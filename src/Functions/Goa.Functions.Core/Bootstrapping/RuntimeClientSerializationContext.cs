using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Bootstrapping;

[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(InvocationErrorPayload))]
[JsonSerializable(typeof(InitializationErrorPayload))]
internal partial class RuntimeClientSerializationContext : JsonSerializerContext
{
}
