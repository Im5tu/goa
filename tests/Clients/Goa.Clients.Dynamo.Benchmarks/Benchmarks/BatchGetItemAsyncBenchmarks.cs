using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using EfficientAttributeValue = EfficientDynamoDb.DocumentModel.AttributeValue;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BatchGetItemAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _fixture.SeedItemsAsync("batch-get-bench", 25).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Batch Get 25 Items")]
    public async Task<BatchGetItemResponse> AwsSdk_BatchGetItem()
    {
        var keys = new List<Dictionary<string, AttributeValue>>();
        for (var i = 0; i < 25; i++)
        {
            keys.Add(new Dictionary<string, AttributeValue>
            {
                ["pk"] = new("batch-get-bench"),
                ["sk"] = new($"item-{i:D4}")
            });
        }

        return await _fixture.AwsSdkClient.BatchGetItemAsync(new BatchGetItemRequest
        {
            RequestItems = new Dictionary<string, KeysAndAttributes>
            {
                [_fixture.TableName] = new() { Keys = keys }
            }
        });
    }

    [Benchmark, BenchmarkCategory("Batch Get 25 Items")]
    public async Task<Goa.Clients.Dynamo.Operations.Batch.BatchGetItemResponse> Goa_BatchGetItem()
    {
        var keys = new List<Dictionary<string, GoaModels.AttributeValue>>();
        for (var i = 0; i < 25; i++)
        {
            keys.Add(new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = new() { S = "batch-get-bench" },
                ["sk"] = new() { S = $"item-{i:D4}" }
            });
        }

        var response = await _fixture.GoaClient.BatchGetItemAsync(new Goa.Clients.Dynamo.Operations.Batch.BatchGetItemRequest
        {
            RequestItems = new Dictionary<string, Goa.Clients.Dynamo.Operations.Batch.BatchGetRequestItem>
            {
                [_fixture.TableName] = new() { Keys = keys }
            }
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Batch Get 25 Items")]
    public async Task<EfficientDynamoDb.Operations.BatchGetItem.BatchGetItemResponse> Efficient_BatchGetItem()
    {
        var keys = new List<IReadOnlyDictionary<string, EfficientAttributeValue>>();
        for (var i = 0; i < 25; i++)
        {
            keys.Add(new Dictionary<string, EfficientAttributeValue>
            {
                ["pk"] = "batch-get-bench",
                ["sk"] = $"item-{i:D4}"
            });
        }

        return await _fixture.EfficientClient.BatchGetItemAsync(new EfficientDynamoDb.Operations.BatchGetItem.BatchGetItemRequest
        {
            RequestItems = new Dictionary<string, EfficientDynamoDb.Operations.BatchGetItem.TableBatchGetItemRequest>
            {
                [_fixture.TableName] = new() { Keys = keys }
            }
        });
    }
}
