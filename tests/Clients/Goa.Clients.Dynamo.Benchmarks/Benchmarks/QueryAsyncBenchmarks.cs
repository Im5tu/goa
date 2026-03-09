using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using Goa.Clients.Dynamo.Benchmarks.Models;
using GoaModels = Goa.Clients.Dynamo.Models;
using GoaQueryRequest = Goa.Clients.Dynamo.Operations.Query.QueryRequest;
using EfficientQueryRequest = EfficientDynamoDb.Operations.Query.QueryRequest;
using EfficientAttributeValue = EfficientDynamoDb.DocumentModel.AttributeValue;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class QueryAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _fixture.SeedItemsAsync("query-bench", 100).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Query 100 Items")]
    public async Task<int> AwsSdk_Query()
    {
        var count = 0;
        Dictionary<string, AttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.AwsSdkClient.QueryAsync(new QueryRequest
            {
                TableName = _fixture.TableName,
                KeyConditionExpression = "pk = :pk",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new("query-bench")
                },
                Limit = 20,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Query 100 Items")]
    public async Task<int> Goa_Query()
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
                    [":pk"] = new() { S = "query-bench" }
                },
                Limit = 20,
                ExclusiveStartKey = lastKey
            });
            count += response.Value.Items.Count;
            lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Query 100 Items")]
    public async Task<int> Efficient_Query()
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
                    [":pk"] = "query-bench"
                },
                Limit = 20,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Query 100 Items")]
    public async Task<int> Efficient_Query_Typed()
    {
        var count = 0;
        string? paginationToken = null;
        do
        {
            var page = await _fixture.EfficientContext.Query<BenchmarkEntity>()
                .WithKeyExpression(Condition<BenchmarkEntity>.On(x => x.Pk).EqualTo("query-bench"))
                .WithLimit(20)
                .WithPaginationToken(paginationToken)
                .ToPageAsync();
            count += page.Items.Count;
            paginationToken = page.PaginationToken;
        } while (paginationToken != null);
        return count;
    }
}
