using BenchmarkDotNet.Order;
using EfficientDynamoDb.DocumentModel;
using EfficientDynamoDb.Operations.BatchWriteItem;
using AwsAttributeValue = Amazon.DynamoDBv2.Model.AttributeValue;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using Goa.Clients.Dynamo.Operations.Batch;
using GoaModels = Goa.Clients.Dynamo.Models;
using AwsBatchWriteItemRequest = Amazon.DynamoDBv2.Model.BatchWriteItemRequest;
using GoaBatchWriteItemRequest = Goa.Clients.Dynamo.Operations.Batch.BatchWriteItemRequest;
using GoaPutRequest = Goa.Clients.Dynamo.Operations.Batch.PutRequest;
using EfficientBatchWriteItemRequest = EfficientDynamoDb.Operations.BatchWriteItem.BatchWriteItemRequest;
using EfficientBatchWriteItemResponse = EfficientDynamoDb.Operations.BatchWriteItem.BatchWriteItemResponse;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class BatchWriteItemAsyncBenchmarks
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

    [Benchmark(Baseline = true), BenchmarkCategory("Batch Write 10 Items")]
    public async Task<Amazon.DynamoDBv2.Model.BatchWriteItemResponse> AwsSdk_BatchWrite_10Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var requests = new List<Amazon.DynamoDBv2.Model.WriteRequest>();
        for (var i = 0; i < 10; i++)
        {
            requests.Add(new Amazon.DynamoDBv2.Model.WriteRequest(new Amazon.DynamoDBv2.Model.PutRequest(
                new Dictionary<string, AwsAttributeValue>
                {
                    ["pk"] = new AwsAttributeValue($"aws-bw-{batch}"),
                    ["sk"] = new AwsAttributeValue($"item-{i:D4}"),
                    ["data"] = new AwsAttributeValue($"value-{i}")
                })));
        }
        return await _fixture.AwsSdkClient.BatchWriteItemAsync(new AwsBatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<Amazon.DynamoDBv2.Model.WriteRequest>>
            {
                [_fixture.TableName] = requests
            }
        });
    }

    [Benchmark, BenchmarkCategory("Batch Write 10 Items")]
    public async Task<bool> Goa_BatchWrite_10Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var requests = new List<BatchWriteRequestItem>();
        for (var i = 0; i < 10; i++)
        {
            requests.Add(new BatchWriteRequestItem
            {
                PutRequest = new GoaPutRequest
                {
                    Item = new Dictionary<string, GoaModels.AttributeValue>
                    {
                        ["pk"] = GoaModels.AttributeValue.String($"goa-bw-{batch}"),
                        ["sk"] = GoaModels.AttributeValue.String($"item-{i:D4}"),
                        ["data"] = GoaModels.AttributeValue.String($"value-{i}")
                    }
                }
            });
        }
        var response = await _fixture.GoaClient.BatchWriteItemAsync(new GoaBatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<BatchWriteRequestItem>>
            {
                [_fixture.TableName] = requests
            }
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Batch Write 10 Items")]
    public async Task<EfficientBatchWriteItemResponse> Efficient_BatchWrite_10Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var operations = new List<BatchWriteOperation>();
        for (var i = 0; i < 10; i++)
        {
            operations.Add(new BatchWriteOperation(new BatchWritePutRequest(new Document
            {
                ["pk"] = $"eff-bw-{batch}",
                ["sk"] = $"item-{i:D4}",
                ["data"] = $"value-{i}"
            })));
        }
        return await _fixture.EfficientClient.BatchWriteItemAsync(new EfficientBatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, IReadOnlyList<BatchWriteOperation>>
            {
                [_fixture.TableName] = operations
            }
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Batch Write 25 Items")]
    public async Task<Amazon.DynamoDBv2.Model.BatchWriteItemResponse> AwsSdk_BatchWrite_25Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var requests = new List<Amazon.DynamoDBv2.Model.WriteRequest>();
        for (var i = 0; i < 25; i++)
        {
            requests.Add(new Amazon.DynamoDBv2.Model.WriteRequest(new Amazon.DynamoDBv2.Model.PutRequest(
                new Dictionary<string, AwsAttributeValue>
                {
                    ["pk"] = new AwsAttributeValue($"aws-bw25-{batch}"),
                    ["sk"] = new AwsAttributeValue($"item-{i:D4}"),
                    ["data"] = new AwsAttributeValue($"value-{i}")
                })));
        }
        return await _fixture.AwsSdkClient.BatchWriteItemAsync(new AwsBatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<Amazon.DynamoDBv2.Model.WriteRequest>>
            {
                [_fixture.TableName] = requests
            }
        });
    }

    [Benchmark, BenchmarkCategory("Batch Write 25 Items")]
    public async Task<bool> Goa_BatchWrite_25Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var requests = new List<BatchWriteRequestItem>();
        for (var i = 0; i < 25; i++)
        {
            requests.Add(new BatchWriteRequestItem
            {
                PutRequest = new GoaPutRequest
                {
                    Item = new Dictionary<string, GoaModels.AttributeValue>
                    {
                        ["pk"] = GoaModels.AttributeValue.String($"goa-bw25-{batch}"),
                        ["sk"] = GoaModels.AttributeValue.String($"item-{i:D4}"),
                        ["data"] = GoaModels.AttributeValue.String($"value-{i}")
                    }
                }
            });
        }
        var response = await _fixture.GoaClient.BatchWriteItemAsync(new GoaBatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<BatchWriteRequestItem>>
            {
                [_fixture.TableName] = requests
            }
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Batch Write 25 Items")]
    public async Task<EfficientBatchWriteItemResponse> Efficient_BatchWrite_25Items()
    {
        var batch = Interlocked.Increment(ref _counter);
        var operations = new List<BatchWriteOperation>();
        for (var i = 0; i < 25; i++)
        {
            operations.Add(new BatchWriteOperation(new BatchWritePutRequest(new Document
            {
                ["pk"] = $"eff-bw25-{batch}",
                ["sk"] = $"item-{i:D4}",
                ["data"] = $"value-{i}"
            })));
        }
        return await _fixture.EfficientClient.BatchWriteItemAsync(new EfficientBatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, IReadOnlyList<BatchWriteOperation>>
            {
                [_fixture.TableName] = operations
            }
        });
    }
}
