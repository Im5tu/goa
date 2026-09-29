using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Goa.Clients.Sqs.Tests.Infrastructure;

/// <summary>
/// Builds a real <see cref="ISqsClient"/> whose HTTP traffic is routed to a <see cref="FakeSqsHttpHandler"/>.
/// </summary>
public static class FakeSqsClientFactory
{
    // Name of the HttpClient registered by AddSqs (nameof(SqsServiceClient), which is internal).
    public const string HttpClientName = "SqsServiceClient";

    public static ServiceProvider Create(FakeSqsHttpHandler handler, out ISqsClient client)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddStaticCredentials("test", "test");
        services.AddSqs(config =>
        {
            config.ServiceUrl = "http://localhost:4566";
            config.Region = "us-east-1";
        });
        services.AddHttpClient(HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);

        var provider = services.BuildServiceProvider();
        client = provider.GetRequiredService<ISqsClient>();
        return provider;
    }
}
