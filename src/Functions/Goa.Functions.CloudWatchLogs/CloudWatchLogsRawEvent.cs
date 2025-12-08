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