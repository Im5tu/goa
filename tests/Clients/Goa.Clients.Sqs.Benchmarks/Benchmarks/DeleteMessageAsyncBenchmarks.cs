using Amazon.SQS;
using Amazon.SQS.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Sqs.Benchmarks.Infrastructure;
using System.Collections.Concurrent;

namespace Goa.Clients.Sqs.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
[WarmupCount(10), IterationCount(50), InvocationCount(1)]
public class DeleteMessageAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;
    private ConcurrentQueue<string> _receiptHandles = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
        _fixture.SeedMessagesAsync(2000).GetAwaiter().GetResult();
        _receiptHandles = _fixture.PreReceiveMessagesAsync(2000).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup() => _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();

    [Benchmark(Baseline = true), BenchmarkCategory("Delete Message")]
    public async Task<DeleteMessageResponse> AwsSdk_DeleteMessage()
    {
        if (!_receiptHandles.TryDequeue(out var receiptHandle))
            throw new InvalidOperationException("Receipt handle pool exhausted. Increase seed count or reduce iteration count.");

        return await _fixture.AwsSdkClient.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            ReceiptHandle = receiptHandle
        });
    }

    [Benchmark, BenchmarkCategory("Delete Message")]
    public async Task<Operations.DeleteMessage.DeleteMessageResponse> Goa_DeleteMessage()
    {
        if (!_receiptHandles.TryDequeue(out var receiptHandle))
            throw new InvalidOperationException("Receipt handle pool exhausted. Increase seed count or reduce iteration count.");

        var response = await _fixture.GoaClient.DeleteMessageAsync(new Operations.DeleteMessage.DeleteMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            ReceiptHandle = receiptHandle
        });
        return response.Value;
    }
}
