using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Goa.Clients.Core.Configuration;
using Goa.Clients.Sqs;
using System.Collections.Concurrent;

namespace Goa.Clients.Sqs.Benchmarks.Infrastructure;

public class LocalStackFixture : IAsyncDisposable
{
    private IContainer? _container;
    private ServiceProvider? _serviceProvider;

    public ISqsClient GoaClient { get; private set; } = null!;
    public AmazonSQSClient AwsSdkClient { get; private set; } = null!;
    public string QueueUrl { get; private set; } = string.Empty;
    public string ServiceUrl { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        _container = new ContainerBuilder(new DockerImage("localstack/localstack"))
            .WithEnvironment("SERVICES", "sqs")
            .WithEnvironment("LOCALSTACK_HOST", "localhost")
            .WithPortBinding(4566, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(4566).ForPath("/_localstack/health")))
            .Build();

        await _container.StartAsync();

        ServiceUrl = $"http://localhost:{_container.GetMappedPublicPort(4566)}";

        AwsSdkClient = new AmazonSQSClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonSQSConfig
            {
                ServiceURL = ServiceUrl,
                AuthenticationRegion = "us-east-1"
            });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddStaticCredentials("test", "test");
        services.AddSqs(config =>
        {
            config.ServiceUrl = ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        GoaClient = _serviceProvider.GetRequiredService<ISqsClient>();

        var createResult = await AwsSdkClient.CreateQueueAsync(new CreateQueueRequest { QueueName = "benchmark-queue" });
        QueueUrl = createResult.QueueUrl;
    }

    public async Task SeedMessagesAsync(int count)
    {
        var entries = new List<SendMessageBatchRequestEntry>();
        for (var i = 0; i < count; i++)
        {
            entries.Add(new SendMessageBatchRequestEntry
            {
                Id = i.ToString(),
                MessageBody = $"seed-message-{i}"
            });

            if (entries.Count == 10 || i == count - 1)
            {
                var batch = new List<SendMessageBatchRequestEntry>(entries);
                var delay = 50;
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    var response = await AwsSdkClient.SendMessageBatchAsync(new SendMessageBatchRequest
                    {
                        QueueUrl = QueueUrl,
                        Entries = batch
                    });

                    if (response.Failed.Count == 0)
                        break;

                    if (attempt == 4)
                    {
                        var failedIds = string.Join(", ", response.Failed.Select(f => $"{f.Id}: {f.Code} - {f.Message}"));
                        throw new InvalidOperationException($"Failed to send {response.Failed.Count} message(s) after 5 attempts: {failedIds}");
                    }

                    var failedIdSet = new HashSet<string>(response.Failed.Select(f => f.Id));
                    batch = batch.Where(e => failedIdSet.Contains(e.Id)).ToList();
                    await Task.Delay(delay);
                    delay *= 2;
                }

                entries.Clear();
            }
        }
    }

    public async Task<ConcurrentQueue<string>> PreReceiveMessagesAsync(int count)
    {
        var receiptHandles = new ConcurrentQueue<string>();
        var seenMessageIds = new HashSet<string>();
        var emptyReceives = 0;
        const int maxEmptyReceives = 3;

        while (receiptHandles.Count < count)
        {
            var response = await AwsSdkClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = QueueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 5,
                VisibilityTimeout = 1800
            });

            var added = 0;
            foreach (var msg in response.Messages)
            {
                if (seenMessageIds.Add(msg.MessageId))
                {
                    receiptHandles.Enqueue(msg.ReceiptHandle);
                    added++;
                    if (receiptHandles.Count >= count) break;
                }
            }

            if (added == 0)
            {
                emptyReceives++;
                if (emptyReceives >= maxEmptyReceives)
                    throw new InvalidOperationException(
                        $"Failed to receive enough messages: got {receiptHandles.Count} of {count} after {maxEmptyReceives} consecutive empty receives.");
            }
            else
            {
                emptyReceives = 0;
            }
        }

        return receiptHandles;
    }

    public async ValueTask DisposeAsync()
    {
        AwsSdkClient?.Dispose();
        _serviceProvider?.Dispose();

        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
