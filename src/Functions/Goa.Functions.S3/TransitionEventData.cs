using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents transition event data
/// </summary>
public class TransitionEventData
{
    /// <summary>
    /// Gets or sets the destination storage class
    /// </summary>
    [JsonPropertyName("destinationStorageClass")]
    public string? DestinationStorageClass { get; set; }
}