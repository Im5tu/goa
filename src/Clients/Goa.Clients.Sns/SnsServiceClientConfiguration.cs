using Goa.Clients.Core.Configuration;

namespace Goa.Clients.Sns;

/// <summary>
/// Configuration class for SNS service providing SNS-specific settings.
/// </summary>
public sealed class SnsServiceClientConfiguration : AwsServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the SnsServiceClientConfiguration class.
    /// </summary>
    public SnsServiceClientConfiguration() : base("sns")
    {
        ApiVersion = "2010-03-31";
    }
}