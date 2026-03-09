using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.DocumentModel;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class TransactWriteItemsAsyncBenchmarks
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

    [Benchmark(Baseline = true), BenchmarkCategory("Transact Write 10 Items")]
    public async Task<TransactWriteItemsResponse> AwsSdk_TransactWriteItems()
    {
        var baseCounter = Interlocked.Add(ref _counter, 10);
        var items = new List<TransactWriteItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new TransactWriteItem
            {
                Put = new Put
                {
                    TableName = _fixture.TableName,
                    Item = new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
                    {
                        ["pk"] = new($"txw-aws-{baseCounter + i}"),
                        ["sk"] = new("item"),
                        ["data"] = new($"value-{baseCounter + i}")
                    }
                }
            });
        }

        return await _fixture.AwsSdkClient.TransactWriteItemsAsync(new TransactWriteItemsRequest
        {
            TransactItems = items
        });
    }

    [Benchmark, BenchmarkCategory("Transact Write 10 Items")]
    public async Task<Goa.Clients.Dynamo.Operations.Transactions.TransactWriteItemResponse> Goa_TransactWriteItems()
    {
        var baseCounter = Interlocked.Add(ref _counter, 10);
        var items = new List<Goa.Clients.Dynamo.Operations.Transactions.TransactWriteItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new Goa.Clients.Dynamo.Operations.Transactions.TransactWriteItem
            {
                Put = new Goa.Clients.Dynamo.Operations.Transactions.TransactPutItem
                {
                    TableName = _fixture.TableName,
                    Item = new Dictionary<string, GoaModels.AttributeValue>
                    {
                        ["pk"] = new() { S = $"txw-goa-{baseCounter + i}" },
                        ["sk"] = new() { S = "item" },
                        ["data"] = new() { S = $"value-{baseCounter + i}" }
                    }
                }
            });
        }

        var response = await _fixture.GoaClient.TransactWriteItemsAsync(new Goa.Clients.Dynamo.Operations.Transactions.TransactWriteRequest
        {
            TransactItems = items
        });
        return response.Value;
    }

    [Benchmark, BenchmarkCategory("Transact Write 10 Items")]
    public async Task<EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItemsResponse> Efficient_TransactWriteItems()
    {
        var baseCounter = Interlocked.Add(ref _counter, 10);
        var items = new List<EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItem>();
        for (var i = 0; i < 10; i++)
        {
            items.Add(new EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItem(
                new EfficientDynamoDb.Operations.TransactWriteItems.TransactPutItem
                {
                    TableName = _fixture.TableName,
                    Item = new Document
                    {
                        ["pk"] = $"txw-eff-{baseCounter + i}",
                        ["sk"] = "item",
                        ["data"] = $"value-{baseCounter + i}"
                    }
                }));
        }

        return await _fixture.EfficientClient.TransactWriteItemsAsync(new EfficientDynamoDb.Operations.TransactWriteItems.TransactWriteItemsRequest
        {
            TransactItems = items
        });
    }
}
