using Amazon.SQS;
using Amazon.SQS.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Sqs.Benchmarks.Infrastructure;

namespace Goa.Clients.Sqs.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class SendMessageBatchAsyncBenchmarks
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

    [Benchmark(Baseline = true), BenchmarkCategory("Batch Send 10 Messages")]
    public async Task<SendMessageBatchResponse> AwsSdk_SendMessageBatch()
    {
        var baseCounter = Interlocked.Add(ref _counter, 10);
        var entries = new List<SendMessageBatchRequestEntry>();
        for (var i = 0; i < 10; i++)
        {
            entries.Add(new SendMessageBatchRequestEntry
            {
                Id = i.ToString(),
                MessageBody = $"batch-message-{baseCounter + i}"
            });
        }

        return await _fixture.AwsSdkClient.SendMessageBatchAsync(new SendMessageBatchRequest
        {
            QueueUrl = _fixture.QueueUrl,
            Entries = entries
        });
    }

    [Benchmark, BenchmarkCategory("Batch Send 10 Messages")]
    public async Task<Operations.SendMessageBatch.SendMessageBatchResponse> Goa_SendMessageBatch()
    {
        var baseCounter = Interlocked.Add(ref _counter, 10);
        var entries = new List<Operations.SendMessageBatch.SendMessageBatchRequestEntry>();
        for (var i = 0; i < 10; i++)
        {
            entries.Add(new Operations.SendMessageBatch.SendMessageBatchRequestEntry
            {
                Id = i.ToString(),
                MessageBody = $"batch-message-{baseCounter + i}"
            });
        }

        var response = await _fixture.GoaClient.SendMessageBatchAsync(new Operations.SendMessageBatch.SendMessageBatchRequest
        {
            QueueUrl = _fixture.QueueUrl,
            Entries = entries
        });
        return response.Value;
    }
}
