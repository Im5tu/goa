using System.Text.Json;
using System.Text.Json.Serialization;

namespace Goa.Functions.Dynamo;

/// <summary>
/// JSON serialization context for DynamoDB events with source generation support
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.General, UseStringEnumConverter = true)]
[JsonSerializable(typeof(DynamoDbEvent))]
public partial class DynamoDbEventSerializationContext : JsonSerializerContext
{
}
