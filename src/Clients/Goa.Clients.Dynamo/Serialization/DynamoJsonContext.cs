using Goa.Json.Generator;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.DeleteItem;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.Transactions;
using Goa.Clients.Dynamo.Operations.UpdateItem;

namespace Goa.Clients.Dynamo.Serialization;

[JsonGenerator(typeof(GetItemRequest))]
[JsonGenerator(typeof(GetItemResponse))]
[JsonGenerator(typeof(PutItemRequest))]
[JsonGenerator(typeof(PutItemResponse))]
[JsonGenerator(typeof(UpdateItemRequest))]
[JsonGenerator(typeof(UpdateItemResponse))]
[JsonGenerator(typeof(DeleteItemRequest))]
[JsonGenerator(typeof(DeleteItemResponse))]
[JsonGenerator(typeof(QueryRequest))]
[JsonGenerator(typeof(QueryResponse))]
[JsonGenerator(typeof(ScanRequest))]
[JsonGenerator(typeof(ScanResponse))]
[JsonGenerator(typeof(BatchGetItemRequest))]
[JsonGenerator(typeof(BatchGetItemResponse))]
[JsonGenerator(typeof(BatchWriteItemRequest))]
[JsonGenerator(typeof(BatchWriteItemResponse))]
[JsonGenerator(typeof(TransactWriteRequest))]
[JsonGenerator(typeof(TransactWriteItemResponse))]
[JsonGenerator(typeof(TransactGetRequest))]
[JsonGenerator(typeof(TransactGetItemResponse))]
internal partial class DynamoJsonContext
{
}
