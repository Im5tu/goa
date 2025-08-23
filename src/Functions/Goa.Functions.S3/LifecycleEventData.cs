using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents lifecycle event data for S3 lifecycle events
/// </summary>
public class LifecycleEventData
{
    /// <summary>
    /// Gets or sets the transition information
    /// </summary>
    [JsonPropertyName("transitionEventData")]
    public TransitionEventData? TransitionEventData { get; set; }
}

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