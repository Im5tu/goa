using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Goa.Clients.Core.Configuration;
using Goa.Clients.Lambda;
using System.IO.Compression;

namespace Goa.Clients.Lambda.Benchmarks.Infrastructure;

public class LocalStackFixture : IAsyncDisposable
{
    private IContainer? _container;
    private ServiceProvider? _serviceProvider;

    public ILambdaClient GoaClient { get; private set; } = null!;
    public AmazonLambdaClient AwsSdkClient { get; private set; } = null!;
    public string FunctionName { get; } = "benchmark-echo";
    public string ServiceUrl { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        _container = new ContainerBuilder(new DockerImage("localstack/localstack"))
            .WithEnvironment("SERVICES", "lambda")
            .WithEnvironment("LOCALSTACK_HOST", "localhost")
            .WithEnvironment("LAMBDA_EXECUTOR", "docker")
            .WithPortBinding(4566, true)
            .WithBindMount("/var/run/docker.sock", "/var/run/docker.sock")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(4566).ForPath("/_localstack/health")))
            .Build();

        await _container.StartAsync();

        ServiceUrl = $"http://localhost:{_container.GetMappedPublicPort(4566)}";

        AwsSdkClient = new AmazonLambdaClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonLambdaConfig
            {
                ServiceURL = ServiceUrl,
                AuthenticationRegion = "us-east-1"
            });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddStaticCredentials("test", "test");
        services.AddLambda(config =>
        {
            config.ServiceUrl = ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        GoaClient = _serviceProvider.GetRequiredService<ILambdaClient>();

        await DeployEchoFunctionAsync();
    }

    private async Task DeployEchoFunctionAsync()
    {
        var pythonCode = "def handler(event, context):\n    return event";

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("lambda_function.py");
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream);
            await writer.WriteAsync(pythonCode);
        }

        zipStream.Position = 0;

        await AwsSdkClient.CreateFunctionAsync(new CreateFunctionRequest
        {
            FunctionName = FunctionName,
            Runtime = Runtime.Python312,
            Role = "arn:aws:iam::000000000000:role/lambda-role",
            Handler = "lambda_function.handler",
            Code = new FunctionCode { ZipFile = zipStream }
        });

        // Wait for function to be active
        for (var i = 0; i < 10; i++)
        {
            var config = await AwsSdkClient.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest
            {
                FunctionName = FunctionName
            });

            if (config.State == State.Active) return;

            await Task.Delay(2000);
        }

        throw new TimeoutException($"Lambda function '{FunctionName}' did not reach Active state within 20 seconds.");
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
