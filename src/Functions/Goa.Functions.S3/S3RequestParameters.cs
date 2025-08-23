using System.Text.Json.Serialization;

namespace Goa.Functions.S3;

/// <summary>
/// Represents request parameters in an S3 event
/// </summary>
public class S3RequestParameters
{
    /// <summary>
    /// Gets or sets the source IP address of the request
    /// </summary>
    [JsonPropertyName("sourceIPAddress")]
    public string? SourceIPAddress { get; set; }
}