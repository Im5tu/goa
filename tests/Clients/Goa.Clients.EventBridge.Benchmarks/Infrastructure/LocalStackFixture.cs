using Amazon.EventBridge;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Goa.Clients.Core.Configuration;
using Goa.Clients.EventBridge;

namespace Goa.Clients.EventBridge.Benchmarks.Infrastructure;

public class LocalStackFixture : IAsyncDisposable
{
    private IContainer? _container;
    private ServiceProvider? _serviceProvider;

    public IEventBridgeClient GoaClient { get; private set; } = null!;
    public AmazonEventBridgeClient AwsSdkClient { get; private set; } = null!;
    public string ServiceUrl { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        _container = new ContainerBuilder(new DockerImage("localstack/localstack"))
            .WithEnvironment("SERVICES", "events")
            .WithEnvironment("LOCALSTACK_HOST", "localhost")
            .WithPortBinding(4566, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(4566).ForPath("/_localstack/health")))
            .Build();

        await _container.StartAsync();

        ServiceUrl = $"http://localhost:{_container.GetMappedPublicPort(4566)}";

        AwsSdkClient = new AmazonEventBridgeClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonEventBridgeConfig
            {
                ServiceURL = ServiceUrl,
                AuthenticationRegion = "us-east-1"
            });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddStaticCredentials("test", "test");
        services.AddEventBridge(config =>
        {
            config.ServiceUrl = ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        GoaClient = _serviceProvider.GetRequiredService<IEventBridgeClient>();
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
