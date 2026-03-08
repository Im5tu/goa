using System.Text.Json;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.Transactions;

namespace Goa.Clients.Dynamo.Internal;

internal static class DynamoResponseReader
{
    public static QueryResult<T> ReadQueryResponse<T>(ReadOnlySpan<byte> utf8Json, DynamoItemReader<T> itemReader)
    {
        var reader = new Utf8JsonReader(utf8Json);
        return ReadQueryResponse(ref reader, itemReader);
    }

    public static QueryResult<T> ReadQueryResponse<T>(ref Utf8JsonReader reader, DynamoItemReader<T> itemReader)
    {
        var result = new QueryResult<T>();

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals("Items"u8))
            {
                reader.Read();
                result.Items = ReadItems(ref reader, itemReader);
            }
            else if (reader.ValueTextEquals("Count"u8))
            {
                reader.Read();
                // Count is derived from Items.Count, skip
                reader.GetInt32();
            }
            else if (reader.ValueTextEquals("ScannedCount"u8))
            {
                reader.Read();
                result.ScannedCount = reader.GetInt32();
            }
            else if (reader.ValueTextEquals("LastEvaluatedKey"u8))
            {
                reader.Read();
                result.LastEvaluatedKey = ReadAttributeMap(ref reader);
            }
            else if (reader.ValueTextEquals("ConsumedCapacity"u8))
            {
                reader.Read();
                result.ConsumedCapacity = ReadConsumedCapacity(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return result;
    }

    public static ScanResult<T> ReadScanResponse<T>(ReadOnlySpan<byte> utf8Json, DynamoItemReader<T> itemReader)
    {
        var reader = new Utf8JsonReader(utf8Json);
        return ReadScanResponse(ref reader, itemReader);
    }

    public static ScanResult<T> ReadScanResponse<T>(ref Utf8JsonReader reader, DynamoItemReader<T> itemReader)
    {
        var result = new ScanResult<T>();

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals("Items"u8))
            {
                reader.Read();
                result.Items = ReadItems(ref reader, itemReader);
            }
            else if (reader.ValueTextEquals("Count"u8))
            {
                reader.Read();
                reader.GetInt32();
            }
            else if (reader.ValueTextEquals("ScannedCount"u8))
            {
                reader.Read();
                result.ScannedCount = reader.GetInt32();
            }
            else if (reader.ValueTextEquals("LastEvaluatedKey"u8))
            {
                reader.Read();
                result.LastEvaluatedKey = ReadAttributeMap(ref reader);
            }
            else if (reader.ValueTextEquals("ConsumedCapacity"u8))
            {
                reader.Read();
                result.ConsumedCapacity = ReadConsumedCapacity(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return result;
    }

    public static T? ReadGetItemResponse<T>(ReadOnlySpan<byte> utf8Json, DynamoItemReader<T> itemReader)
    {
        var reader = new Utf8JsonReader(utf8Json);
        return ReadGetItemResponse(ref reader, itemReader);
    }

    public static T? ReadGetItemResponse<T>(ref Utf8JsonReader reader, DynamoItemReader<T> itemReader)
    {
        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals("Item"u8))
            {
                reader.Read();
                return itemReader(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return default;
    }

    public static DynamoRecord ReadDynamoRecordItem(ref Utf8JsonReader reader)
    {
        var record = new DynamoRecord();
        // reader must be at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var key = reader.GetString()!;
            reader.Read();
            record[key] = ReadAttributeValue(ref reader);
        }
        return record;
    }

    private static List<T> ReadItems<T>(ref Utf8JsonReader reader, DynamoItemReader<T> itemReader)
    {
        var items = new List<T>();
        if (reader.TokenType == JsonTokenType.Null)
            return items;
        // reader is at StartArray
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            // reader is at StartObject for each item
            items.Add(itemReader(ref reader));
        }
        return items;
    }

    private static Dictionary<string, AttributeValue> ReadAttributeMap(ref Utf8JsonReader reader)
    {
        var map = new Dictionary<string, AttributeValue>();
        // reader is at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var key = reader.GetString()!;
            reader.Read(); // StartObject (type wrapper)
            map[key] = ReadAttributeValue(ref reader);
        }
        return map;
    }

