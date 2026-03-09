using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;

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
        _fixture.SeedItemsAsync("txget-bench", 10).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Transact Get 10 Items")]
    public async Task<TransactGetItemsResponse> AwsSdk_TransactGetItems()
    {
        var items = new List<TransactGetItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new TransactGetItem
            {
                Get = new Get
                {
                    TableName = _fixture.TableName,
                    Key = new Dictionary<string, AttributeValue>
                    {
                        ["pk"] = new("txget-bench"),
                        ["sk"] = new($"item-{i:D4}")
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
    public async Task<Goa.Clients.Dynamo.Operations.Transactions.TransactGetItemResponse> Goa_TransactGetItems()
    {
        var items = new List<Goa.Clients.Dynamo.Operations.Transactions.TransactGetItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new Goa.Clients.Dynamo.Operations.Transactions.TransactGetItem
            {
                Get = new Goa.Clients.Dynamo.Operations.Transactions.TransactGetItemRequest
                {
                    TableName = _fixture.TableName,
                    Key = new Dictionary<string, GoaModels.AttributeValue>
                    {
                        ["pk"] = new() { S = "txget-bench" },
                        ["sk"] = new() { S = $"item-{i:D4}" }
                    }
                }
            });
        }

        var response = await _fixture.GoaClient.TransactGetItemsAsync(new Goa.Clients.Dynamo.Operations.Transactions.TransactGetRequest
        {
            TransactItems = items
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Transact Get 10 Items")]
    public async Task<EfficientDynamoDb.Operations.TransactGetItems.TransactGetItemsResponse> Efficient_TransactGetItems()
    {
        var items = new List<EfficientDynamoDb.Operations.TransactGetItems.TransactGetRequest>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new EfficientDynamoDb.Operations.TransactGetItems.TransactGetRequest
            {
                TableName = _fixture.TableName,
                Key = new PrimaryKey("pk", "txget-bench", "sk", $"item-{i:D4}")
            });
        }

        return await _fixture.EfficientClient.TransactGetItemsAsync(new EfficientDynamoDb.Operations.TransactGetItems.TransactGetItemsRequest
        {
            TransactItems = items
        });
    }
}
