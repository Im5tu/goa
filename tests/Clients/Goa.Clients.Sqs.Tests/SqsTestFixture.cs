using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Goa.Clients.Sqs.Tests;

public class SqsTestFixture : IAsyncInitializer, IAsyncDisposable
{
    private LocalStackFixture _localStack = null!;
    private ServiceProvider _serviceProvider = null!;

    public ISqsClient SqsClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _localStack = new LocalStackFixture();
        await _localStack.InitializeAsync();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddStaticCredentials("test", "test");
        services.AddSqs(config =>
        {
            config.ServiceUrl = _localStack.ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        SqsClient = _serviceProvider.GetRequiredService<ISqsClient>();

        await CreateTestQueueAsync();
    }

    public async Task<string> CreateTestQueueAsync()
    {
        var tempClient = new AmazonSQSClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonSQSConfig
        {
            ServiceURL = _localStack.ServiceUrl,
            UseHttp = true
        });

        var queueName = $"sqs-test-queue-{Guid.NewGuid():N}";
        var createQueueRequest = new CreateQueueRequest
        {
            QueueName = queueName
        };

        try
        {
            var response = await tempClient.CreateQueueAsync(createQueueRequest);
            return response.QueueUrl;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create test SQS queue: {ex.Message}", ex);
        }
        finally
        {
            tempClient.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var tempClient = new AmazonSQSClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonSQSConfig
            {
                ServiceURL = _localStack.ServiceUrl,
                UseHttp = true
            });

            try
            {
                foreach (var queue in (await tempClient.ListQueuesAsync("sqs-")).QueueUrls)
                {
                    await tempClient.DeleteQueueAsync(queue!);
                }
            }
            finally
            {
                tempClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to delete test SQS queue: {ex.Message}");
        }

        if (_localStack != null)
            await _localStack.DisposeAsync();

        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
