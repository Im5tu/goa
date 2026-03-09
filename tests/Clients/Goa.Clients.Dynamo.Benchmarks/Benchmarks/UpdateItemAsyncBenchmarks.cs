using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using EfficientUpdateItemResponse = EfficientDynamoDb.Operations.UpdateItem.UpdateItemResponse;
using EfficientAttributeValue = EfficientDynamoDb.DocumentModel.AttributeValue;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class UpdateItemAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;
    private int _counter;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _fixture.SeedItemsAsync("update-bench", 1).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Update Item")]
    public async Task<UpdateItemResponse> AwsSdk_UpdateItem()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new("update-bench"),
                ["sk"] = new("item-0000")
            },
            UpdateExpression = "SET #d = :d",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":d"] = new($"updated-{i}")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Update Item")]
    public async Task<Goa.Clients.Dynamo.Operations.UpdateItem.UpdateItemResponse> Goa_UpdateItem()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.UpdateItemAsync(new Goa.Clients.Dynamo.Operations.UpdateItem.UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = new() { S = "update-bench" },
                ["sk"] = new() { S = "item-0000" }
            },
            UpdateExpression = "SET #d = :d",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
            ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
            {
                [":d"] = new() { S = $"updated-{i}" }
            }
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Update Item")]
    public async Task<EfficientUpdateItemResponse> Efficient_UpdateItem()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.EfficientClient.UpdateItemAsync(new EfficientDynamoDb.Operations.UpdateItem.UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", "update-bench", "sk", "item-0000"),
            UpdateExpression = "SET #d = :d",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
            ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
            {
                [":d"] = $"updated-{i}"
            }
        });
    }
}
