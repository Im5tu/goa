using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents S3-specific information in an S3 event
/// </summary>
public class S3Info
{
    /// <summary>
    /// Gets or sets the S3 schema version
    /// </summary>
    [JsonPropertyName("s3SchemaVersion")]
    public string? S3SchemaVersion { get; set; }

    /// <summary>
    /// Gets or sets the configuration ID from the bucket notification configuration
    /// </summary>
    [JsonPropertyName("configurationId")]
    public string? ConfigurationId { get; set; }

    /// <summary>
    /// Gets or sets the bucket information
    /// </summary>
    [JsonPropertyName("bucket")]
    public S3Bucket? Bucket { get; set; }

    /// <summary>
    /// Gets or sets the object information
    /// </summary>
    [JsonPropertyName("object")]
    public S3Object? Object { get; set; }
}