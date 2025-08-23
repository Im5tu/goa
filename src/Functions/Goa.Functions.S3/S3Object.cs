using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents S3 object information in an S3 event
/// </summary>
public class S3Object
{
    /// <summary>
    /// Gets or sets the object key (URL encoded)
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets the object size in bytes
    /// </summary>
    [JsonPropertyName("size")]
    public long? Size { get; set; }

    /// <summary>
    /// Gets or sets the object ETag
    /// </summary>
    [JsonPropertyName("eTag")]
    public string? ETag { get; set; }

    /// <summary>
    /// Gets or sets the object version ID (if bucket is versioning-enabled)
    /// </summary>
    [JsonPropertyName("versionId")]
    public string? VersionId { get; set; }

    /// <summary>
    /// Gets or sets the sequencer value used to determine event sequence
    /// </summary>
    [JsonPropertyName("sequencer")]
    public string? Sequencer { get; set; }
}