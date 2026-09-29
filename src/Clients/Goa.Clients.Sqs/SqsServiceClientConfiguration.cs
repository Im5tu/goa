using Goa.Clients.Core.Configuration;

namespace Goa.Clients.Sqs;

/// <summary>
/// Configuration class for SQS service providing SQS-specific settings.
/// </summary>
public sealed class SqsServiceClientConfiguration : AwsServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the SqsServiceClientConfiguration class.
    /// </summary>
    /// <remarks>
    /// <see cref="AwsServiceConfiguration.HttpTimeout"/> defaults to 30 seconds so that long-polling
    /// receives (<c>WaitTimeSeconds</c> up to 20) complete before the HTTP client gives up.
    /// </remarks>
    public SqsServiceClientConfiguration() : base("sqs")
    {
        ApiVersion = "2012-11-05";
        HttpTimeout = TimeSpan.FromSeconds(30);
    }
}