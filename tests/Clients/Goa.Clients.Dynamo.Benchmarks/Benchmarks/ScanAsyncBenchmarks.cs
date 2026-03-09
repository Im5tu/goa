using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using GoaScanRequest = Goa.Clients.Dynamo.Operations.Scan.ScanRequest;
using EfficientScanRequest = EfficientDynamoDb.Operations.Scan.ScanRequest;
using EfficientAttributeValue = EfficientDynamoDb.DocumentModel.AttributeValue;

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
        _fixture.SeedItemsAsync("scan-bench", 100).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Scan")]
    public async Task<int> AwsSdk_Scan()
    {
        var count = 0;
        Dictionary<string, AttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.AwsSdkClient.ScanAsync(new ScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 20,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
        } while (lastKey != null);
        return count;
    }

    [Benchmark, BenchmarkCategory("Scan")]
    public async Task<int> Goa_Scan()
    {
        var count = 0;
        Dictionary<string, GoaModels.AttributeValue>? lastKey = null;
        do
        {
            var response = await _fixture.GoaClient.ScanAsync(new GoaScanRequest
            {
                TableName = _fixture.TableName,
                Limit = 20,
                ExclusiveStartKey = lastKey
            });
            count += response.Value.Items.Count;
            lastKey = response.Value.HasMoreResults ? response.Value.LastEvaluatedKey : null;
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
                Limit = 20,
                ExclusiveStartKey = lastKey
            });
            count += response.Items.Count;
            lastKey = response.LastEvaluatedKey;
        } while (lastKey != null);
        return count;
    }
}
