using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents intelligent tiering event data for S3 intelligent tiering events
/// </summary>
public class IntelligentTieringEventData
{
    /// <summary>
    /// Gets or sets the destination access tier
    /// </summary>
    [JsonPropertyName("destinationAccessTier")]
    public string? DestinationAccessTier { get; set; }
}