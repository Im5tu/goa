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
#pragma warning disable GOA1503 // One-time method group allocations cached in static readonly fields
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
#pragma warning restore GOA1503
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
        return DynamoResponseReader.ReadDynamoRecordQueryResponse(ref reader);
    }

    private static ScanResponse ReadScanResponse(ref Utf8JsonReader reader)
    {
        return DynamoResponseReader.ReadDynamoRecordScanResponse(ref reader);
    }

    private static GetItemResponse ReadGetItemResponse(ref Utf8JsonReader reader)
    {
        return DynamoResponseReader.ReadDynamoRecordGetItemResponse(ref reader);
    }

    private static BatchGetItemResponse ReadBatchGetItemResponse(ref Utf8JsonReader reader)
    {
        return DynamoResponseReader.ReadDynamoRecordBatchGetItemResponse(ref reader);
    }

    private static TransactGetItemResponse ReadTransactGetItemResponse(ref Utf8JsonReader reader)
    {
        return DynamoResponseReader.ReadDynamoRecordTransactGetItemResponse(ref reader);
    }
}
