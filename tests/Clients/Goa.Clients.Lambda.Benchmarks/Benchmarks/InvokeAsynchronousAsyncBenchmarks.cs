using Amazon.Lambda;
using Amazon.Lambda.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Lambda.Benchmarks.Infrastructure;

namespace Goa.Clients.Lambda.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class InvokeAsynchronousAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;
    private string _payload = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _payload = """{"message": "async invoke"}""";
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Async Invoke")]
    public async Task<InvokeResponse> AwsSdk_InvokeAsync()
    {
        return await _fixture.AwsSdkClient.InvokeAsync(new InvokeRequest
        {
            FunctionName = _fixture.FunctionName,
            InvocationType = InvocationType.Event,
            Payload = _payload
        });
    }

    [Benchmark, BenchmarkCategory("Async Invoke")]
    public async Task<Operations.InvokeAsync.InvokeAsyncResponse> Goa_InvokeAsync()
    {
        var response = await _fixture.GoaClient.InvokeAsynchronousAsync(new Operations.InvokeAsync.InvokeAsyncRequest
        {
            FunctionName = _fixture.FunctionName,
            Payload = _payload
        });
        return response.Value;
    }
}
