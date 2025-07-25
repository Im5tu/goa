using Goa.Clients.Core.Configuration;

namespace Goa.Clients.Lambda;

/// <summary>
/// Configuration class for Lambda service providing Lambda-specific settings.
/// </summary>
public sealed class LambdaServiceClientConfiguration : AwsServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the LambdaServiceClientConfiguration class.
    /// </summary>
    public LambdaServiceClientConfiguration() : base("lambda")
    {
        ApiVersion = "2015-03-31";
    }
}