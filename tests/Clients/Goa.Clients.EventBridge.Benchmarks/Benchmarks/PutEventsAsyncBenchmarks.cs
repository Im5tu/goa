using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.EventBridge.Benchmarks.Infrastructure;

namespace Goa.Clients.EventBridge.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PutEventsAsyncBenchmarks
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

    [Benchmark(Baseline = true), BenchmarkCategory("Single Event")]
    public async Task<PutEventsResponse> AwsSdk_PutEvents_Single()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.PutEventsAsync(new PutEventsRequest
        {
            Entries =
            [
                new PutEventsRequestEntry
                {
                    Source = "benchmark.source",
                    DetailType = "BenchmarkEvent",
                    Detail = $"{{\"id\": {i}, \"message\": \"benchmark event\"}}"
                }
            ]
        });
    }

    [Benchmark, BenchmarkCategory("Single Event")]
    public async Task<Goa.Clients.EventBridge.Operations.PutEvents.PutEventsResponse> Goa_PutEvents_Single()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.PutEventsAsync(new Goa.Clients.EventBridge.Operations.PutEvents.PutEventsRequest
        {
            Entries =
            [
                new Goa.Clients.EventBridge.Models.EventEntry
                {
                    Source = "benchmark.source",
                    DetailType = "BenchmarkEvent",
                    Detail = $"{{\"id\": {i}, \"message\": \"benchmark event\"}}"
                }
            ]
        });
        return response.Value;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Batch 10 Events")]
    public async Task<PutEventsResponse> AwsSdk_PutEvents_Batch()
    {
        var baseCounter = Interlocked.Add(ref _counter, 10);
        var entries = new List<PutEventsRequestEntry>();
        for (var i = 0; i < 10; i++)
        {
            entries.Add(new PutEventsRequestEntry
            {
                Source = "benchmark.source",
                DetailType = "BenchmarkEvent",
                Detail = $"{{\"id\": {baseCounter + i}, \"message\": \"benchmark event\"}}"
            });
        }

        return await _fixture.AwsSdkClient.PutEventsAsync(new PutEventsRequest { Entries = entries });
    }

    [Benchmark, BenchmarkCategory("Batch 10 Events")]
    public async Task<Goa.Clients.EventBridge.Operations.PutEvents.PutEventsResponse> Goa_PutEvents_Batch()
    {
        var baseCounter = Interlocked.Add(ref _counter, 10);
        var entries = new List<Goa.Clients.EventBridge.Models.EventEntry>();
        for (var i = 0; i < 10; i++)
        {
            entries.Add(new Goa.Clients.EventBridge.Models.EventEntry
            {
                Source = "benchmark.source",
                DetailType = "BenchmarkEvent",
                Detail = $"{{\"id\": {baseCounter + i}, \"message\": \"benchmark event\"}}"
            });
        }

        var response = await _fixture.GoaClient.PutEventsAsync(new Goa.Clients.EventBridge.Operations.PutEvents.PutEventsRequest
        {
            Entries = entries
        });
        return response.Value;
    }
}
