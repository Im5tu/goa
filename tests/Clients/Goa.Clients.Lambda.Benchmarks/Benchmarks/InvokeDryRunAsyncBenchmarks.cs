using Amazon.Lambda;
using Amazon.Lambda.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Lambda.Benchmarks.Infrastructure;

namespace Goa.Clients.Lambda.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class InvokeDryRunAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;
    private string _payload = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _payload = """{"message": "dry run"}""";
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Dry Run")]
    public async Task<InvokeResponse> AwsSdk_InvokeDryRun()
    {
        return await _fixture.AwsSdkClient.InvokeAsync(new InvokeRequest
        {
            FunctionName = _fixture.FunctionName,
            InvocationType = InvocationType.DryRun,
            Payload = _payload
        });
    }

    [Benchmark, BenchmarkCategory("Dry Run")]
    public async Task<Operations.InvokeDryRun.InvokeDryRunResponse> Goa_InvokeDryRun()
    {
        var response = await _fixture.GoaClient.InvokeDryRunAsync(new Operations.InvokeDryRun.InvokeDryRunRequest
        {
            FunctionName = _fixture.FunctionName,
            Payload = _payload
        });
        return response.Value;
    }
}
