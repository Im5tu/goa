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
    public SqsServiceClientConfiguration() : base("sqs")
    {
        ApiVersion = "2012-11-05";
    }
}