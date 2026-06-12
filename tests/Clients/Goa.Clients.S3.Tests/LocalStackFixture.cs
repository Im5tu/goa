using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using TUnit.Core.Interfaces;

namespace Goa.Clients.S3.Tests;

public class LocalStackFixture : IAsyncInitializer, IAsyncDisposable
{
    private IContainer? _container;

    public string ServiceUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        // Pinned to the v4 community line: localstack/localstack:latest (2026+) requires a licence.
        _container = new ContainerBuilder(new DockerImage("localstack/localstack:4"))
            .WithEnvironment("SERVICES", "s3")
            // Ask LocalStack to validate SigV4 signatures rather than skipping them, so the
            // integration tests exercise real request signing end-to-end. (Note: the v4 community
            // image does not strictly enforce this for every request, but the signed requests are
            // additionally proven correct against the AWS SDK in RequestSignerComparisonTests.)
            .WithEnvironment("S3_SKIP_SIGNATURE_VALIDATION", "0")
            .WithEnvironment("DEBUG", "1")
            .WithEnvironment("DOCKER_HOST", "unix:///var/run/docker.sock")
            .WithEnvironment("LOCALSTACK_HOST", "localhost")
            .WithPortBinding(4566, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(4566).ForPath("/_localstack/health")))
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
