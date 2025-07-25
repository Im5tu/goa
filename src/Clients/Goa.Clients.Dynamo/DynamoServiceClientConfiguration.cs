using Goa.Clients.Core.Configuration;

namespace Goa.Clients.Dynamo;

/// <summary>
/// Configuration class for DynamoDB service providing DynamoDB-specific settings.
/// </summary>
public class DynamoServiceClientConfiguration : AwsServiceConfiguration
{
    /// <summary>
    /// Initializes a new instance of the DynamoServiceClientConfiguration class.
    /// </summary>
    public DynamoServiceClientConfiguration() : base("dynamodb")
    {
        // DynamoDB API version is 20120810
        ApiVersion = "20120810";
    }
}
