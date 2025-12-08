using System.Text.Json.Serialization;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Contains the base64-encoded, gzip-compressed log data
/// </summary>
internal class CloudWatchLogsCompressedData
{
    /// <summary>
    /// Gets or sets the base64-encoded, gzip-compressed JSON data
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }
}