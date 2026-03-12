using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using Goa.Clients.Dynamo.Operations.Batch;
using GoaModels = Goa.Clients.Dynamo.Models;
using AwsBatchGetItemRequest = Amazon.DynamoDBv2.Model.BatchGetItemRequest;
using GoaBatchGetItemRequest = Goa.Clients.Dynamo.Operations.Batch.BatchGetItemRequest;
using EfficientBatchGetItemRequest = EfficientDynamoDb.Operations.BatchGetItem.BatchGetItemRequest;
using EfficientBatchGetItemResponse = EfficientDynamoDb.Operations.BatchGetItem.BatchGetItemResponse;
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
        _fixture.SeedItemsAsync("batch-get", 25).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static List<Dictionary<string, AttributeValue>> CreateAwsKeys(int count)
    {
        var keys = new List<Dictionary<string, AttributeValue>>();
        for (var i = 0; i < count; i++)
        {
            keys.Add(new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue("batch-get"),
                ["sk"] = new AttributeValue($"item-{i:D4}")
            });
        }
        return keys;
    }

    private static List<Dictionary<string, GoaModels.AttributeValue>> CreateGoaKeys(int count)
    {
        var keys = new List<Dictionary<string, GoaModels.AttributeValue>>();
        for (var i = 0; i < count; i++)
        {
            keys.Add(new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("batch-get"),
                ["sk"] = GoaModels.AttributeValue.String($"item-{i:D4}")
            });
        }
        return keys;
    }

    private static List<IReadOnlyDictionary<string, EfficientAttributeValue>> CreateEfficientKeys(int count)
    {
        var keys = new List<IReadOnlyDictionary<string, EfficientAttributeValue>>();
        for (var i = 0; i < count; i++)
        {
            keys.Add(new Dictionary<string, EfficientAttributeValue>
            {
                ["pk"] = "batch-get",
                ["sk"] = $"item-{i:D4}"
            });
        }
        return keys;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Batch Get 10 Items")]
    public async Task<Amazon.DynamoDBv2.Model.BatchGetItemResponse> AwsSdk_BatchGet_10Items()
    {
        return await _fixture.AwsSdkClient.BatchGetItemAsync(new AwsBatchGetItemRequest
        {
            RequestItems = new Dictionary<string, KeysAndAttributes>
            {
                [_fixture.TableName] = new() { Keys = CreateAwsKeys(10) }
            }
        });
    }

    [Benchmark, BenchmarkCategory("Batch Get 10 Items")]
    public async Task<Goa.Clients.Dynamo.Operations.Batch.BatchGetItemResponse> Goa_BatchGet_10Items()
    {
        var response = await _fixture.GoaClient.BatchGetItemAsync(new GoaBatchGetItemRequest
        {
            RequestItems = new Dictionary<string, BatchGetRequestItem>
            {
                [_fixture.TableName] = new() { Keys = CreateGoaKeys(10) }
            }
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Batch Get 10 Items")]
    public async Task<EfficientBatchGetItemResponse> Efficient_BatchGet_10Items()
    {
        return await _fixture.EfficientClient.BatchGetItemAsync(new EfficientBatchGetItemRequest
        {
            RequestItems = new Dictionary<string, EfficientDynamoDb.Operations.BatchGetItem.TableBatchGetItemRequest>
            {
                [_fixture.TableName] = new() { Keys = CreateEfficientKeys(10) }
            }
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Batch Get 25 Items")]
    public async Task<Amazon.DynamoDBv2.Model.BatchGetItemResponse> AwsSdk_BatchGet_25Items()
    {
        return await _fixture.AwsSdkClient.BatchGetItemAsync(new AwsBatchGetItemRequest
        {
            RequestItems = new Dictionary<string, KeysAndAttributes>
            {
                [_fixture.TableName] = new() { Keys = CreateAwsKeys(25) }
            }
        });
    }

    [Benchmark, BenchmarkCategory("Batch Get 25 Items")]
    public async Task<Goa.Clients.Dynamo.Operations.Batch.BatchGetItemResponse> Goa_BatchGet_25Items()
    {
        var response = await _fixture.GoaClient.BatchGetItemAsync(new GoaBatchGetItemRequest
        {
            RequestItems = new Dictionary<string, BatchGetRequestItem>
            {
                [_fixture.TableName] = new() { Keys = CreateGoaKeys(25) }
            }
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Batch Get 25 Items")]
    public async Task<EfficientBatchGetItemResponse> Efficient_BatchGet_25Items()
    {
        return await _fixture.EfficientClient.BatchGetItemAsync(new EfficientBatchGetItemRequest
        {
            RequestItems = new Dictionary<string, EfficientDynamoDb.Operations.BatchGetItem.TableBatchGetItemRequest>
            {
                [_fixture.TableName] = new() { Keys = CreateEfficientKeys(25) }
            }
        });
    }
}
