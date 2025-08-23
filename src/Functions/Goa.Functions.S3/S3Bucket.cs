using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents S3 bucket information in an S3 event
/// </summary>
public class S3Bucket
{
    /// <summary>
    /// Gets or sets the bucket name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the bucket owner identity
    /// </summary>
    [JsonPropertyName("ownerIdentity")]
    public S3Identity? OwnerIdentity { get; set; }

    /// <summary>
    /// Gets or sets the bucket ARN
    /// </summary>
    [JsonPropertyName("arn")]
    public string? Arn { get; set; }
}