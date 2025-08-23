using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// JSON serialization context for S3 events with source generation support
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.General, UseStringEnumConverter = true)]
[JsonSerializable(typeof(S3Event))]
public partial class S3EventSerializationContext : JsonSerializerContext
{
}