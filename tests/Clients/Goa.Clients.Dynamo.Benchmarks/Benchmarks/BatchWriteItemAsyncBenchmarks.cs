using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.DocumentModel;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;

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
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Batch Write 25 Items")]
    public async Task<BatchWriteItemResponse> AwsSdk_BatchWriteItem()
    {
        var baseCounter = Interlocked.Add(ref _counter, 25);
        var items = new List<WriteRequest>();
        for (var i = 0; i < 25; i++)
        {
            items.Add(new WriteRequest(new PutRequest(new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
            {
                ["pk"] = new($"bw-aws-{baseCounter + i}"),
                ["sk"] = new("item"),
                ["data"] = new($"value-{baseCounter + i}")
            })));
        }

        return await _fixture.AwsSdkClient.BatchWriteItemAsync(new BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<WriteRequest>>
            {
                [_fixture.TableName] = items
            }
        });
    }

    [Benchmark, BenchmarkCategory("Batch Write 25 Items")]
    public async Task<Goa.Clients.Dynamo.Operations.Batch.BatchWriteItemResponse> Goa_BatchWriteItem()
    {
        var baseCounter = Interlocked.Add(ref _counter, 25);
        var items = new List<Goa.Clients.Dynamo.Operations.Batch.BatchWriteRequestItem>();
        for (var i = 0; i < 25; i++)
        {
            items.Add(new Goa.Clients.Dynamo.Operations.Batch.BatchWriteRequestItem
            {
                PutRequest = new Goa.Clients.Dynamo.Operations.Batch.PutRequest
                {
                    Item = new Dictionary<string, GoaModels.AttributeValue>
                    {
                        ["pk"] = new() { S = $"bw-goa-{baseCounter + i}" },
                        ["sk"] = new() { S = "item" },
                        ["data"] = new() { S = $"value-{baseCounter + i}" }
                    }
                }
            });
        }

        var response = await _fixture.GoaClient.BatchWriteItemAsync(new Goa.Clients.Dynamo.Operations.Batch.BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, List<Goa.Clients.Dynamo.Operations.Batch.BatchWriteRequestItem>>
            {
                [_fixture.TableName] = items
            }
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Batch Write 25 Items")]
    public async Task<EfficientDynamoDb.Operations.BatchWriteItem.BatchWriteItemResponse> Efficient_BatchWriteItem()
    {
        var baseCounter = Interlocked.Add(ref _counter, 25);
        var items = new List<EfficientDynamoDb.Operations.BatchWriteItem.BatchWriteOperation>();
        for (var i = 0; i < 25; i++)
        {
            items.Add(new EfficientDynamoDb.Operations.BatchWriteItem.BatchWriteOperation(
                new EfficientDynamoDb.Operations.BatchWriteItem.BatchWritePutRequest(new Document
                {
                    ["pk"] = $"bw-eff-{baseCounter + i}",
                    ["sk"] = "item",
                    ["data"] = $"value-{baseCounter + i}"
                })));
        }

        return await _fixture.EfficientClient.BatchWriteItemAsync(new EfficientDynamoDb.Operations.BatchWriteItem.BatchWriteItemRequest
        {
            RequestItems = new Dictionary<string, IReadOnlyList<EfficientDynamoDb.Operations.BatchWriteItem.BatchWriteOperation>>
            {
                [_fixture.TableName] = items
            }
        });
    }
}
