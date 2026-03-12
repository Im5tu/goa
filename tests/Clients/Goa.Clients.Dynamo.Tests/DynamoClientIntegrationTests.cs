using Goa.Clients.Dynamo.Enums;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.DeleteItem;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.UpdateItem;

namespace Goa.Clients.Dynamo.Tests;

[ClassDataSource<DynamoTestFixture>(Shared = SharedType.PerAssembly)]
public class DynamoClientIntegrationTests
{
    private readonly DynamoTestFixture _fixture;

    public DynamoClientIntegrationTests(DynamoTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Test]
    public async Task PutItemAsync_ShouldStoreItem_WhenValidItemProvided()
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = AttributeValue.String("test-pk"),
            ["sk"] = AttributeValue.String("test-sk"),
            ["data"] = AttributeValue.String("test-data"),
            ["number"] = AttributeValue.Number("123")
        };

        var request = new PutItemRequest
        {
            TableName = _fixture.TestTableName,
            Item = item
        };

        var response = await _fixture.DynamoClient.PutItemAsync(request);

        await Assert.That(response.IsError).IsFalse();
    }

    [Test]
    public async Task GetItemAsync_ShouldReturnItem_WhenItemExists()
    {
        var putItem = new Dictionary<string, AttributeValue>
        {
            ["pk"] = AttributeValue.String("get-test-pk"),
            ["sk"] = AttributeValue.String("get-test-sk"),
            ["data"] = AttributeValue.String("retrieved-data")
        };

        var putRequest = new PutItemRequest
        {
            TableName = _fixture.TestTableName,
            Item = putItem
        };

        await _fixture.DynamoClient.PutItemAsync(putRequest);

        var getRequest = new GetItemRequest
        {
            TableName = _fixture.TestTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("get-test-pk"),
                ["sk"] = AttributeValue.String("get-test-sk")
            }
        };

        var response = await _fixture.DynamoClient.GetItemAsync(getRequest);

        await Assert.That(response.IsError).IsFalse();
        await Assert.That(() => response.Value.Item).IsNotNull();
        var attribute = (response.Value.Item!)["data"];
        await Assert.That(attribute).IsNotNull();
        await Assert.That(attribute!.Value.S).IsEqualTo("retrieved-data");
    }

    [Test]
    public async Task GetItemAsync_ShouldReturnEmptyItem_WhenItemDoesNotExist()
    {
        var getRequest = new GetItemRequest
        {
            TableName = _fixture.TestTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("non-existent-pk"),
                ["sk"] = AttributeValue.String("non-existent-sk")
            }
        };

        var response = await _fixture.DynamoClient.GetItemAsync(getRequest);

        await Assert.That(response.IsError).IsFalse();
        await Assert.That(() => response.Value.Item).IsNull();
    }

    [Test]
    public async Task UpdateItemAsync_ShouldModifyItem_WhenItemExists()
    {
        var initialItem = new Dictionary<string, AttributeValue>
        {
            ["pk"] = AttributeValue.String("update-test-pk"),
            ["sk"] = AttributeValue.String("update-test-sk"),
            ["data"] = AttributeValue.String("initial-data"),
            ["counter"] = AttributeValue.Number("1")
        };

        var putRequest = new PutItemRequest
        {
            TableName = _fixture.TestTableName,
            Item = initialItem
        };

        await _fixture.DynamoClient.PutItemAsync(putRequest);

        var updateRequest = new UpdateItemRequest
        {
            TableName = _fixture.TestTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("update-test-pk"),
                ["sk"] = AttributeValue.String("update-test-sk")
            },
            UpdateExpression = "SET #data = :newData, #counter = #counter + :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#data"] = "data",
                ["#counter"] = "counter"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":newData"] = AttributeValue.String("updated-data"),
                [":inc"] = AttributeValue.Number("1")
            },
            ReturnValues = ReturnValues.ALL_NEW
        };

        var response = await _fixture.DynamoClient.UpdateItemAsync(updateRequest);

        await Assert.That(response.IsError).IsFalse();
        await Assert.That(() => response.Value.Attributes).IsNotNull();

        var data = response.Value.Attributes!["data"];
        var counter = response.Value.Attributes!["counter"];

        await Assert.That(data?.S).IsEqualTo("updated-data");
        await Assert.That(counter?.N).IsEqualTo("2");
    }

    [Test]
    public async Task DeleteItemAsync_ShouldRemoveItem_WhenItemExists()
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["pk"] = AttributeValue.String("delete-test-pk"),
            ["sk"] = AttributeValue.String("delete-test-sk"),
            ["data"] = AttributeValue.String("to-be-deleted")
        };

        var putRequest = new PutItemRequest
        {
            TableName = _fixture.TestTableName,
            Item = item
        };

        await _fixture.DynamoClient.PutItemAsync(putRequest);

        var deleteRequest = new DeleteItemRequest
        {
            TableName = _fixture.TestTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("delete-test-pk"),
                ["sk"] = AttributeValue.String("delete-test-sk")
            },
            ReturnValues = ReturnValues.ALL_OLD
        };

        var deleteResponse = await _fixture.DynamoClient.DeleteItemAsync(deleteRequest);

        await Assert.That(deleteResponse.IsError).IsFalse();
        var attributes = deleteResponse.Value.Attributes;
        await Assert.That(() => attributes).IsNotNull();
        await Assert.That(attributes!["data"]?.S).IsEqualTo("to-be-deleted");

        var getRequest = new GetItemRequest
        {
            TableName = _fixture.TestTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("delete-test-pk"),
                ["sk"] = AttributeValue.String("delete-test-sk")
            }
        };

        var getResponse = await _fixture.DynamoClient.GetItemAsync(getRequest);
        await Assert.That(() => getResponse.Value.Item).IsNull();
    }

    [Test]
    public async Task QueryAsync_ShouldReturnMatchingItems_WhenItemsExist()
    {
        var items = new[]
        {
            new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("query-test-pk"),
                ["sk"] = AttributeValue.String("item1"),
                ["data"] = AttributeValue.String("data1")
            },
            new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("query-test-pk"),
                ["sk"] = AttributeValue.String("item2"),
                ["data"] = AttributeValue.String("data2")
            },
            new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("different-pk"),
                ["sk"] = AttributeValue.String("item3"),
                ["data"] = AttributeValue.String("data3")
            }
        };

        foreach (var item in items)
        {
            var putRequest = new PutItemRequest
            {
                TableName = _fixture.TestTableName,
                Item = item
            };
            await _fixture.DynamoClient.PutItemAsync(putRequest);
        }

        var queryRequest = new QueryRequest
        {
            TableName = _fixture.TestTableName,
            KeyConditionExpression = "pk = :pk",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = AttributeValue.String("query-test-pk")
            }
        };

        var response = await _fixture.DynamoClient.QueryAsync(queryRequest);

        await Assert.That(response.IsError).IsFalse();
        await Assert.That(response.Value.Count).IsEqualTo(2);
        await Assert.That(response.Value.Items.All(item => item["pk"]?.S == "query-test-pk")).IsTrue();
    }

    [Test]
    public async Task ScanAsync_ShouldReturnAllItems_WhenNoFilterApplied()
    {
        var items = new[]
        {
            new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("scan-test-pk1"),
                ["sk"] = AttributeValue.String("scan-item1"),
                ["type"] = AttributeValue.String("test")
            },
            new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("scan-test-pk2"),
                ["sk"] = AttributeValue.String("scan-item2"),
                ["type"] = AttributeValue.String("test")
            }
        };

        foreach (var item in items)
        {
            var putRequest = new PutItemRequest
            {
                TableName = _fixture.TestTableName,
                Item = item
            };
            await _fixture.DynamoClient.PutItemAsync(putRequest);
        }

        var scanRequest = new ScanRequest
        {
            TableName = _fixture.TestTableName,
            FilterExpression = "#type = :type",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#type"] = "type"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":type"] = AttributeValue.String("test")
            }
        };

        var response = await _fixture.DynamoClient.ScanAsync(scanRequest);

        await Assert.That(response.IsError).IsFalse();
        await Assert.That(response.Value.Count).IsEqualTo(2);
        await Assert.That(response.Value.Items.All(item => item["type"]?.S == "test")).IsTrue();
    }

    [Test]
    public async Task BatchGetItemAsync_ShouldReturnRequestedItems_WhenItemsExist()
    {
        var items = new[]
        {
            new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("batch-get-pk1"),
                ["sk"] = AttributeValue.String("batch-get-sk1"),
                ["data"] = AttributeValue.String("batch-data1")
            },
            new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("batch-get-pk2"),
                ["sk"] = AttributeValue.String("batch-get-sk2"),
                ["data"] = AttributeValue.String("batch-data2")
            }
        };

        foreach (var item in items)
        {
            var putRequest = new PutItemRequest
            {
                TableName = _fixture.TestTableName,
                Item = item
            };
            await _fixture.DynamoClient.PutItemAsync(putRequest);
        }

        var batchGetRequest = new BatchGetItemRequest
        {
            RequestItems = new Dictionary<string, BatchGetRequestItem>
            {
                [_fixture.TestTableName] = new()
                {
                    Keys = new List<Dictionary<string, AttributeValue>>
                    {
                        new()
                        {
                            ["pk"] = AttributeValue.String("batch-get-pk1"),
                            ["sk"] = AttributeValue.String("batch-get-sk1")
                        },
                        new()
                        {
                            ["pk"] = AttributeValue.String("batch-get-pk2"),
                            ["sk"] = AttributeValue.String("batch-get-sk2")
                        }
                    }
                }
            }
        };

        var response = await _fixture.DynamoClient.BatchGetItemAsync(batchGetRequest);

        await Assert.That(response.IsError).IsFalse();
        await Assert.That(response.Value.Responses[_fixture.TestTableName]).Count().IsEqualTo(2);
    }

    [Test]
    public async Task BatchWriteItemAsync_ShouldProcessMultipleOperations_WhenValidRequestProvided()
    {
        var putItem = new Dictionary<string, AttributeValue>
        {
            ["pk"] = AttributeValue.String("batch-write-pk1"),
            ["sk"] = AttributeValue.String("batch-write-sk1"),
            ["data"] = AttributeValue.String("batch-put-data")
        };

        var existingItem = new Dictionary<string, AttributeValue>
        {
            ["pk"] = AttributeValue.String("batch-write-pk2"),
            ["sk"] = AttributeValue.String("batch-write-sk2"),
            ["data"] = AttributeValue.String("to-be-deleted")
        };

        var putExistingRequest = new PutItemRequest
        {
            TableName = _fixture.TestTableName,
            Item = existingItem
        };
        await _fixture.DynamoClient.PutItemAsync(putExistingRequest);

        var batchWriteRequest = new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<BatchWriteRequestItem>>
            {
                [_fixture.TestTableName] = new()
                {
                    new BatchWriteRequestItem
                    {
                        PutRequest = new PutRequest { Item = putItem }
                    },
                    new BatchWriteRequestItem
                    {
                        DeleteRequest = new DeleteRequest
                        {
                            Key = new Dictionary<string, AttributeValue>
                            {
                                ["pk"] = AttributeValue.String("batch-write-pk2"),
                                ["sk"] = AttributeValue.String("batch-write-sk2")
                            }
                        }
                    }
                }
            }
        };

        var response = await _fixture.DynamoClient.BatchWriteItemAsync(batchWriteRequest);

        await Assert.That(response.IsError).IsFalse();

        var getRequest = new GetItemRequest
        {
            TableName = _fixture.TestTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("batch-write-pk1"),
                ["sk"] = AttributeValue.String("batch-write-sk1")
            }
        };

        var getResponse = await _fixture.DynamoClient.GetItemAsync(getRequest);
        await Assert.That(() => getResponse.Value.Item).IsNotNull();

        var getDeletedRequest = new GetItemRequest
        {
            TableName = _fixture.TestTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = AttributeValue.String("batch-write-pk2"),
                ["sk"] = AttributeValue.String("batch-write-sk2")
            }
        };

        var getDeletedResponse = await _fixture.DynamoClient.GetItemAsync(getDeletedRequest);
        await Assert.That(() => getDeletedResponse.Value.Item).IsNull();
    }

    [Test]
    public async Task QueryAsync_ShouldPaginate_WhenLimitIsSet()
    {
        // Insert 10 items
        for (var i = 0; i < 10; i++)
        {
            await _fixture.DynamoClient.PutItemAsync(new PutItemRequest
            {
                TableName = _fixture.TestTableName,
                Item = new Dictionary<string, AttributeValue>
                {
                    ["pk"] = AttributeValue.String("paginate-test"),
                    ["sk"] = AttributeValue.String($"item-{i:D4}"),
                    ["data"] = AttributeValue.String($"value-{i}")
                }
            });
        }

        var totalCount = 0;
        var pageCount = 0;
        Dictionary<string, AttributeValue>? lastKey = null;

        do
        {
            var response = await _fixture.DynamoClient.QueryAsync(new QueryRequest
            {
                TableName = _fixture.TestTableName,
                KeyConditionExpression = "pk = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = AttributeValue.String("paginate-test")
                },
                Limit = 3,
                ExclusiveStartKey = lastKey
            });

            await Assert.That(response.IsError).IsFalse();
            totalCount += response.Value.Items.Count;
            pageCount++;
            lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
        } while (lastKey != null);

        await Assert.That(totalCount).IsEqualTo(10);
        await Assert.That(pageCount).IsGreaterThanOrEqualTo(4); // 10 items / 3 per page = at least 4 pages
    }
}
