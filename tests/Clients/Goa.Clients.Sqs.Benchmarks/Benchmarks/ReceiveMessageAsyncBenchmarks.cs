using Amazon.SQS;
using Amazon.SQS.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Sqs.Benchmarks.Infrastructure;

namespace Goa.Clients.Sqs.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ReceiveMessageAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _fixture.SeedMessagesAsync(1000).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Receive Messages")]
    public async Task<ReceiveMessageResponse> AwsSdk_ReceiveMessage()
    {
        return await _fixture.AwsSdkClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            MaxNumberOfMessages = 10,
            VisibilityTimeout = 0,
            WaitTimeSeconds = 0
        });
    }

    [Benchmark, BenchmarkCategory("Receive Messages")]
    public async Task<Operations.ReceiveMessage.ReceiveMessageResponse> Goa_ReceiveMessage()
    {
        var response = await _fixture.GoaClient.ReceiveMessageAsync(new Operations.ReceiveMessage.ReceiveMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            MaxNumberOfMessages = 10,
            VisibilityTimeout = 0,
            WaitTimeSeconds = 0
        });
        return response.Value;
    }
}
