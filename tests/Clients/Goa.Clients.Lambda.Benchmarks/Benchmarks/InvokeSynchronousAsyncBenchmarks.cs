using Amazon.Lambda;
using Amazon.Lambda.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Lambda.Benchmarks.Infrastructure;

namespace Goa.Clients.Lambda.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class InvokeSynchronousAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;
    private string _smallPayload = null!;
    private string _mediumPayload = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _smallPayload = """{"message": "hello world"}""";
        _mediumPayload = GenerateMediumPayload();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Small Payload")]
    public async Task<InvokeResponse> AwsSdk_Invoke_Small()
    {
        return await _fixture.AwsSdkClient.InvokeAsync(new InvokeRequest
        {
            FunctionName = _fixture.FunctionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = _smallPayload
        });
    }

    [Benchmark, BenchmarkCategory("Small Payload")]
    public async Task<Operations.Invoke.InvokeResponse> Goa_Invoke_Small()
    {
        var response = await _fixture.GoaClient.InvokeSynchronousAsync(new Operations.Invoke.InvokeRequest
        {
            FunctionName = _fixture.FunctionName,
            Payload = _smallPayload
        });
        return response.Value;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Medium Payload")]
    public async Task<InvokeResponse> AwsSdk_Invoke_Medium()
    {
        return await _fixture.AwsSdkClient.InvokeAsync(new InvokeRequest
        {
            FunctionName = _fixture.FunctionName,
            InvocationType = InvocationType.RequestResponse,
            Payload = _mediumPayload
        });
    }

    [Benchmark, BenchmarkCategory("Medium Payload")]
    public async Task<Operations.Invoke.InvokeResponse> Goa_Invoke_Medium()
    {
        var response = await _fixture.GoaClient.InvokeSynchronousAsync(new Operations.Invoke.InvokeRequest
        {
            FunctionName = _fixture.FunctionName,
            Payload = _mediumPayload
        });
        return response.Value;
    }

    private static string GenerateMediumPayload()
    {
        var items = new List<string>();
        for (var i = 0; i < 100; i++)
        {
            items.Add($"\"item_{i}\": \"value_{i}_{'x'.ToString().PadRight(80, 'x')}\"");
        }
        return "{" + string.Join(",", items) + "}";
    }
}
