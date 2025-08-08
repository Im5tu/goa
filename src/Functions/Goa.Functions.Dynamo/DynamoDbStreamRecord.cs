using System.Text.Json.Serialization;

namespace Goa.Functions.Dynamo;

/// <summary>
/// Represents a single DynamoDB stream record
/// </summary>
public class DynamoDbStreamRecord
{
    /// <summary>
    /// Gets or sets the Amazon Resource Name (ARN) of the event source
    /// </summary>
    [JsonPropertyName("eventSourceARN")]
    public string? EventSourceArn { get; set; }

    /// <summary>
    /// Gets or sets the AWS region where the event originated
    /// </summary>
    [JsonPropertyName("awsRegion")]
    public string? AwsRegion { get; set; }

    /// <summary>
    /// Gets or sets the DynamoDB-specific data for the stream record
    /// </summary>
    [JsonPropertyName("dynamodb")]
    public StreamRecord? Dynamodb { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for the event
    /// </summary>
    [JsonPropertyName("eventID")]
    public string? EventID { get; set; }

    /// <summary>
    /// Gets or sets the type of DynamoDB operation that triggered the event
    /// </summary>
    [JsonPropertyName("eventName")]
    public DynamoStreamOperation EventName { get; set; }

    /// <summary>
    /// Gets or sets the event source identifier
    /// </summary>
    [JsonPropertyName("eventSource")]
    public string? EventSource { get; set; }

    /// <summary>
    /// Gets or sets the version of the event format
    /// </summary>
    [JsonPropertyName("eventVersion")]
    public string? EventVersion { get; set; }

    /// <summary>
    /// Gets or sets the user identity information
    /// </summary>
    [JsonPropertyName("userIdentity")]
    public Identity? UserIdentity { get; set; }

    internal ProcessingType ProcessingType { get; private set; }

    /// <summary>
    /// Marks this particular record as failed in the stream processing
    /// </summary>
    public void MarkAsFailed() => ProcessingType = ProcessingType.Failure;
}
