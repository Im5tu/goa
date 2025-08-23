using System.Text.Json.Serialization;

namespace Goa.Functions.Kinesis;

/// <summary>
/// Represents a Kinesis event containing one or more stream records
/// </summary>
public class KinesisEvent
{
    /// <summary>
    /// Gets or sets the list of Kinesis stream records
    /// </summary>
    [JsonPropertyName("Records")]
    public IList<KinesisRecord>? Records { get; set; }
}