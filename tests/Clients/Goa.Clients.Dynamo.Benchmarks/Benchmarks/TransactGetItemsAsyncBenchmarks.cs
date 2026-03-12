using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using Goa.Clients.Dynamo.Operations.Transactions;
using GoaModels = Goa.Clients.Dynamo.Models;
using EfficientTransactGetItemsRequest = EfficientDynamoDb.Operations.TransactGetItems.TransactGetItemsRequest;
using EfficientTransactGetItemsResponse = EfficientDynamoDb.Operations.TransactGetItems.TransactGetItemsResponse;
using EfficientTransactGetRequest = EfficientDynamoDb.Operations.TransactGetItems.TransactGetRequest;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class TransactGetItemsAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _fixture.SeedItemsAsync("transact-get", 10).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Transact Get 5 Items")]
    public async Task<TransactGetItemsResponse> AwsSdk_TransactGet_5Items()
    {
        var items = new List<Amazon.DynamoDBv2.Model.TransactGetItem>();
        for (var i = 0; i < 5; i++)
        {
            items.Add(new Amazon.DynamoDBv2.Model.TransactGetItem
            {
                Get = new Get
                {
                    TableName = _fixture.TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["pk"] = new AttributeValue("transact-get"),
                        ["sk"] = new AttributeValue($"item-{i:D4}")
                    }
                }
            });
        }
        return await _fixture.AwsSdkClient.TransactGetItemsAsync(new TransactGetItemsRequest
        {
            TransactItems = items
        });
    }

    [Benchmark, BenchmarkCategory("Transact Get 5 Items")]
    public async Task<TransactGetItemResponse> Goa_TransactGet_5Items()
    {
        var items = new List<Goa.Clients.Dynamo.Operations.Transactions.TransactGetItem>();
        for (var i = 0; i < 5; i++)
        {
            items.Add(new Goa.Clients.Dynamo.Operations.Transactions.TransactGetItem
            {
                Get = new TransactGetItemRequest
                {
                    TableName = _fixture.TableName,
                    Key = new Dictionary<string, GoaModels.AttributeValue>
                    {
                        ["pk"] = GoaModels.AttributeValue.String("transact-get"),
                        ["sk"] = GoaModels.AttributeValue.String($"item-{i:D4}")
                    }
                }
            });
        }
        var response = await _fixture.GoaClient.TransactGetItemsAsync(new TransactGetRequest
        {
            TransactItems = items
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Transact Get 5 Items")]
    public async Task<EfficientTransactGetItemsResponse> Efficient_TransactGet_5Items()
    {
        var items = new List<EfficientTransactGetRequest>();
        for (var i = 0; i < 5; i++)
        {
            items.Add(new EfficientTransactGetRequest
            {
                TableName = _fixture.TableName,
                Key = new PrimaryKey("pk", "transact-get", "sk", $"item-{i:D4}")
            });
        }
        return await _fixture.EfficientClient.TransactGetItemsAsync(new EfficientTransactGetItemsRequest
        {
            TransactItems = items
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Transact Get 10 Items")]
    public async Task<TransactGetItemsResponse> AwsSdk_TransactGet_10Items()
    {
        var items = new List<Amazon.DynamoDBv2.Model.TransactGetItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new Amazon.DynamoDBv2.Model.TransactGetItem
            {
                Get = new Get
                {
                    TableName = _fixture.TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["pk"] = new AttributeValue("transact-get"),
                        ["sk"] = new AttributeValue($"item-{i:D4}")
                    }
                }
            });
        }
        return await _fixture.AwsSdkClient.TransactGetItemsAsync(new TransactGetItemsRequest
        {
            TransactItems = items
        });
    }

    [Benchmark, BenchmarkCategory("Transact Get 10 Items")]
    public async Task<TransactGetItemResponse> Goa_TransactGet_10Items()
    {
        var items = new List<Goa.Clients.Dynamo.Operations.Transactions.TransactGetItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new Goa.Clients.Dynamo.Operations.Transactions.TransactGetItem
            {
                Get = new TransactGetItemRequest
                {
                    TableName = _fixture.TableName,
                    Key = new Dictionary<string, GoaModels.AttributeValue>
                    {
                        ["pk"] = GoaModels.AttributeValue.String("transact-get"),
                        ["sk"] = GoaModels.AttributeValue.String($"item-{i:D4}")
                    }
                }
            });
        }
        var response = await _fixture.GoaClient.TransactGetItemsAsync(new TransactGetRequest
        {
            TransactItems = items
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Transact Get 10 Items")]
    public async Task<EfficientTransactGetItemsResponse> Efficient_TransactGet_10Items()
    {
        var items = new List<EfficientTransactGetRequest>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new EfficientTransactGetRequest
            {
                TableName = _fixture.TableName,
                Key = new PrimaryKey("pk", "transact-get", "sk", $"item-{i:D4}")
            });
        }
        return await _fixture.EfficientClient.TransactGetItemsAsync(new EfficientTransactGetItemsRequest
        {
            TransactItems = items
        });
    }
}
