using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using EfficientDeleteItemResponse = EfficientDynamoDb.Operations.DeleteItem.DeleteItemResponse;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DeleteItemAsyncBenchmarks
{
    private const int PoolSize = 10_000;
    private LocalStackFixture _fixture = null!;
    private int _counter;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _fixture.SeedItemsByPkPrefixAsync("del-aws-", "item", PoolSize).GetAwaiter().GetResult();
        _fixture.SeedItemsByPkPrefixAsync("del-goa-", "item", PoolSize).GetAwaiter().GetResult();
        _fixture.SeedItemsByPkPrefixAsync("del-eff-", "item", PoolSize).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Delete Item")]
    public async Task<DeleteItemResponse> AwsSdk_DeleteItem()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new($"del-aws-{i}"),
                ["sk"] = new("item")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Delete Item")]
    public async Task<Goa.Clients.Dynamo.Operations.DeleteItem.DeleteItemResponse> Goa_DeleteItem()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.DeleteItemAsync(new Goa.Clients.Dynamo.Operations.DeleteItem.DeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = new() { S = $"del-goa-{i}" },
                ["sk"] = new() { S = "item" }
            }
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Delete Item")]
    public async Task<EfficientDeleteItemResponse> Efficient_DeleteItem()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.EfficientClient.DeleteItemAsync(new EfficientDynamoDb.Operations.DeleteItem.DeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", $"del-eff-{i}", "sk", "item")
        });
    }
}
