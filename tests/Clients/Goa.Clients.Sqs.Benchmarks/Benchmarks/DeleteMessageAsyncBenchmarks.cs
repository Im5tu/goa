using Amazon.SQS;
using Amazon.SQS.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Sqs.Benchmarks.Infrastructure;
using System.Collections.Concurrent;

namespace Goa.Clients.Sqs.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
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
        _receiptHandles.TryDequeue(out var receiptHandle);
        return await _fixture.AwsSdkClient.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            ReceiptHandle = receiptHandle ?? "dummy-receipt-handle"
        });
    }

    [Benchmark, BenchmarkCategory("Delete Message")]
    public async Task<Operations.DeleteMessage.DeleteMessageResponse> Goa_DeleteMessage()
    {
        _receiptHandles.TryDequeue(out var receiptHandle);
        var response = await _fixture.GoaClient.DeleteMessageAsync(new Operations.DeleteMessage.DeleteMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            ReceiptHandle = receiptHandle ?? "dummy-receipt-handle"
        });
        return response.Value;
    }
}
