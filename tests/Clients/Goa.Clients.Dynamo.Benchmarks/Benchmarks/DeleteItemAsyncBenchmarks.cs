using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using EfficientDeleteItemRequest = EfficientDynamoDb.Operations.DeleteItem.DeleteItemRequest;
using EfficientDeleteItemResponse = EfficientDynamoDb.Operations.DeleteItem.DeleteItemResponse;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DeleteItemAsyncBenchmarks
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

    [Benchmark(Baseline = true), BenchmarkCategory("Delete Item")]
    public async Task<DeleteItemResponse> AwsSdk_DeleteItem()
    {
        // Delete a non-existent item (no-op but exercises full request path)
        var id = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue($"del-aws-{id}"),
                ["sk"] = new AttributeValue("item")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Delete Item")]
    public async Task<bool> Goa_DeleteItem()
    {
        var id = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.DeleteItemAsync(new Goa.Clients.Dynamo.Operations.DeleteItem.DeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String($"del-goa-{id}"),
                ["sk"] = GoaModels.AttributeValue.String("item")
            }
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Delete Item")]
    public async Task<EfficientDeleteItemResponse> Efficient_DeleteItem()
    {
        var id = Interlocked.Increment(ref _counter);
        return await _fixture.EfficientClient.DeleteItemAsync(new EfficientDeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", $"del-eff-{id}", "sk", "item")
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Delete Item With Return Values")]
    public async Task<DeleteItemResponse> AwsSdk_DeleteItem_WithReturnValues()
    {
        var id = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue($"delret-aws-{id}"),
                ["sk"] = new AttributeValue("item")
            },
            ReturnValues = ReturnValue.ALL_OLD
        });
    }

    [Benchmark, BenchmarkCategory("Delete Item With Return Values")]
    public async Task<GoaModels.DynamoRecord?> Goa_DeleteItem_WithReturnValues()
    {
        var id = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.DeleteItemAsync(new Goa.Clients.Dynamo.Operations.DeleteItem.DeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String($"delret-goa-{id}"),
                ["sk"] = GoaModels.AttributeValue.String("item")
            },
            ReturnValues = Goa.Clients.Dynamo.Enums.ReturnValues.ALL_OLD
        });
        return response.Value.Attributes;
    }

    [Benchmark, BenchmarkCategory("Delete Item With Return Values")]
    public async Task<EfficientDeleteItemResponse> Efficient_DeleteItem_WithReturnValues()
    {
        var id = Interlocked.Increment(ref _counter);
        return await _fixture.EfficientClient.DeleteItemAsync(new EfficientDeleteItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", $"delret-eff-{id}", "sk", "item"),
            ReturnValues = ReturnValues.AllOld
        });
    }
}
