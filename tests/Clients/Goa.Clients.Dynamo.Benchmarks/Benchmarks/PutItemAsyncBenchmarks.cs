using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.DocumentModel;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using EfficientPutItemResponse = EfficientDynamoDb.Operations.PutItem.PutItemResponse;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PutItemAsyncBenchmarks
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
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Put Item")]
    public async Task<PutItemResponse> AwsSdk_PutItem()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.PutItemAsync(new PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
            {
                ["pk"] = new($"put-aws-{i}"),
                ["sk"] = new("item"),
                ["data"] = new($"value-{i}"),
                ["number"] = new() { N = i.ToString() },
                ["status"] = new("active")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Put Item")]
    public async Task<Goa.Clients.Dynamo.Operations.PutItem.PutItemResponse> Goa_PutItem()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.PutItemAsync(new Goa.Clients.Dynamo.Operations.PutItem.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = new() { S = $"put-goa-{i}" },
                ["sk"] = new() { S = "item" },
                ["data"] = new() { S = $"value-{i}" },
                ["number"] = new() { N = i.ToString() },
                ["status"] = new() { S = "active" }
            }
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Put Item")]
    public async Task<EfficientPutItemResponse> Efficient_PutItem()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.EfficientClient.PutItemAsync(new EfficientDynamoDb.Operations.PutItem.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Document
            {
                ["pk"] = $"put-eff-{i}",
                ["sk"] = "item",
                ["data"] = $"value-{i}",
                ["number"] = i,
                ["status"] = "active"
            }
        });
    }
}
