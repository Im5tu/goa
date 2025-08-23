using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents response elements in an S3 event
/// </summary>
public class S3ResponseElements
{
    /// <summary>
    /// Gets or sets the Amazon S3 generated request ID
    /// </summary>
    [JsonPropertyName("x-amz-request-id")]
    public string? XAmzRequestId { get; set; }

    /// <summary>
    /// Gets or sets the Amazon S3 host that processed the request
    /// </summary>
    [JsonPropertyName("x-amz-id-2")]
    public string? XAmzId2 { get; set; }
}