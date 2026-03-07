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
        var result = new QueryResult<T>();
        var reader = new Utf8JsonReader(utf8Json);

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Items":
                    result.Items = ReadItems(ref reader, itemReader);
                    break;
                case "Count":
                    // Count is derived from Items.Count, skip
                    reader.GetInt32();
                    break;
                case "ScannedCount":
                    result.ScannedCount = reader.GetInt32();
                    break;
                case "LastEvaluatedKey":
                    result.LastEvaluatedKey = ReadAttributeMap(ref reader);
                    break;
                case "ConsumedCapacity":
                    result.ConsumedCapacity = ReadConsumedCapacity(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return result;
    }

    public static ScanResult<T> ReadScanResponse<T>(ReadOnlySpan<byte> utf8Json, DynamoItemReader<T> itemReader)
    {
        var result = new ScanResult<T>();
        var reader = new Utf8JsonReader(utf8Json);

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Items":
                    result.Items = ReadItems(ref reader, itemReader);
                    break;
                case "Count":
                    reader.GetInt32();
                    break;
                case "ScannedCount":
                    result.ScannedCount = reader.GetInt32();
                    break;
                case "LastEvaluatedKey":
                    result.LastEvaluatedKey = ReadAttributeMap(ref reader);
                    break;
                case "ConsumedCapacity":
                    result.ConsumedCapacity = ReadConsumedCapacity(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return result;
    }

    public static T? ReadGetItemResponse<T>(ReadOnlySpan<byte> utf8Json, DynamoItemReader<T> itemReader)
    {
        var reader = new Utf8JsonReader(utf8Json);

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Item":
                    return itemReader(ref reader);
                default:
                    reader.Skip();
                    break;
            }
        }

        return default;
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
            var typeName = reader.GetString();
            reader.Read(); // value

            switch (typeName)
            {
                case "S":
                    attr.S = reader.GetString();
                    break;
                case "N":
                    attr.N = reader.GetString();
                    break;
                case "BOOL":
                    attr.BOOL = reader.GetBoolean();
                    break;
                case "NULL":
                    attr.NULL = reader.GetBoolean();
                    break;
                case "SS":
                    attr.SS = ReadStringArray(ref reader);
                    break;
                case "NS":
                    attr.NS = ReadStringArray(ref reader);
                    break;
                case "L":
                    attr.L = ReadAttributeValueList(ref reader);
                    break;
                case "M":
                    attr.M = ReadAttributeMap(ref reader);
                    break;
                case "B":
                case "BS":
                    reader.Skip();
                    break;
                default:
                    reader.Skip();
                    break;
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
            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "TableName":
                    capacity.TableName = reader.GetString();
                    break;
                case "CapacityUnits":
                    capacity.CapacityUnits = reader.GetDouble();
                    break;
                case "ReadCapacityUnits":
                    capacity.ReadCapacityUnits = reader.GetDouble();
                    break;
                case "WriteCapacityUnits":
                    capacity.WriteCapacityUnits = reader.GetDouble();
                    break;
                case "Table":
                    capacity.Table = ReadCapacityDetail(ref reader);
                    break;
                case "GlobalSecondaryIndexes":
                    capacity.GlobalSecondaryIndexes = ReadCapacityDetailMap(ref reader);
                    break;
                case "LocalSecondaryIndexes":
                    capacity.LocalSecondaryIndexes = ReadCapacityDetailMap(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
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
            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "CapacityUnits":
                    detail.CapacityUnits = reader.GetDouble();
                    break;
                case "ReadCapacityUnits":
                    detail.ReadCapacityUnits = reader.GetDouble();
                    break;
                case "WriteCapacityUnits":
                    detail.WriteCapacityUnits = reader.GetDouble();
                    break;
                default:
                    reader.Skip();
                    break;
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
        var result = new BatchGetResult<T>();
        var reader = new Utf8JsonReader(utf8Json);

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Responses":
                    result.Responses = ReadTableResponses(ref reader, itemReader);
                    break;
                case "UnprocessedKeys":
                    // Skip for now - complex structure, users can retry with original request
                    reader.Skip();
                    break;
                case "ConsumedCapacity":
                    result.ConsumedCapacity = ReadConsumedCapacityList(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return result;
    }

    public static TransactGetResult<T> ReadTransactGetItemResponse<T>(ReadOnlySpan<byte> utf8Json, DynamoItemReader<T> itemReader)
    {
        var result = new TransactGetResult<T>();
        var reader = new Utf8JsonReader(utf8Json);

        reader.Read(); // StartObject
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Responses":
                    result.Items = ReadTransactGetResponses(ref reader, itemReader);
                    break;
                case "ConsumedCapacity":
                    result.ConsumedCapacity = ReadConsumedCapacityList(ref reader);
                    break;
                default:
                    reader.Skip();
                    break;
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
