using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents glacier event data for S3 restore events
/// </summary>
public class GlacierEventData
{
    /// <summary>
    /// Gets or sets the restore event data
    /// </summary>
    [JsonPropertyName("restoreEventData")]
    public RestoreEventData? RestoreEventData { get; set; }
}