    private static AttributeValue ReadAttributeValue(ref Utf8JsonReader reader)
    {
        var attr = new AttributeValue();
        // reader is at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.ValueTextEquals("S"u8))
            {
                reader.Read();
                attr.S = reader.GetString();
            }
            else if (reader.ValueTextEquals("N"u8))
            {
                reader.Read();
                attr.N = reader.GetString();
            }
            else if (reader.ValueTextEquals("BOOL"u8))
            {
                reader.Read();
                attr.BOOL = reader.GetBoolean();
            }
            else if (reader.ValueTextEquals("NULL"u8))
            {
                reader.Read();
                attr.NULL = reader.GetBoolean();
            }
            else if (reader.ValueTextEquals("SS"u8))
            {
                reader.Read();
                attr.SS = ReadStringArray(ref reader);
            }
            else if (reader.ValueTextEquals("NS"u8))
            {
                reader.Read();
                attr.NS = ReadStringArray(ref reader);
            }
            else if (reader.ValueTextEquals("L"u8))
            {
                reader.Read();
                attr.L = ReadAttributeValueList(ref reader);
            }
            else if (reader.ValueTextEquals("M"u8))
            {
                reader.Read();
                attr.M = ReadAttributeMap(ref reader);
            }
            else if (reader.ValueTextEquals("B"u8) || reader.ValueTextEquals("BS"u8))
            {
                reader.Read();
                reader.Skip();
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }
        return attr;
    }

