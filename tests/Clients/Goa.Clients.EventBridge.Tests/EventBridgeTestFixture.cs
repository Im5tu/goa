using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.Runtime;
using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Goa.Clients.EventBridge.Tests;

public class EventBridgeTestFixture : IAsyncInitializer, IAsyncDisposable
{
    private LocalStackFixture _localStack = null!;
    private ServiceProvider _serviceProvider = null!;

    public IEventBridgeClient EventBridgeClient { get; private set; } = null!;
    public string TestEventBusName { get; } = $"test-event-bus-{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        _localStack = new LocalStackFixture();
        await _localStack.InitializeAsync();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddStaticCredentials("test", "test");
        services.AddEventBridge(config =>
        {
            config.ServiceUrl = _localStack.ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        EventBridgeClient = _serviceProvider.GetRequiredService<IEventBridgeClient>();

        await CreateTestEventBusAsync();
    }

    private async Task CreateTestEventBusAsync()
    {
        var tempClient = new AmazonEventBridgeClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonEventBridgeConfig
        {
            ServiceURL = _localStack.ServiceUrl,
            UseHttp = true
        });

        var createEventBusRequest = new CreateEventBusRequest
        {
            Name = TestEventBusName
        };

        try
        {
            await tempClient.CreateEventBusAsync(createEventBusRequest);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create test event bus: {ex.Message}", ex);
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
            var tempClient = new AmazonEventBridgeClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonEventBridgeConfig
            {
                ServiceURL = _localStack.ServiceUrl,
                UseHttp = true
            });

            try
            {
                await tempClient.DeleteEventBusAsync(new DeleteEventBusRequest { Name = TestEventBusName });
            }
            finally
            {
                tempClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to delete test event bus: {ex.Message}");
        }

        if (_localStack != null)
            await _localStack.DisposeAsync();

        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}