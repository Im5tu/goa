using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Logging;

[JsonSourceGenerationOptions(WriteIndented = false,
    UseStringEnumConverter = true,
    DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
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
internal partial class LoggingSerializationContext : JsonSerializerContext;
