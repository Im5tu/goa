using System.Text.Json;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.Transactions;

namespace Goa.Clients.Dynamo.Internal;

/// <summary>
/// Delegate that reads a value from a UTF-8 JSON reader.
/// </summary>
internal delegate T JsonReader<T>(ref Utf8JsonReader reader);

/// <summary>
/// Type registry for DynamoDB response deserializers. Provides custom readers for
/// DynamoRecord-based response types that handle DynamoDB's type-wrapped wire format.
/// </summary>
internal static class DynamoResponseReaderRegistry
{
    /// <summary>
    /// Gets a reader for the given response type.
    /// </summary>
    public static JsonReader<T> GetReader<T>() =>
        Cache<T>.Reader;

    private static class Cache<T>
    {
        public static readonly JsonReader<T> Reader;

        static Cache()
        {
            if (typeof(T) == typeof(QueryResponse))
                Reader = (JsonReader<T>)(object)(JsonReader<QueryResponse>)ReadQueryResponse;
            else if (typeof(T) == typeof(ScanResponse))
                Reader = (JsonReader<T>)(object)(JsonReader<ScanResponse>)ReadScanResponse;
            else if (typeof(T) == typeof(GetItemResponse))
                Reader = (JsonReader<T>)(object)(JsonReader<GetItemResponse>)ReadGetItemResponse;
            else if (typeof(T) == typeof(BatchGetItemResponse))
                Reader = (JsonReader<T>)(object)(JsonReader<BatchGetItemResponse>)ReadBatchGetItemResponse;
            else if (typeof(T) == typeof(TransactGetItemResponse))
                Reader = (JsonReader<T>)(object)(JsonReader<TransactGetItemResponse>)ReadTransactGetItemResponse;
            else
                Reader = FallbackReader;
        }

        private static T FallbackReader(ref Utf8JsonReader reader)
        {
            var typeInfo = Serialization.DynamoJsonContext.Default.GetTypeInfo(typeof(T))
                as System.Text.Json.Serialization.Metadata.JsonTypeInfo<T>
                ?? throw new InvalidOperationException($"Cannot find type {typeof(T).Name} in serialization context");
            return JsonSerializer.Deserialize(ref reader, typeInfo)!;
        }
    }

    private static QueryResponse ReadQueryResponse(ref Utf8JsonReader reader)
    {
        var result = DynamoResponseReader.ReadQueryResponse(ref reader, DynamoResponseReader.ReadDynamoRecordItem);
        return new QueryResponse
        {
            Items = result.Items,
            LastEvaluatedKey = result.LastEvaluatedKey,
            ScannedCount = result.ScannedCount,
            ConsumedCapacity = result.ConsumedCapacity
        };
    }

    private static ScanResponse ReadScanResponse(ref Utf8JsonReader reader)
    {
        var result = DynamoResponseReader.ReadScanResponse(ref reader, DynamoResponseReader.ReadDynamoRecordItem);
        return new ScanResponse
        {
            Items = result.Items,
            LastEvaluatedKey = result.LastEvaluatedKey,
            ScannedCount = result.ScannedCount,
            ConsumedCapacity = result.ConsumedCapacity
        };
    }

    private static GetItemResponse ReadGetItemResponse(ref Utf8JsonReader reader)
    {
        var item = DynamoResponseReader.ReadGetItemResponse(ref reader, DynamoResponseReader.ReadDynamoRecordItem);
        return new GetItemResponse { Item = item };
    }

    private static BatchGetItemResponse ReadBatchGetItemResponse(ref Utf8JsonReader reader)
    {
        var result = DynamoResponseReader.ReadBatchGetItemResponse(ref reader, DynamoResponseReader.ReadDynamoRecordItem);
        return new BatchGetItemResponse
        {
            Responses = result.Responses,
            UnprocessedKeys = result.UnprocessedKeys,
            ConsumedCapacity = result.ConsumedCapacity
        };
    }

    private static TransactGetItemResponse ReadTransactGetItemResponse(ref Utf8JsonReader reader)
    {
        var result = DynamoResponseReader.ReadTransactGetItemResponse(ref reader, DynamoResponseReader.ReadDynamoRecordItem);
        var responses = new List<TransactGetResult>(result.Items.Count);
        foreach (var item in result.Items)
            responses.Add(new TransactGetResult { Item = item });
        return new TransactGetItemResponse
        {
            Responses = responses,
            ConsumedCapacity = result.ConsumedCapacity
        };
    }
}
