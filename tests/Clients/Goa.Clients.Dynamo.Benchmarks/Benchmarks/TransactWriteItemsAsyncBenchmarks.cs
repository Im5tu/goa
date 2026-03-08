using BenchmarkDotNet.Order;
using EfficientDynamoDb.DocumentModel;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using Goa.Clients.Dynamo.Operations.Transactions;
using GoaModels = Goa.Clients.Dynamo.Models;
using AwsTransactWriteItem = Amazon.DynamoDBv2.Model.TransactWriteItem;
using EfficientTransactWriteItemsRequest = EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItemsRequest;
using EfficientTransactWriteItemsResponse = EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItemsResponse;
using EfficientAttributeValue = EfficientDynamoDb.DocumentModel.AttributeValue;
using AwsTransactWriteItemsRequest = Amazon.DynamoDBv2.Model.TransactWriteItemsRequest;
using AwsTransactWriteItemsResponse = Amazon.DynamoDBv2.Model.TransactWriteItemsResponse;
using AwsAttributeValue = Amazon.DynamoDBv2.Model.AttributeValue;
using AwsPut = Amazon.DynamoDBv2.Model.Put;
using AwsUpdate = Amazon.DynamoDBv2.Model.Update;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class TransactWriteItemsAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;
    private int _counter;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Transact Write 5 Items")]
    public async Task<AwsTransactWriteItemsResponse> AwsSdk_TransactWrite_5Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var items = new List<AwsTransactWriteItem>();
        for (var i = 0; i < 5; i++)
        {
            items.Add(new AwsTransactWriteItem
            {
                Put = new AwsPut
                {
                    TableName = _fixture.TableName,
                    Item = new Dictionary<string, AwsAttributeValue>
                    {
                        ["pk"] = new AwsAttributeValue($"aws-tw-{batch}"),
                        ["sk"] = new AwsAttributeValue($"item-{i:D4}"),
                        ["data"] = new AwsAttributeValue($"value-{i}")
                    }
                }
            });
        }
        return await _fixture.AwsSdkClient.TransactWriteItemsAsync(new AwsTransactWriteItemsRequest
        {
            TransactItems = items
        });
    }

    [Benchmark, BenchmarkCategory("Transact Write 5 Items")]
    public async Task<bool> Goa_TransactWrite_5Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var items = new List<Goa.Clients.Dynamo.Operations.Transactions.TransactWriteItem>();
        for (var i = 0; i < 5; i++)
        {
            items.Add(new Goa.Clients.Dynamo.Operations.Transactions.TransactWriteItem
            {
                Put = new TransactPutItem
                {
                    TableName = _fixture.TableName,
                    Item = new Dictionary<string, GoaModels.AttributeValue>
                    {
                        ["pk"] = GoaModels.AttributeValue.String($"goa-tw-{batch}"),
                        ["sk"] = GoaModels.AttributeValue.String($"item-{i:D4}"),
                        ["data"] = GoaModels.AttributeValue.String($"value-{i}")
                    }
                }
            });
        }
        var response = await _fixture.GoaClient.TransactWriteItemsAsync(new TransactWriteRequest
        {
            TransactItems = items
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Transact Write 5 Items")]
    public async Task<EfficientTransactWriteItemsResponse> Efficient_TransactWrite_5Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var items = new List<EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItem>();
        for (var i = 0; i < 5; i++)
        {
            items.Add(new EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItem(new EfficientDynamoDb.Operations.TransactWriteItems.TransactPutItem
            {
                TableName = _fixture.TableName,
                Item = new Document
                {
                    ["pk"] = $"eff-tw-{batch}",
                    ["sk"] = $"item-{i:D4}",
                    ["data"] = $"value-{i}"
                }
            }));
        }
        return await _fixture.EfficientClient.TransactWriteItemsAsync(new EfficientTransactWriteItemsRequest
        {
            TransactItems = items
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Transact Write Mixed Ops")]
    public async Task<AwsTransactWriteItemsResponse> AwsSdk_TransactWrite_MixedOps()
    {
        var batch = Interlocked.Increment(ref _counter);
        // First seed an item to update/delete
        await _fixture.AwsSdkClient.PutItemAsync(new Amazon.DynamoDBv2.Model.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, AwsAttributeValue>
            {
                ["pk"] = new AwsAttributeValue($"aws-twm-{batch}"),
                ["sk"] = new AwsAttributeValue("existing"),
                ["data"] = new AwsAttributeValue("seed")
            }
        });

        return await _fixture.AwsSdkClient.TransactWriteItemsAsync(new AwsTransactWriteItemsRequest
        {
            TransactItems =
            [
                new AwsTransactWriteItem
                {
                    Put = new AwsPut
                    {
                        TableName = _fixture.TableName,
                        Item = new Dictionary<string, AwsAttributeValue>
                        {
                            ["pk"] = new AwsAttributeValue($"aws-twm-{batch}"),
                            ["sk"] = new AwsAttributeValue("new-item"),
                            ["data"] = new AwsAttributeValue("created")
                        }
                    }
                },
                new AwsTransactWriteItem
                {
                    Update = new AwsUpdate
                    {
                        TableName = _fixture.TableName,
                        Key = new Dictionary<string, AwsAttributeValue>
                        {
                            ["pk"] = new AwsAttributeValue($"aws-twm-{batch}"),
                            ["sk"] = new AwsAttributeValue("existing")
                        },
                        UpdateExpression = "SET #d = :val",
                        ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
                        ExpressionAttributeValues = new Dictionary<string, AwsAttributeValue>
                        {
                            [":val"] = new AwsAttributeValue("updated")
                        }
                    }
                }
            ]
        });
    }

    [Benchmark, BenchmarkCategory("Transact Write Mixed Ops")]
    public async Task<bool> Goa_TransactWrite_MixedOps()
    {
        var batch = Interlocked.Increment(ref _counter);
        await _fixture.GoaClient.PutItemAsync(new Goa.Clients.Dynamo.Operations.PutItem.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String($"goa-twm-{batch}"),
                ["sk"] = GoaModels.AttributeValue.String("existing"),
                ["data"] = GoaModels.AttributeValue.String("seed")
            }
        });

        var response = await _fixture.GoaClient.TransactWriteItemsAsync(new TransactWriteRequest
        {
            TransactItems =
            [
                new Goa.Clients.Dynamo.Operations.Transactions.TransactWriteItem
                {
                    Put = new TransactPutItem
                    {
                        TableName = _fixture.TableName,
                        Item = new Dictionary<string, GoaModels.AttributeValue>
                        {
                            ["pk"] = GoaModels.AttributeValue.String($"goa-twm-{batch}"),
                            ["sk"] = GoaModels.AttributeValue.String("new-item"),
                            ["data"] = GoaModels.AttributeValue.String("created")
                        }
                    }
                },
                new Goa.Clients.Dynamo.Operations.Transactions.TransactWriteItem
                {
                    Update = new TransactUpdateItem
                    {
                        TableName = _fixture.TableName,
                        Key = new Dictionary<string, GoaModels.AttributeValue>
                        {
                            ["pk"] = GoaModels.AttributeValue.String($"goa-twm-{batch}"),
                            ["sk"] = GoaModels.AttributeValue.String("existing")
                        },
                        UpdateExpression = "SET #d = :val",
                        ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
                        ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
                        {
                            [":val"] = GoaModels.AttributeValue.String("updated")
                        }
                    }
                }
            ]
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Transact Write Mixed Ops")]
    public async Task<EfficientTransactWriteItemsResponse> Efficient_TransactWrite_MixedOps()
    {
        var batch = Interlocked.Increment(ref _counter);
        await _fixture.EfficientClient.PutItemAsync(new EfficientDynamoDb.Operations.PutItem.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Document
            {
                ["pk"] = $"eff-twm-{batch}",
                ["sk"] = "existing",
                ["data"] = "seed"
            }
        });

        return await _fixture.EfficientClient.TransactWriteItemsAsync(new EfficientTransactWriteItemsRequest
        {
            TransactItems =
            [
                new EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItem(new EfficientDynamoDb.Operations.TransactWriteItems.TransactPutItem
                {
                    TableName = _fixture.TableName,
                    Item = new Document
                    {
                        ["pk"] = $"eff-twm-{batch}",
                        ["sk"] = "new-item",
                        ["data"] = "created"
                    }
                }),
                new EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItem(new EfficientDynamoDb.Operations.TransactWriteItems.TransactUpdateItem
                {
                    TableName = _fixture.TableName,
                    Key = new PrimaryKey("pk", $"eff-twm-{batch}", "sk", "existing"),
                    UpdateExpression = "SET #d = :val",
                    ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
                    ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
                    {
                        [":val"] = "updated"
                    }
                })
            ]
        });
    }
}
