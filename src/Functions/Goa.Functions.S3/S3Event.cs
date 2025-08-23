using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents an S3 event containing one or more S3 event records
/// </summary>
public class S3Event
{
    /// <summary>
    /// Gets or sets the list of S3 event records
    /// </summary>
    [JsonPropertyName("Records")]
    public IList<S3EventRecord>? Records { get; set; }
}