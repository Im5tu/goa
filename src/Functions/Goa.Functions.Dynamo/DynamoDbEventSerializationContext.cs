using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Functions.Core;

namespace Goa.Functions.Dynamo;

/// <summary>
/// JSON serialization context for DynamoDB events with source generation support
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.General, UseStringEnumConverter = true)]
[JsonSerializable(typeof(DynamoDbEvent))]
[JsonSerializable(typeof(BatchItemFailureResponse))]
[JsonSerializable(typeof(BatchItemFailure))]
[JsonSerializable(typeof(List<BatchItemFailure>))]
public partial class DynamoDbEventSerializationContext : JsonSerializerContext
{
}
