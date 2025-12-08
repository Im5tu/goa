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