    private static List<string> ReadStringArray(ref Utf8JsonReader reader)
    {
        var list = new List<string>();
        // reader is at StartArray
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(reader.GetString()!);
        }
        return list;
    }

    private static List<AttributeValue> ReadAttributeValueList(ref Utf8JsonReader reader)
    {
        var list = new List<AttributeValue>();
        // reader is at StartArray
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            // Each element is an AttributeValue object like {"S": "hello"}
            list.Add(ReadAttributeValue(ref reader));
        }
        return list;
    }

    private static ConsumedCapacity ReadConsumedCapacity(ref Utf8JsonReader reader)
    {
        var capacity = new ConsumedCapacity();
        // reader is at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.ValueTextEquals("TableName"u8))
            {
                reader.Read();
                capacity.TableName = reader.GetString();
            }
            else if (reader.ValueTextEquals("CapacityUnits"u8))
            {
                reader.Read();
                capacity.CapacityUnits = reader.GetDouble();
            }
            else if (reader.ValueTextEquals("ReadCapacityUnits"u8))
            {
                reader.Read();
                capacity.ReadCapacityUnits = reader.GetDouble();
            }
            else if (reader.ValueTextEquals("WriteCapacityUnits"u8))
            {
                reader.Read();
                capacity.WriteCapacityUnits = reader.GetDouble();
            }
            else if (reader.ValueTextEquals("Table"u8))
            {
                reader.Read();
                capacity.Table = ReadCapacityDetail(ref reader);
            }
            else if (reader.ValueTextEquals("GlobalSecondaryIndexes"u8))
            {
                reader.Read();
                capacity.GlobalSecondaryIndexes = ReadCapacityDetailMap(ref reader);
            }
            else if (reader.ValueTextEquals("LocalSecondaryIndexes"u8))
            {
                reader.Read();
                capacity.LocalSecondaryIndexes = ReadCapacityDetailMap(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }
        return capacity;
    }

    private static CapacityDetail ReadCapacityDetail(ref Utf8JsonReader reader)
    {
        var detail = new CapacityDetail();
        // reader is at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.ValueTextEquals("CapacityUnits"u8))
            {
                reader.Read();
                detail.CapacityUnits = reader.GetDouble();
            }
            else if (reader.ValueTextEquals("ReadCapacityUnits"u8))
            {
                reader.Read();
                detail.ReadCapacityUnits = reader.GetDouble();
            }
            else if (reader.ValueTextEquals("WriteCapacityUnits"u8))
            {
                reader.Read();
                detail.WriteCapacityUnits = reader.GetDouble();
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }
        return detail;
    }

    private static Dictionary<string, ConsumedCapacity> ReadCapacityDetailMap(ref Utf8JsonReader reader)
    {
        var map = new Dictionary<string, ConsumedCapacity>();
        // reader is at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var key = reader.GetString()!;
            reader.Read(); // StartObject
            map[key] = ReadConsumedCapacity(ref reader);
        }
        return map;
    }

    public static BatchGetResult<T> ReadBatchGetItemResponse<T>(ReadOnlySpan<byte> utf8Json, DynamoItemReader<T> itemReader)
    {
        var reader = new Utf8JsonReader(utf8Json);
        return ReadBatchGetItemResponse(ref reader, itemReader);
    }

    public static BatchGetResult<T> ReadBatchGetItemResponse<T>(ref Utf8JsonReader reader, DynamoItemReader<T> itemReader)
    {
        var result = new BatchGetResult<T>();

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals("Responses"u8))
            {
                reader.Read();
                result.Responses = ReadTableResponses(ref reader, itemReader);
            }
            else if (reader.ValueTextEquals("UnprocessedKeys"u8))
            {
                reader.Read();
                result.UnprocessedKeys = ReadUnprocessedKeys(ref reader);
            }
            else if (reader.ValueTextEquals("ConsumedCapacity"u8))
            {
                reader.Read();
                result.ConsumedCapacity = ReadConsumedCapacityList(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return result;
    }

    public static TransactGetResult<T> ReadTransactGetItemResponse<T>(ReadOnlySpan<byte> utf8Json, DynamoItemReader<T> itemReader)
    {
        var reader = new Utf8JsonReader(utf8Json);
        return ReadTransactGetItemResponse(ref reader, itemReader);
    }

    public static TransactGetResult<T> ReadTransactGetItemResponse<T>(ref Utf8JsonReader reader, DynamoItemReader<T> itemReader)
    {
        var result = new TransactGetResult<T>();

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            if (reader.ValueTextEquals("Responses"u8))
            {
                reader.Read();
                result.Items = ReadTransactGetResponses(ref reader, itemReader);
            }
            else if (reader.ValueTextEquals("ConsumedCapacity"u8))
            {
                reader.Read();
                result.ConsumedCapacity = ReadConsumedCapacityList(ref reader);
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }

        return result;
    }

    private static Dictionary<string, List<T>> ReadTableResponses<T>(ref Utf8JsonReader reader, DynamoItemReader<T> itemReader)
    {
        var responses = new Dictionary<string, List<T>>();
        // reader is at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var tableName = reader.GetString()!;
            reader.Read(); // StartArray
            responses[tableName] = ReadItems(ref reader, itemReader);
        }
        return responses;
    }

    private static List<ConsumedCapacity> ReadConsumedCapacityList(ref Utf8JsonReader reader)
    {
        var list = new List<ConsumedCapacity>();
        // reader is at StartArray
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            list.Add(ReadConsumedCapacity(ref reader));
        }
        return list;
    }

    private static Dictionary<string, BatchGetRequestItem> ReadUnprocessedKeys(ref Utf8JsonReader reader)
    {
        var map = new Dictionary<string, BatchGetRequestItem>();
        // reader is at StartObject (or empty object)
        if (reader.TokenType == JsonTokenType.Null)
            return map;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var tableName = reader.GetString()!;
            reader.Read(); // StartObject of BatchGetRequestItem
            map[tableName] = ReadBatchGetRequestItem(ref reader);
        }
        return map;
    }

    private static BatchGetRequestItem ReadBatchGetRequestItem(ref Utf8JsonReader reader)
    {
        var item = new BatchGetRequestItem();
        // reader is at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.ValueTextEquals("Keys"u8))
            {
                reader.Read(); // StartArray
                var keys = new List<Dictionary<string, AttributeValue>>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    keys.Add(ReadAttributeMap(ref reader));
                }
                item.Keys = keys;
            }
            else if (reader.ValueTextEquals("ProjectionExpression"u8))
            {
                reader.Read();
                item.ProjectionExpression = reader.GetString();
            }
            else if (reader.ValueTextEquals("ConsistentRead"u8))
            {
                reader.Read();
                item.ConsistentRead = reader.GetBoolean();
            }
            else if (reader.ValueTextEquals("ExpressionAttributeNames"u8))
            {
                reader.Read();
                var names = new Dictionary<string, string>();
                while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                {
                    var key = reader.GetString()!;
                    reader.Read();
                    names[key] = reader.GetString()!;
                }
                item.ExpressionAttributeNames = names;
            }
            else
            {
                reader.Read();
                reader.Skip();
            }
        }
        return item;
    }

    private static List<T?> ReadTransactGetResponses<T>(ref Utf8JsonReader reader, DynamoItemReader<T> itemReader)
    {
        var items = new List<T?>();
        // reader is at StartArray
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            // Each element is an object like {"Item": {...}}
            T? item = default;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                var propName = reader.GetString();
                reader.Read();
                if (propName == "Item")
                {
                    item = itemReader(ref reader);
                }
                else
                {
                    reader.Skip();
                }
            }
            items.Add(item);
        }
        return items;
    }
}
