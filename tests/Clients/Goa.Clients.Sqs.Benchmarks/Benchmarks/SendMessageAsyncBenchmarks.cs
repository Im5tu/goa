using Amazon.SQS;
using Amazon.SQS.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Sqs.Benchmarks.Infrastructure;

namespace Goa.Clients.Sqs.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class SendMessageAsyncBenchmarks
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

    [Benchmark(Baseline = true), BenchmarkCategory("Send Simple Message")]
    public async Task<SendMessageResponse> AwsSdk_SendMessage()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            MessageBody = $"benchmark-message-{i}"
        });
    }

    [Benchmark, BenchmarkCategory("Send Simple Message")]
    public async Task<Operations.SendMessage.SendMessageResponse> Goa_SendMessage()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.SendMessageAsync(new Operations.SendMessage.SendMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            MessageBody = $"benchmark-message-{i}"
        });
        return response.Value;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Send With Attributes")]
    public async Task<SendMessageResponse> AwsSdk_SendMessageWithAttributes()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.SendMessageAsync(new SendMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            MessageBody = $"benchmark-message-{i}",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["attr1"] = new() { DataType = "String", StringValue = "value1" },
                ["attr2"] = new() { DataType = "Number", StringValue = "42" }
            }
        });
    }

    [Benchmark, BenchmarkCategory("Send With Attributes")]
    public async Task<Operations.SendMessage.SendMessageResponse> Goa_SendMessageWithAttributes()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.SendMessageAsync(new Operations.SendMessage.SendMessageRequest
        {
            QueueUrl = _fixture.QueueUrl,
            MessageBody = $"benchmark-message-{i}",
            MessageAttributes = new Dictionary<string, Goa.Clients.Sqs.Models.MessageAttributeValue>
            {
                ["attr1"] = new() { DataType = "String", StringValue = "value1" },
                ["attr2"] = new() { DataType = "Number", StringValue = "42" }
            }
        });
        return response.Value;
    }
}
