using System.Text.Json.Serialization;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Represents a DynamoDB stream event containing one or more stream records
/// </summary>
public class DynamoDbEvent
{
    /// <summary>
    /// Gets or sets the list of DynamoDB stream records
    /// </summary>
    [JsonPropertyName("Records")]
    public IList<DynamoDbStreamRecord>? Records { get; set; }
}
