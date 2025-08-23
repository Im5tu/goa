using System.Text.Json.Serialization;

namespace Goa.Functions.Sqs;

/// <summary>
/// Represents an SQS event containing one or more messages
/// </summary>
public class SqsEvent
{
    /// <summary>
    /// Gets or sets the list of SQS messages
    /// </summary>
    [JsonPropertyName("Records")]
    public IList<SqsMessage>? Records { get; set; }
}