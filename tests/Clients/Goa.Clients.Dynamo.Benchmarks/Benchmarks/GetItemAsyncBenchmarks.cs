using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using EfficientGetItemResponse = EfficientDynamoDb.Operations.GetItem.GetItemResponse;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class GetItemAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _fixture.SeedItemsAsync("get-bench", 1).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Get Item")]
    public async Task<GetItemResponse> AwsSdk_GetItem()
    {
        return await _fixture.AwsSdkClient.GetItemAsync(new GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new("get-bench"),
                ["sk"] = new("item-0000")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Get Item")]
    public async Task<GoaModels.DynamoRecord?> Goa_GetItem()
    {
        var response = await _fixture.GoaClient.GetItemAsync(new Goa.Clients.Dynamo.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = new() { S = "get-bench" },
                ["sk"] = new() { S = "item-0000" }
            }
        });
        return response.Value.Item;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Get Item Miss")]
    public async Task<GetItemResponse> AwsSdk_GetItem_Miss()
    {
        return await _fixture.AwsSdkClient.GetItemAsync(new GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new("nonexistent"),
                ["sk"] = new("nonexistent")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Get Item Miss")]
    public async Task<GoaModels.DynamoRecord?> Goa_GetItem_Miss()
    {
        var response = await _fixture.GoaClient.GetItemAsync(new Goa.Clients.Dynamo.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = new() { S = "nonexistent" },
                ["sk"] = new() { S = "nonexistent" }
            }
        });
        return response.Value.Item;
    }

    [Benchmark, BenchmarkCategory("Get Item")]
    public async Task<EfficientGetItemResponse> Efficient_GetItem()
    {
        return await _fixture.EfficientClient.GetItemAsync(new EfficientDynamoDb.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", "get-bench", "sk", "item-0000")
        });
    }

    [Benchmark, BenchmarkCategory("Get Item Miss")]
    public async Task<EfficientGetItemResponse> Efficient_GetItem_Miss()
    {
        return await _fixture.EfficientClient.GetItemAsync(new EfficientDynamoDb.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", "nonexistent", "sk", "nonexistent")
        });
    }
}
