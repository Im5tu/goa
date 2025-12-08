using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.Core.Generic;

/// <summary>
/// JSON serialization context for string payloads used by the string handler
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.General)]
[JsonSerializable(typeof(string))]
internal partial class StringSerializationContext : JsonSerializerContext
{
}
