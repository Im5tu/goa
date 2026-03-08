using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.DocumentModel;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using Goa.Clients.Dynamo.Benchmarks.Models;
using GoaModels = Goa.Clients.Dynamo.Models;
using GoaScanRequest = Goa.Clients.Dynamo.Operations.Scan.ScanRequest;
using EfficientScanRequest = EfficientDynamoDb.Operations.Scan.ScanRequest;
using EfficientAttributeValue = EfficientDynamoDb.DocumentModel.AttributeValue;
using AwsAttributeValue = Amazon.DynamoDBv2.Model.AttributeValue;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ScanAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();

        // Seed items spread across different partition keys for scan
        _fixture.SeedItemsAsync("scan-a", 50).GetAwaiter().GetResult();
        _fixture.SeedItemsAsync("scan-b", 50).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    // --- Scan (100 items, Limit=25, forces 4 pages) ---

    [Benchmark(Baseline = true), BenchmarkCategory("Scan")]
    public async Task<int> AwsSdk_Scan()
    {
        var count = 0;
        Dictionary<string, AwsAttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.AwsSdkClient.ScanAsync(new ScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan")]
    public async Task<int> Goa_Scan_DynamoRecord()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.GoaClient.ScanAsync(new GoaScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Value.Items.Count;
            lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan")]
    public async Task<int> Goa_Scan_Typed()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var result = await _fixture.GoaClient.ScanAsync<BenchmarkItem>(new GoaScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 25,
                ExclusiveStartKey = lastKey
            }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
            count += result.Value.Items.Count;
            lastKey = result.Value.HasMoreResults ? result.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan")]
    public async Task<int> Efficient_Scan()
    {
        var count = 0;
        IReadOnlyDictionary<string, EfficientAttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.EfficientClient.ScanAsync(new EfficientScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey;
        } while (lastKey != null);
        return count;
    }

    // --- Scan With Filter (Limit=25, paginate through filtered results) ---

    [Benchmark(Baseline = true), BenchmarkCategory("Scan With Filter")]
    public async Task<int> AwsSdk_Scan_WithFilter()
    {
        var count = 0;
        Dictionary<string, AwsAttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.AwsSdkClient.ScanAsync(new ScanRequest
            {
                TableName = _fixture.TableName,
                FilterExpression = "#n > :minNum",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#n"] = "number" },
                ExpressionAttributeValues = new Dictionary<string, AwsAttributeValue>
                {
                    [":minNum"] = new AwsAttributeValue { N = "25" }
                },
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan With Filter")]
    public async Task<int> Goa_Scan_WithFilter_DynamoRecord()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.GoaClient.ScanAsync(new GoaScanRequest
            {
                TableName = _fixture.TableName,
                FilterExpression = "#n > :minNum",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#n"] = "number" },
                ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
                {
                    [":minNum"] = GoaModels.AttributeValue.Number("25")
                },
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Value.Items.Count;
            lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan With Filter")]
    public async Task<int> Goa_Scan_WithFilter_Typed()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var result = await _fixture.GoaClient.ScanAsync<BenchmarkItem>(new GoaScanRequest
            {
                TableName = _fixture.TableName,
                FilterExpression = "#n > :minNum",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#n"] = "number" },
                ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
                {
                    [":minNum"] = GoaModels.AttributeValue.Number("25")
                },
                Limit = 25,
                ExclusiveStartKey = lastKey
            }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
            count += result.Value.Items.Count;
            lastKey = result.Value.HasMoreResults ? result.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan With Filter")]
    public async Task<int> Efficient_Scan_WithFilter()
    {
        var count = 0;
        IReadOnlyDictionary<string, EfficientAttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.EfficientClient.ScanAsync(new EfficientScanRequest
            {
                TableName = _fixture.TableName,
                FilterExpression = "#n > :minNum",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#n"] = "number" },
                ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
                {
                    [":minNum"] = new NumberAttributeValue("25")
                },
                Limit = 25,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey;
        } while (lastKey != null);
        return count;
    }

    // --- Scan With Limit (Limit=10, paginate through all results) ---

    [Benchmark(Baseline = true), BenchmarkCategory("Scan With Limit")]
    public async Task<int> AwsSdk_Scan_WithLimit()
    {
        var count = 0;
        Dictionary<string, AwsAttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.AwsSdkClient.ScanAsync(new ScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 10,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan With Limit")]
    public async Task<int> Goa_Scan_WithLimit_DynamoRecord()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.GoaClient.ScanAsync(new GoaScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 10,
                ExclusiveStartKey = lastKey
            });
            count += response.Value.Items.Count;
            lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan With Limit")]
    public async Task<int> Goa_Scan_WithLimit_Typed()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var result = await _fixture.GoaClient.ScanAsync<BenchmarkItem>(new GoaScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 10,
                ExclusiveStartKey = lastKey
            }, DynamoItemReaderRegistry.Get<BenchmarkItem>());
            count += result.Value.Items.Count;
            lastKey = result.Value.HasMoreResults ? result.Value.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan With Limit")]
    public async Task<int> Efficient_Scan_WithLimit()
    {
        var count = 0;
        IReadOnlyDictionary<string, EfficientAttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.EfficientClient.ScanAsync(new EfficientScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 10,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey;
        } while (lastKey != null);
        return count;
    }
}
