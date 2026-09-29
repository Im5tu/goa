using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sqs.Tests.Infrastructure;

/// <summary>
/// Builds a real <see cref="ISqsClient"/> whose HTTP traffic is routed to a <see cref="FakeSqsHttpHandler"/>.
/// </summary>
public static class FakeSqsClientFactory
{
    // Name of the HttpClient registered by AddSqs.
    public const string HttpClientName = nameof(SqsServiceClient);

    public static ServiceProvider Create(
        FakeSqsHttpHandler handler,
        out ISqsClient client,
        Action<SqsServiceClientConfiguration>? configure = null,
        ILoggerProvider? loggerProvider = null)
    {
        var services = new ServiceCollection();

        services.AddLogging(logging =>
        {
            if (loggerProvider is not null)
                logging.AddProvider(loggerProvider);
        });
        services.AddStaticCredentials("test", "test");
        services.AddSqs(config =>
        {
            config.ServiceUrl = "http://localhost:4566";
            config.Region = "us-east-1";
            configure?.Invoke(config);
        });
        services.AddHttpClient(HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        client = provider.GetRequiredService<ISqsClient>();
        return provider;
    }
}
