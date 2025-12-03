using System.Text.Json.Serialization;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Represents the raw CloudWatch Logs event as received by Lambda.
/// The actual log data is base64-encoded and gzip-compressed.
/// </summary>
internal class CloudWatchLogsRawEvent
{
    /// <summary>
    /// Gets or sets the compressed log data container
    /// </summary>
    [JsonPropertyName("awslogs")]
    public CloudWatchLogsCompressedData? AwsLogs { get; set; }
}

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
