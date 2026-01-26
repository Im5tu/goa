using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Testcontainers.LocalStack;
using TUnit.Core.Interfaces;

namespace Goa.Clients.Lambda.Tests;

public class LocalStackFixture : IAsyncInitializer, IAsyncDisposable
{
    private IContainer? _container;

    public string ServiceUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var unixSocket = "/var/run/docker.sock";
        _container = new ContainerBuilder(new DockerImage("localstack/localstack"))
            .WithEnvironment("SERVICES", "lambda")
            .WithEnvironment("DEBUG", "1")
            .WithEnvironment("DOCKER_HOST", $"unix://{unixSocket}")
            .WithEnvironment("LAMBDA_EXECUTOR", "docker")
            .WithPortBinding(4566, true)
            .WithBindMount(unixSocket, unixSocket)
            .Build();

        await _container.StartAsync();

        ServiceUrl = $"http://localhost:{_container.GetMappedPublicPort(4566)}";

        Console.WriteLine($"LocalStack started at: {ServiceUrl}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }

        GC.SuppressFinalize(this);
    }
}
