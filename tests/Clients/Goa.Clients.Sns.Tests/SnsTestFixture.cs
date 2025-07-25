using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Goa.Clients.Sns.Tests;

public class SnsTestFixture : IAsyncInitializer, IAsyncDisposable
{
    private LocalStackFixture _localStack = null!;
    private ServiceProvider _serviceProvider = null!;

    public ISnsClient SnsClient { get; private set; } = null!;
    public string TestTopicArn { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _localStack = new LocalStackFixture();
        await _localStack.InitializeAsync();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddStaticCredentials("test", "test");
        services.AddSns(config =>
        {
            config.ServiceUrl = _localStack.ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        SnsClient = _serviceProvider.GetRequiredService<ISnsClient>();

        await CreateTestTopicAsync();
    }

    private async Task CreateTestTopicAsync()
    {
        var tempClient = new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonSimpleNotificationServiceConfig
        {
            ServiceURL = _localStack.ServiceUrl,
            UseHttp = true
        });

        var topicName = $"test-topic-{Guid.NewGuid():N}";
        var createTopicRequest = new CreateTopicRequest
        {
            Name = topicName
        };

        try
        {
            var response = await tempClient.CreateTopicAsync(createTopicRequest);
            TestTopicArn = response.TopicArn;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create test SNS topic: {ex.Message}", ex);
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
            var tempClient = new AmazonSimpleNotificationServiceClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = _localStack.ServiceUrl,
                UseHttp = true
            });

            try
            {
                await tempClient.DeleteTopicAsync(TestTopicArn);
            }
            finally
            {
                tempClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to delete test SNS topic: {ex.Message}");
        }

        if (_localStack != null)
            await _localStack.DisposeAsync();

        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}