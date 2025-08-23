using System.Text.Json.Serialization;

namespace Goa.Functions.EventBridge;

/// <summary>
/// Represents an EventBridge event for Lambda function invocation
/// Note: EventBridge typically sends single events to Lambda functions
/// </summary>
public class EventbridgeEvent
{
    /// <summary>
    /// Gets or sets the event format version
    /// </summary>
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier for this event
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the type of event
    /// </summary>
    [JsonPropertyName("detail-type")]
    public string? DetailType { get; set; }

    /// <summary>
    /// Gets or sets the source that generated this event
    /// </summary>
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets the AWS account ID where the event originated
    /// </summary>
    [JsonPropertyName("account")]
    public string? Account { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the event occurred
    /// </summary>
    [JsonPropertyName("time")]
    public string? Time { get; set; }

    /// <summary>
    /// Gets or sets the AWS region where the event originated
    /// </summary>
    [JsonPropertyName("region")]
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the list of resources involved in this event
    /// </summary>
    [JsonPropertyName("resources")]
    public IList<string>? Resources { get; set; }

    /// <summary>
    /// Gets or sets the event-specific data
    /// </summary>
    [JsonPropertyName("detail")]
    public object? Detail { get; set; }

    internal ProcessingType ProcessingType { get; private set; }

    /// <summary>
    /// Marks this particular event as failed in processing
    /// </summary>
    public void MarkAsFailed() => ProcessingType = ProcessingType.Failure;
}