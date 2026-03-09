using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using BenchmarkDotNet.Order;
using Goa.Clients.Sns.Benchmarks.Infrastructure;
using Goa.Clients.Sns.Models;

namespace Goa.Clients.Sns.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PublishAsyncBenchmarks
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

    [Benchmark(Baseline = true), BenchmarkCategory("Publish Simple")]
    public async Task<PublishResponse> AwsSdk_Publish()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.PublishAsync(new PublishRequest
        {
            TopicArn = _fixture.TopicArn,
            Message = $"benchmark-message-{i}"
        });
    }

    [Benchmark, BenchmarkCategory("Publish Simple")]
    public async Task<Goa.Clients.Sns.Operations.Publish.PublishResponse> Goa_Publish()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.PublishAsync(new Goa.Clients.Sns.Operations.Publish.PublishRequest
        {
            TopicArn = _fixture.TopicArn,
            Message = $"benchmark-message-{i}"
        });
        return response.Value;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Publish With Attributes")]
    public async Task<PublishResponse> AwsSdk_PublishWithAttributes()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.PublishAsync(new PublishRequest
        {
            TopicArn = _fixture.TopicArn,
            Message = $"benchmark-message-{i}",
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["attr1"] = new() { DataType = "String", StringValue = "value1" },
                ["attr2"] = new() { DataType = "Number", StringValue = "42" }
            }
        });
    }

    [Benchmark, BenchmarkCategory("Publish With Attributes")]
    public async Task<Goa.Clients.Sns.Operations.Publish.PublishResponse> Goa_PublishWithAttributes()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.PublishAsync(new Goa.Clients.Sns.Operations.Publish.PublishRequest
        {
            TopicArn = _fixture.TopicArn,
            Message = $"benchmark-message-{i}",
            MessageAttributes = new Dictionary<string, SnsMessageAttributeValue>
            {
                ["attr1"] = SnsMessageAttributeValue.Create("value1"),
                ["attr2"] = SnsMessageAttributeValue.Create("42", "Number")
            }
        });
        return response.Value;
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Publish With Subject")]
    public async Task<PublishResponse> AwsSdk_PublishWithSubject()
    {
        var i = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.PublishAsync(new PublishRequest
        {
            TopicArn = _fixture.TopicArn,
            Message = $"benchmark-message-{i}",
            Subject = "Benchmark Subject"
        });
    }

    [Benchmark, BenchmarkCategory("Publish With Subject")]
    public async Task<Goa.Clients.Sns.Operations.Publish.PublishResponse> Goa_PublishWithSubject()
    {
        var i = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.PublishAsync(new Goa.Clients.Sns.Operations.Publish.PublishRequest
        {
            TopicArn = _fixture.TopicArn,
            Message = $"benchmark-message-{i}",
            Subject = "Benchmark Subject"
        });
        return response.Value;
    }
}
