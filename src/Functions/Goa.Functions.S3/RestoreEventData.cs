using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents restore event data
/// </summary>
public class RestoreEventData
{
    /// <summary>
    /// Gets or sets the lifecycle restoration expiry time
    /// </summary>
    [JsonPropertyName("lifecycleRestorationExpiryTime")]
    public string? LifecycleRestorationExpiryTime { get; set; }

    /// <summary>
    /// Gets or sets the lifecycle restore storage class
    /// </summary>
    [JsonPropertyName("lifecycleRestoreStorageClass")]
    public string? LifecycleRestoreStorageClass { get; set; }
}