using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Tests.Bootstrapping;

[JsonSourceGenerationOptions(WriteIndented = false, UseStringEnumConverter = true, DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Data))]
public partial class DataSerializationContext : JsonSerializerContext
{
}