using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.DeleteItem;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.Transactions;
using Goa.Clients.Dynamo.Operations.UpdateItem;
using System.Text.Json.Serialization;

namespace Goa.Clients.Dynamo.Serialization;

/// <summary>
/// JSON source generator context for all DynamoDB types to enable AOT compilation and improved performance.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    GenerationMode = JsonSourceGenerationMode.Default,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AttributeValue))]
[JsonSerializable(typeof(DynamoRecord))]
[JsonSerializable(typeof(GetItemRequest))]
[JsonSerializable(typeof(GetItemResponse))]
[JsonSerializable(typeof(PutItemRequest))]
[JsonSerializable(typeof(PutItemResponse))]
[JsonSerializable(typeof(UpdateItemRequest))]
[JsonSerializable(typeof(UpdateItemResponse))]
[JsonSerializable(typeof(DeleteItemRequest))]
[JsonSerializable(typeof(DeleteItemResponse))]
[JsonSerializable(typeof(QueryRequest))]
[JsonSerializable(typeof(QueryResponse))]
[JsonSerializable(typeof(ScanRequest))]
[JsonSerializable(typeof(ScanResponse))]
[JsonSerializable(typeof(BatchGetItemRequest))]
[JsonSerializable(typeof(BatchGetItemResponse))]
[JsonSerializable(typeof(BatchGetRequestItem))]
[JsonSerializable(typeof(BatchWriteItemRequest))]
[JsonSerializable(typeof(BatchWriteItemResponse))]
[JsonSerializable(typeof(BatchWriteRequestItem))]
[JsonSerializable(typeof(PutRequest))]
[JsonSerializable(typeof(DeleteRequest))]
[JsonSerializable(typeof(TransactWriteRequest))]
[JsonSerializable(typeof(TransactWriteItemResponse))]
[JsonSerializable(typeof(TransactWriteItem))]
[JsonSerializable(typeof(TransactPutItem))]
[JsonSerializable(typeof(TransactUpdateItem))]
[JsonSerializable(typeof(TransactDeleteItem))]
[JsonSerializable(typeof(TransactConditionCheckItem))]
[JsonSerializable(typeof(TransactGetRequest))]
[JsonSerializable(typeof(TransactGetItemResponse))]
[JsonSerializable(typeof(TransactGetItem))]
[JsonSerializable(typeof(TransactGetItemRequest))]
[JsonSerializable(typeof(TransactGetResult))]
[JsonSerializable(typeof(ConsumedCapacity))]
[JsonSerializable(typeof(CapacityDetail))]
[JsonSerializable(typeof(ReturnConsumedCapacity))]
[JsonSerializable(typeof(ReturnValues))]
[JsonSerializable(typeof(ReturnItemCollectionMetrics))]
[JsonSerializable(typeof(Select))]
[JsonSerializable(typeof(Dictionary<string, AttributeValue>))]
[JsonSerializable(typeof(List<AttributeValue>))]
[JsonSerializable(typeof(List<DynamoRecord>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<ConsumedCapacity>))]
public partial class DynamoJsonContext : JsonSerializerContext
{
}
