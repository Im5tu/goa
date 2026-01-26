using Goa.Clients.Core.Configuration;

namespace Goa.Clients.Bedrock;

/// <summary>
/// Configuration class for Bedrock Runtime service providing Bedrock-specific settings.
/// </summary>
public class BedrockServiceClientConfiguration : AwsServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the BedrockServiceClientConfiguration class.
    /// </summary>
    public BedrockServiceClientConfiguration() : base("bedrock-runtime", signingService: "bedrock")
    {
    }
}
