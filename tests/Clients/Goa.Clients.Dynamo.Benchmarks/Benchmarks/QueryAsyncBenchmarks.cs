using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb;
using EfficientDynamoDb.Attributes;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using Goa.Clients.Dynamo.Benchmarks.Models;
using EfficientAttributeValue = EfficientDynamoDb.DocumentModel.AttributeValue;
using GoaModels = Goa.Clients.Dynamo.Models;
using GoaQueryRequest = Goa.Clients.Dynamo.Operations.Query.QueryRequest;
using EfficientQueryRequest = EfficientDynamoDb.Operations.Query.QueryRequest;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[DynamoDbTable("benchmark-table")]
public class BenchmarkEntity
{
    [DynamoDbProperty("pk", DynamoDbAttributeType.PartitionKey)]
    public string Pk { get; set; } = "";

    [DynamoDbProperty("sk", DynamoDbAttributeType.SortKey)]
    public string Sk { get; set; } = "";

    [DynamoDbProperty("data")]
    public string Data { get; set; } = "";

    [DynamoDbProperty("number")]
    public int Number { get; set; }

    [DynamoDbProperty("status")]
    public string Status { get; set; } = "";
}

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class QueryAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();

        // Seed items for query benchmarks
        _fixture.SeedItemsAsync("query-1", 1).GetAwaiter().GetResult();
        _fixture.SeedItemsAsync("query-10", 10).GetAwaiter().GetResult();
        _fixture.SeedItemsAsync("query-100", 100).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    // // --- Single item query ---
    //
    // [Benchmark(Baseline = true), BenchmarkCategory("1 Item")]
    // public async Task<int> AwsSdk_Query_1Item()
    // {
    //     var count = 0;
    //     Dictionary<string, AttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.AwsSdkClient.QueryAsync(new Amazon.DynamoDBv2.Model.QueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, AttributeValue>
    //             {
    //                 [":pk"] = new AttributeValue("query-1")
    //             },
    //             ExclusiveStartKey = lastKey
    //         });
    //         count += response.Items.Count;
    //         lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("1 Item")]
    // public async Task<int> Goa_Query_1Item_DynamoRecord()
    // {
    //     var count = 0;
    //     Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.GoaClient.QueryAsync(new GoaQueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
    //             {
    //                 [":pk"] = GoaModels.AttributeValue.String("query-1")
    //             },
    //             ExclusiveStartKey = lastKey
    //         });
    //
    //         count += response.Value.Items.Count;
    //         lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("1 Item")]
    // public async Task<int> Efficient_Query_1Item()
    // {
    //     var count = 0;
    //     IReadOnlyDictionary<string, EfficientAttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.EfficientClient.QueryAsync(new EfficientQueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
    //             {
    //                 [":pk"] = "query-1"
    //             },
    //             ExclusiveStartKey = lastKey
    //         });
    //         count += response.Items.Count;
    //         lastKey = response.LastEvaluatedKey;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("1 Item")]
    // public async Task<int> Efficient_Query_1Item_Typed()
    // {
    //     return (await _fixture.EfficientContext.Query<BenchmarkEntity>()
    //         .WithKeyExpression(Condition<BenchmarkEntity>.On(x => x.Pk).EqualTo("query-1"))
    //         .ToListAsync()).Count;
    // }

    // --- 10 item query (Limit=5, forces 2 pages) ---

    // [Benchmark(Baseline = true), BenchmarkCategory("10 Items")]
    // public async Task<int> AwsSdk_Query_10Items()
    // {
    //     var count = 0;
    //     Dictionary<string, AttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.AwsSdkClient.QueryAsync(new Amazon.DynamoDBv2.Model.QueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, AttributeValue>
    //             {
    //                 [":pk"] = new AttributeValue("query-10")
    //             },
    //             Limit = 5,
    //             ExclusiveStartKey = lastKey
    //         });
    //         count += response.Items.Count;
    //         lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("10 Items")]
    // public async Task<int> Goa_Query_10Items_DynamoRecord()
    // {
    //     var count = 0;
    //     Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.GoaClient.QueryAsync(new GoaQueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
    //             {
    //                 [":pk"] = GoaModels.AttributeValue.String("query-10")
    //             },
    //             Limit = 5,
    //             ExclusiveStartKey = lastKey
    //         });
    //         count += response.Value.Items.Count;
    //         lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("10 Items")]
    // public async Task<int> Goa_Query_10Items_Typed()
    // {
    //     var count = 0;
    //     Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
    //     do
    //     {
    //         var result = await _fixture.GoaClient.QueryAsync<BenchmarkItem>(new GoaQueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
    //             {
    //                 [":pk"] = GoaModels.AttributeValue.String("query-10")
    //             },
    //             Limit = 5,
    //             ExclusiveStartKey = lastKey
    //         }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
    //         count += result.Value.Items.Count;
    //         lastKey = result.Value.HasMoreResults ? result.Value.LastEvaluatedKey : null;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("10 Items")]
    // public async Task<int> Efficient_Query_10Items()
    // {
    //     var count = 0;
    //     IReadOnlyDictionary<string, EfficientAttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.EfficientClient.QueryAsync(new EfficientQueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
    //             {
    //                 [":pk"] = "query-10"
    //             },
    //             Limit = 5,
    //             ExclusiveStartKey = lastKey
    //         });
    //         count += response.Items.Count;
    //         lastKey = response.LastEvaluatedKey;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("10 Items")]
    // public async Task<int> Efficient_Query_10Items_Typed()
    // {
    //     var count = 0;
    //     string? paginationToken = null;
    //     do
    //     {
    //         var page = await _fixture.EfficientContext.Query<BenchmarkEntity>()
    //             .WithKeyExpression(Condition<BenchmarkEntity>.On(x => x.Pk).EqualTo("query-10"))
    //             .WithLimit(5)
    //             .WithPaginationToken(paginationToken)
    //             .ToPageAsync();
    //         count += page.Items.Count;
    //         paginationToken = page.PaginationToken;
    //     } while (paginationToken != null);
    //     return count;
    // }
    //
    // --- 100 item query (Limit=25, forces 4 pages) ---

    [Benchmark(Baseline = true), BenchmarkCategory("100 Items")]
    public async Task<int> AwsSdk_Query_100Items()
    {
        var count = 0;
        Dictionary<string, AttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.AwsSdkClient.QueryAsync(new Amazon.DynamoDBv2.Model.QueryRequest
            {
                TableName = _fixture.TableName,
                KeyConditionExpression = "pk = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue("query-100")
                },
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("100 Items")]
    public async Task<int> Goa_Query_100Items_DynamoRecord()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.GoaClient.QueryAsync(new GoaQueryRequest
            {
                TableName = _fixture.TableName,
                KeyConditionExpression = "pk = :pk",
                ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
                {
                    [":pk"] = GoaModels.AttributeValue.String("query-100")
                },
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Value.Items.Count;
            lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("100 Items")]
    public async Task<int> Goa_Query_100Items_Typed()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var result = await _fixture.GoaClient.QueryAsync<BenchmarkItem>(new GoaQueryRequest
            {
                TableName = _fixture.TableName,
                KeyConditionExpression = "pk = :pk",
                ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
                {
                    [":pk"] = GoaModels.AttributeValue.String("query-100")
                },
                Limit = 25,
                ExclusiveStartKey = lastKey
            }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
            count += result.Value.Items.Count;
            lastKey = result.Value.HasMoreResults ? result.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("100 Items")]
    public async Task<int> Efficient_Query_100Items()
    {
        var count = 0;
        IReadOnlyDictionary<string, EfficientAttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.EfficientClient.QueryAsync(new EfficientQueryRequest
            {
                TableName = _fixture.TableName,
                KeyConditionExpression = "pk = :pk",
                ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
                {
                    [":pk"] = "query-100"
                },
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("100 Items")]
    public async Task<int> Efficient_Query_100Items_Typed()
    {
        var count = 0;
        string? paginationToken = null;
        do
        {
            var page = await _fixture.EfficientContext.Query<BenchmarkEntity>()
                .WithKeyExpression(Condition<BenchmarkEntity>.On(x => x.Pk).EqualTo("query-100"))
                .WithLimit(25)
                .WithPaginationToken(paginationToken)
                .ToPageAsync();
            count += page.Items.Count;
            paginationToken = page.PaginationToken;
        } while (paginationToken != null);
        return count;
    }

    // // --- Empty query ---
    //
    // [Benchmark(Baseline = true), BenchmarkCategory("No Results")]
    // public async Task<int> AwsSdk_Query_NoResults()
    // {
    //     var count = 0;
    //     Dictionary<string, AttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.AwsSdkClient.QueryAsync(new Amazon.DynamoDBv2.Model.QueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, AttributeValue>
    //             {
    //                 [":pk"] = new AttributeValue("nonexistent-pk")
    //             },
    //             ExclusiveStartKey = lastKey
    //         });
    //         count += response.Items.Count;
    //         lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("No Results")]
    // public async Task<int> Goa_Query_NoResults_DynamoRecord()
    // {
    //     var count = 0;
    //     Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.GoaClient.QueryAsync(new GoaQueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
    //             {
    //                 [":pk"] = GoaModels.AttributeValue.String("nonexistent-pk")
    //             },
    //             ExclusiveStartKey = lastKey
    //         });
    //         count += response.Value.Items.Count;
    //         lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("No Results")]
    // public async Task<int> Goa_Query_NoResults_Typed()
    // {
    //     var count = 0;
    //     Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
    //     do
    //     {
    //         var result = await _fixture.GoaClient.QueryAsync<BenchmarkItem>(new GoaQueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
    //             {
    //                 [":pk"] = GoaModels.AttributeValue.String("nonexistent-pk")
    //             },
    //             ExclusiveStartKey = lastKey
    //         }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
    //         count += result.Value.Items.Count;
    //         lastKey = result.Value.HasMoreResults ? result.Value.LastEvaluatedKey : null;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("No Results")]
    // public async Task<int> Efficient_Query_NoResults()
    // {
    //     var count = 0;
    //     IReadOnlyDictionary<string, EfficientAttributeValue>? lastKey = null;
    //     do
    //     {
    //         var response = await _fixture.EfficientClient.QueryAsync(new EfficientQueryRequest
    //         {
    //             TableName = _fixture.TableName,
    //             KeyConditionExpression = "pk = :pk",
    //             ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
    //             {
    //                 [":pk"] = "nonexistent-pk"
    //             },
    //             ExclusiveStartKey = lastKey
    //         });
    //         count += response.Items.Count;
    //         lastKey = response.LastEvaluatedKey;
    //     } while (lastKey != null);
    //     return count;
    // }
    //
    // [Benchmark, BenchmarkCategory("No Results")]
    // public async Task<int> Efficient_Query_NoResults_Typed()
    // {
    //     return (await _fixture.EfficientContext.Query<BenchmarkEntity>()
    //         .WithKeyExpression(Condition<BenchmarkEntity>.On(x => x.Pk).EqualTo("nonexistent-pk"))
    //         .ToListAsync()).Count;
    // }
}
