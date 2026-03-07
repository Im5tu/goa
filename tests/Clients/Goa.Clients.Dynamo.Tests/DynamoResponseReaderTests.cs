using System.Text.Json;
using Goa.Clients.Dynamo.Internal;

namespace Goa.Clients.Dynamo.Tests;

public class DynamoResponseReaderTests
{
    /// <summary>
    /// Simple test entity for deserialization.
    /// </summary>
    private sealed class TestEntity
    {
        public string? Pk { get; set; }
        public string? Sk { get; set; }
        public string? Data { get; set; }
    }

    /// <summary>
    /// Reads a TestEntity from a DynamoDB item JSON object.
    /// Simulates what a source-generated DynamoJsonMapper would produce.
    /// </summary>
    private static TestEntity ReadTestEntity(ref Utf8JsonReader reader)
    {
        var entity = new TestEntity();

        // reader is at StartObject
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            var propName = reader.GetString();
            reader.Read(); // StartObject (type wrapper)
            reader.Read(); // PropertyName (type indicator like "S")
            var typeName = reader.GetString();
            reader.Read(); // Value

            switch (propName)
            {
                case "pk" when typeName == "S":
                    entity.Pk = reader.GetString();
                    break;
                case "sk" when typeName == "S":
                    entity.Sk = reader.GetString();
                    break;
                case "data" when typeName == "S":
                    entity.Data = reader.GetString();
                    break;
                default:
                    // Skip unknown values
                    break;
            }

            reader.Read(); // EndObject (type wrapper)
        }

