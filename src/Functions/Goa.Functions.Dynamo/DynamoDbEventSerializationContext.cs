using System.Text.Json;
using System.Text.Json.Serialization;
using Goa.Clients.Dynamo.Models;
using Goa.Functions.Core;

namespace Goa.Functions.Dynamo;

/// <summary>
/// JSON serialization context for DynamoDB events with source generation support
/// </summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.General, UseStringEnumConverter = true)]
// Request types
[JsonSerializable(typeof(DynamoDbEvent))]
[JsonSerializable(typeof(IList<DynamoDbStreamRecord>))]
[JsonSerializable(typeof(DynamoDbStreamRecord))]
[JsonSerializable(typeof(StreamRecord))]
[JsonSerializable(typeof(Identity))]
// DynamoDB attribute types from Goa.Clients.Dynamo
[JsonSerializable(typeof(DynamoRecord))]
[JsonSerializable(typeof(AttributeValue))]
[JsonSerializable(typeof(Dictionary<string, AttributeValue>))]
[JsonSerializable(typeof(List<AttributeValue>))]
[JsonSerializable(typeof(List<string>))]
// Response types
[JsonSerializable(typeof(BatchItemFailureResponse))]
[JsonSerializable(typeof(BatchItemFailure))]
[JsonSerializable(typeof(List<BatchItemFailure>))]
public partial class DynamoDbEventSerializationContext : JsonSerializerContext
{
}
