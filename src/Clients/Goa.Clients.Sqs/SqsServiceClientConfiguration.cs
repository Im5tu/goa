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
    /// <see cref="AwsServiceConfiguration.HttpTimeout"/> is applied to each request. <c>ReceiveMessage</c> requests
    /// get their <c>WaitTimeSeconds</c> on top of it, so long polls are never cut short by the client.
    /// </remarks>
    public SqsServiceClientConfiguration() : base("sqs")
    {
        ApiVersion = "2012-11-05";
    }
}