        return entity;
    }

    [Test]
    public async Task ReadQueryResponse_ShouldDeserializeItems()
    {
        var json = """
        {
            "Items": [
                {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}, "data": {"S": "value1"}},
                {"pk": {"S": "pk2"}, "sk": {"S": "sk2"}, "data": {"S": "value2"}}
            ],
            "Count": 2,
            "ScannedCount": 5
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(2);
        await Assert.That(result.Items[0].Pk).IsEqualTo("pk1");
        await Assert.That(result.Items[0].Sk).IsEqualTo("sk1");
        await Assert.That(result.Items[0].Data).IsEqualTo("value1");
        await Assert.That(result.Items[1].Pk).IsEqualTo("pk2");
        await Assert.That(result.Items[1].Sk).IsEqualTo("sk2");
        await Assert.That(result.Items[1].Data).IsEqualTo("value2");
        await Assert.That(result.ScannedCount).IsEqualTo(5);
        await Assert.That(result.HasMoreResults).IsFalse();
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseLastEvaluatedKey()
    {
        var json = """
        {
            "Items": [
                {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}}
            ],
            "Count": 1,
            "ScannedCount": 1,
            "LastEvaluatedKey": {
                "pk": {"S": "pk1"},
                "sk": {"S": "sk1"}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.HasMoreResults).IsTrue();
        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["pk"].S).IsEqualTo("pk1");
        await Assert.That(result.LastEvaluatedKey!["sk"].S).IsEqualTo("sk1");
    }

    [Test]
    public async Task ReadQueryResponse_ShouldHandleEmptyItems()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(0);
        await Assert.That(result.ScannedCount).IsEqualTo(0);
        await Assert.That(result.HasMoreResults).IsFalse();
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseLastEvaluatedKeyWithNumberType()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "LastEvaluatedKey": {
                "pk": {"S": "partition"},
                "sk": {"N": "42"}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["pk"].S).IsEqualTo("partition");
        await Assert.That(result.LastEvaluatedKey!["sk"].N).IsEqualTo("42");
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseLastEvaluatedKeyWithBoolType()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "LastEvaluatedKey": {
                "pk": {"S": "partition"},
                "flag": {"BOOL": true}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["flag"].BOOL).IsEqualTo(true);
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseLastEvaluatedKeyWithNullType()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "LastEvaluatedKey": {
                "pk": {"S": "partition"},
                "empty": {"NULL": true}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["empty"].NULL).IsEqualTo(true);
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseLastEvaluatedKeyWithStringSet()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "LastEvaluatedKey": {
                "tags": {"SS": ["a", "b", "c"]}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["tags"].SS).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["tags"].SS!.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseLastEvaluatedKeyWithNumberSet()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "LastEvaluatedKey": {
                "scores": {"NS": ["1", "2", "3"]}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["scores"].NS).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["scores"].NS!.Count).IsEqualTo(3);
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseLastEvaluatedKeyWithListType()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "LastEvaluatedKey": {
                "items": {"L": [{"S": "hello"}, {"N": "42"}]}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["items"].L).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["items"].L!.Count).IsEqualTo(2);
        await Assert.That(result.LastEvaluatedKey!["items"].L![0].S).IsEqualTo("hello");
        await Assert.That(result.LastEvaluatedKey!["items"].L![1].N).IsEqualTo("42");
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseLastEvaluatedKeyWithMapType()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "LastEvaluatedKey": {
                "nested": {"M": {"key1": {"S": "val1"}, "key2": {"N": "99"}}}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["nested"].M).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["nested"].M!["key1"].S).IsEqualTo("val1");
        await Assert.That(result.LastEvaluatedKey!["nested"].M!["key2"].N).IsEqualTo("99");
    }

    [Test]
    public async Task ReadQueryResponse_ShouldSkipUnknownTopLevelProperties()
    {
        var json = """
        {
            "Items": [
                {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}}
            ],
            "Count": 1,
            "ScannedCount": 1,
            "SomeUnknownField": {},
            "ConsumedCapacity": {"TableName": "test", "CapacityUnits": 1.0}
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.Items[0].Pk).IsEqualTo("pk1");
        await Assert.That(result.ConsumedCapacity).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.TableName).IsEqualTo("test");
        await Assert.That(result.ConsumedCapacity!.CapacityUnits).IsEqualTo(1.0);
    }

    // === ScanResponse parsing ===

    [Test]
    public async Task ReadScanResponse_ShouldDeserializeItems()
    {
        var json = """
        {
            "Items": [
                {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}, "data": {"S": "value1"}},
                {"pk": {"S": "pk2"}, "sk": {"S": "sk2"}, "data": {"S": "value2"}}
            ],
            "Count": 2,
            "ScannedCount": 5
        }
        """u8;

        var result = DynamoResponseReader.ReadScanResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(2);
        await Assert.That(result.Items[0].Pk).IsEqualTo("pk1");
        await Assert.That(result.Items[0].Sk).IsEqualTo("sk1");
        await Assert.That(result.Items[0].Data).IsEqualTo("value1");
        await Assert.That(result.Items[1].Pk).IsEqualTo("pk2");
        await Assert.That(result.Items[1].Sk).IsEqualTo("sk2");
        await Assert.That(result.Items[1].Data).IsEqualTo("value2");
        await Assert.That(result.ScannedCount).IsEqualTo(5);
        await Assert.That(result.HasMoreResults).IsFalse();
    }

    [Test]
    public async Task ReadScanResponse_ShouldParseLastEvaluatedKey()
    {
        var json = """
        {
            "Items": [
                {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}}
            ],
            "Count": 1,
            "ScannedCount": 1,
            "LastEvaluatedKey": {
                "pk": {"S": "pk1"},
                "sk": {"S": "sk1"}
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadScanResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(result.HasMoreResults).IsTrue();
        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["pk"].S).IsEqualTo("pk1");
        await Assert.That(result.LastEvaluatedKey!["sk"].S).IsEqualTo("sk1");
    }

    [Test]
    public async Task ReadScanResponse_ShouldHandleEmptyItems()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0
        }
        """u8;

        var result = DynamoResponseReader.ReadScanResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Count).IsEqualTo(0);
        await Assert.That(result.ScannedCount).IsEqualTo(0);
        await Assert.That(result.HasMoreResults).IsFalse();
    }

    // === GetItemResponse parsing ===

    [Test]
    public async Task ReadGetItemResponse_ShouldDeserializeItem()
    {
        var json = """
        {
            "Item": {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}, "data": {"S": "hello"}}
        }
        """u8;

        var result = DynamoResponseReader.ReadGetItemResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Pk).IsEqualTo("pk1");
        await Assert.That(result!.Sk).IsEqualTo("sk1");
        await Assert.That(result!.Data).IsEqualTo("hello");
    }

    [Test]
    public async Task ReadGetItemResponse_ShouldReturnDefault_WhenNoItemProperty()
    {
        var json = """
        {}
        """u8;

        var result = DynamoResponseReader.ReadGetItemResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ReadGetItemResponse_ShouldSkipUnknownProperties()
    {
        var json = """
        {
            "ConsumedCapacity": {"TableName": "t", "CapacityUnits": 1.0},
            "Item": {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}}
        }
        """u8;

        var result = DynamoResponseReader.ReadGetItemResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Pk).IsEqualTo("pk1");
    }

    // === ConsumedCapacity parsing ===

    [Test]
    public async Task ReadQueryResponse_ShouldParseConsumedCapacity()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "ConsumedCapacity": {
                "TableName": "TestTable",
                "CapacityUnits": 5.0,
                "ReadCapacityUnits": 3.0,
                "WriteCapacityUnits": 2.0
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.ConsumedCapacity).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.TableName).IsEqualTo("TestTable");
        await Assert.That(result.ConsumedCapacity!.CapacityUnits).IsEqualTo(5.0);
        await Assert.That(result.ConsumedCapacity!.ReadCapacityUnits).IsEqualTo(3.0);
        await Assert.That(result.ConsumedCapacity!.WriteCapacityUnits).IsEqualTo(2.0);
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseConsumedCapacity_WithTableDetail()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "ConsumedCapacity": {
                "TableName": "TestTable",
                "CapacityUnits": 5.0,
                "Table": {
                    "CapacityUnits": 4.0,
                    "ReadCapacityUnits": 2.5,
                    "WriteCapacityUnits": 1.5
                }
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.ConsumedCapacity).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.Table).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.Table!.CapacityUnits).IsEqualTo(4.0);
        await Assert.That(result.ConsumedCapacity!.Table!.ReadCapacityUnits).IsEqualTo(2.5);
        await Assert.That(result.ConsumedCapacity!.Table!.WriteCapacityUnits).IsEqualTo(1.5);
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseConsumedCapacity_WithIndexes()
    {
        var json = """
        {
            "Items": [],
            "Count": 0,
            "ScannedCount": 0,
            "ConsumedCapacity": {
                "TableName": "TestTable",
                "CapacityUnits": 10.0,
                "GlobalSecondaryIndexes": {
                    "GSI1": {
                        "CapacityUnits": 3.0,
                        "ReadCapacityUnits": 2.0,
                        "WriteCapacityUnits": 1.0
                    }
                },
                "LocalSecondaryIndexes": {
                    "LSI1": {
                        "CapacityUnits": 2.0,
                        "ReadCapacityUnits": 1.5,
                        "WriteCapacityUnits": 0.5
                    }
                }
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.ConsumedCapacity).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.GlobalSecondaryIndexes).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.GlobalSecondaryIndexes!["GSI1"].CapacityUnits).IsEqualTo(3.0);
        await Assert.That(result.ConsumedCapacity!.GlobalSecondaryIndexes!["GSI1"].ReadCapacityUnits).IsEqualTo(2.0);
        await Assert.That(result.ConsumedCapacity!.GlobalSecondaryIndexes!["GSI1"].WriteCapacityUnits).IsEqualTo(1.0);
        await Assert.That(result.ConsumedCapacity!.LocalSecondaryIndexes).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.LocalSecondaryIndexes!["LSI1"].CapacityUnits).IsEqualTo(2.0);
        await Assert.That(result.ConsumedCapacity!.LocalSecondaryIndexes!["LSI1"].ReadCapacityUnits).IsEqualTo(1.5);
        await Assert.That(result.ConsumedCapacity!.LocalSecondaryIndexes!["LSI1"].WriteCapacityUnits).IsEqualTo(0.5);
    }

    // === BatchGetItemResponse parsing ===

    [Test]
    public async Task ReadBatchGetItemResponse_ShouldDeserializeItems()
    {
        var json = """
        {
            "Responses": {
                "TestTable": [
                    {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}, "data": {"S": "data1"}},
                    {"pk": {"S": "pk2"}, "sk": {"S": "sk2"}, "data": {"S": "data2"}}
                ],
                "OtherTable": [
                    {"pk": {"S": "pk3"}, "sk": {"S": "sk3"}, "data": {"S": "data3"}}
                ]
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadBatchGetItemResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Responses.Count).IsEqualTo(2);
        await Assert.That(result.Responses["TestTable"].Count).IsEqualTo(2);
        await Assert.That(result.Responses["TestTable"][0].Pk).IsEqualTo("pk1");
        await Assert.That(result.Responses["TestTable"][0].Data).IsEqualTo("data1");
        await Assert.That(result.Responses["TestTable"][1].Pk).IsEqualTo("pk2");
        await Assert.That(result.Responses["OtherTable"].Count).IsEqualTo(1);
        await Assert.That(result.Responses["OtherTable"][0].Pk).IsEqualTo("pk3");
        await Assert.That(result.HasUnprocessedKeys).IsFalse();
    }

    [Test]
    public async Task ReadBatchGetItemResponse_ShouldHandleEmptyResponses()
    {
        var json = """
        {
            "Responses": {}
        }
        """u8;

        var result = DynamoResponseReader.ReadBatchGetItemResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Responses.Count).IsEqualTo(0);
        await Assert.That(result.HasUnprocessedKeys).IsFalse();
    }

    [Test]
    public async Task ReadBatchGetItemResponse_ShouldParseConsumedCapacity()
    {
        var json = """
        {
            "Responses": {
                "TestTable": [
                    {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}}
                ]
            },
            "ConsumedCapacity": [
                {
                    "TableName": "TestTable",
                    "CapacityUnits": 5.0,
                    "ReadCapacityUnits": 3.0
                }
            ]
        }
        """u8;

        var result = DynamoResponseReader.ReadBatchGetItemResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.ConsumedCapacity).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.Count).IsEqualTo(1);
        await Assert.That(result.ConsumedCapacity![0].TableName).IsEqualTo("TestTable");
        await Assert.That(result.ConsumedCapacity![0].CapacityUnits).IsEqualTo(5.0);
        await Assert.That(result.ConsumedCapacity![0].ReadCapacityUnits).IsEqualTo(3.0);
    }

    // === TransactGetItemResponse parsing ===

    [Test]
    public async Task ReadTransactGetItemResponse_ShouldDeserializeItems()
    {
        var json = """
        {
            "Responses": [
                {"Item": {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}, "data": {"S": "data1"}}},
                {"Item": {"pk": {"S": "pk2"}, "sk": {"S": "sk2"}, "data": {"S": "data2"}}}
            ]
        }
        """u8;

        var result = DynamoResponseReader.ReadTransactGetItemResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(2);
        await Assert.That(result.Items[0]).IsNotNull();
        await Assert.That(result.Items[0]!.Pk).IsEqualTo("pk1");
        await Assert.That(result.Items[0]!.Data).IsEqualTo("data1");
        await Assert.That(result.Items[1]).IsNotNull();
        await Assert.That(result.Items[1]!.Pk).IsEqualTo("pk2");
        await Assert.That(result.Items[1]!.Data).IsEqualTo("data2");
    }

    [Test]
    public async Task ReadTransactGetItemResponse_ShouldHandleEmptyItemObjects()
    {
        var json = """
        {
            "Responses": [
                {"Item": {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}, "data": {"S": "data1"}}},
                {},
                {"Item": {"pk": {"S": "pk3"}, "sk": {"S": "sk3"}, "data": {"S": "data3"}}}
            ]
        }
        """u8;

        var result = DynamoResponseReader.ReadTransactGetItemResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(3);
        await Assert.That(result.Items[0]).IsNotNull();
        await Assert.That(result.Items[0]!.Pk).IsEqualTo("pk1");
        await Assert.That(result.Items[1]).IsNull();
        await Assert.That(result.Items[2]).IsNotNull();
        await Assert.That(result.Items[2]!.Pk).IsEqualTo("pk3");
    }

    [Test]
    public async Task ReadQueryResponse_ShouldParseFullResponse()
    {
        var json = """
        {
            "Items": [
                {"pk": {"S": "pk1"}, "sk": {"S": "sk1"}, "data": {"S": "d1"}},
                {"pk": {"S": "pk2"}, "sk": {"S": "sk2"}, "data": {"S": "d2"}}
            ],
            "Count": 2,
            "ScannedCount": 10,
            "LastEvaluatedKey": {
                "pk": {"S": "pk2"},
                "sk": {"S": "sk2"}
            },
            "ConsumedCapacity": {
                "TableName": "FullTable",
                "CapacityUnits": 7.5
            }
        }
        """u8;

        var result = DynamoResponseReader.ReadQueryResponse<TestEntity>(json, ReadTestEntity);

        await Assert.That(result.Items.Count).IsEqualTo(2);
        await Assert.That(result.Items[0].Pk).IsEqualTo("pk1");
        await Assert.That(result.Items[0].Sk).IsEqualTo("sk1");
        await Assert.That(result.Items[0].Data).IsEqualTo("d1");
        await Assert.That(result.Items[1].Pk).IsEqualTo("pk2");
        await Assert.That(result.Items[1].Sk).IsEqualTo("sk2");
        await Assert.That(result.Items[1].Data).IsEqualTo("d2");
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result.ScannedCount).IsEqualTo(10);
        await Assert.That(result.HasMoreResults).IsTrue();
        await Assert.That(result.LastEvaluatedKey).IsNotNull();
        await Assert.That(result.LastEvaluatedKey!["pk"].S).IsEqualTo("pk2");
        await Assert.That(result.LastEvaluatedKey!["sk"].S).IsEqualTo("sk2");
        await Assert.That(result.ConsumedCapacity).IsNotNull();
        await Assert.That(result.ConsumedCapacity!.TableName).IsEqualTo("FullTable");
        await Assert.That(result.ConsumedCapacity!.CapacityUnits).IsEqualTo(7.5);
    }

}
