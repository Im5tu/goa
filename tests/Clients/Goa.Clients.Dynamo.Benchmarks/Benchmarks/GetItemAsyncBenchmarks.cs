using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using Goa.Clients.Dynamo.Benchmarks.Models;
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

        // Seed one item for "hit" benchmarks
        _fixture.SeedItemsAsync("get-bench", 1).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Get Item")]
    public async Task<GetItemResponse> AwsSdk_GetItem()
    {
        return await _fixture.AwsSdkClient.GetItemAsync(new GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue("get-bench"),
                ["sk"] = new AttributeValue("item-0000")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Get Item")]
    public async Task<GoaModels.DynamoRecord?> Goa_GetItem_DynamoRecord()
    {
        var response = await _fixture.GoaClient.GetItemAsync(new Goa.Clients.Dynamo.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("get-bench"),
                ["sk"] = GoaModels.AttributeValue.String("item-0000")
            }
        });
        return response.Value.Item;
    }

    [Benchmark, BenchmarkCategory("Get Item")]
    public async Task<BenchmarkItem?> Goa_GetItem_Typed()
    {
        var result = await _fixture.GoaClient.GetItemAsync<BenchmarkItem>(new Goa.Clients.Dynamo.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("get-bench"),
                ["sk"] = GoaModels.AttributeValue.String("item-0000")
            }
        }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
        return result.Value;
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

    [Benchmark(Baseline = true), BenchmarkCategory("Get Item Miss")]
    public async Task<GetItemResponse> AwsSdk_GetItem_Miss()
    {
        return await _fixture.AwsSdkClient.GetItemAsync(new GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue("nonexistent"),
                ["sk"] = new AttributeValue("nonexistent")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Get Item Miss")]
    public async Task<GoaModels.DynamoRecord?> Goa_GetItem_Miss_DynamoRecord()
    {
        var response = await _fixture.GoaClient.GetItemAsync(new Goa.Clients.Dynamo.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("nonexistent"),
                ["sk"] = GoaModels.AttributeValue.String("nonexistent")
            }
        });
        return response.Value.Item;
    }

    [Benchmark, BenchmarkCategory("Get Item Miss")]
    public async Task<BenchmarkItem?> Goa_GetItem_Miss_Typed()
    {
        var result = await _fixture.GoaClient.GetItemAsync<BenchmarkItem>(new Goa.Clients.Dynamo.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("nonexistent"),
                ["sk"] = GoaModels.AttributeValue.String("nonexistent")
            }
        }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
        return result.Value;
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

    [Benchmark(Baseline = true), BenchmarkCategory("Get Item Consistent Read")]
    public async Task<GetItemResponse> AwsSdk_GetItem_ConsistentRead()
    {
        return await _fixture.AwsSdkClient.GetItemAsync(new GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue("get-bench"),
                ["sk"] = new AttributeValue("item-0000")
            },
            ConsistentRead = true
        });
    }

    [Benchmark, BenchmarkCategory("Get Item Consistent Read")]
    public async Task<GoaModels.DynamoRecord?> Goa_GetItem_ConsistentRead_DynamoRecord()
    {
        var response = await _fixture.GoaClient.GetItemAsync(new Goa.Clients.Dynamo.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("get-bench"),
                ["sk"] = GoaModels.AttributeValue.String("item-0000")
            },
            ConsistentRead = true
        });
        return response.Value.Item;
    }

    [Benchmark, BenchmarkCategory("Get Item Consistent Read")]
    public async Task<BenchmarkItem?> Goa_GetItem_ConsistentRead_Typed()
    {
        var result = await _fixture.GoaClient.GetItemAsync<BenchmarkItem>(new Goa.Clients.Dynamo.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("get-bench"),
                ["sk"] = GoaModels.AttributeValue.String("item-0000")
            },
            ConsistentRead = true
        }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
        return result.Value;
    }

    [Benchmark, BenchmarkCategory("Get Item Consistent Read")]
    public async Task<EfficientGetItemResponse> Efficient_GetItem_ConsistentRead()
    {
        return await _fixture.EfficientClient.GetItemAsync(new EfficientDynamoDb.Operations.GetItem.GetItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", "get-bench", "sk", "item-0000"),
            ConsistentRead = true
        });
    }
}
