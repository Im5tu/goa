using System.Text.Json.Serialization;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// Represents a decoded CloudWatch Logs subscription filter event
/// </summary>
public class CloudWatchLogsEvent
{
    /// <summary>
    /// Gets or sets the AWS account ID that owns the log data
    /// </summary>
    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    /// <summary>
    /// Gets or sets the log group name
    /// </summary>
    [JsonPropertyName("logGroup")]
    public string? LogGroup { get; set; }

    /// <summary>
    /// Gets or sets the log stream name
    /// </summary>
    [JsonPropertyName("logStream")]
    public string? LogStream { get; set; }

    /// <summary>
    /// Gets or sets the list of subscription filter names that matched
    /// </summary>
    [JsonPropertyName("subscriptionFilters")]
    public IList<string>? SubscriptionFilters { get; set; }

    /// <summary>
    /// Gets or sets the message type (DATA_MESSAGE or CONTROL_MESSAGE)
    /// </summary>
    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    /// <summary>
    /// Gets or sets the policy level (ACCOUNT_LEVEL or null)
    /// </summary>
    [JsonPropertyName("policyLevel")]
    public string? PolicyLevel { get; set; }

    /// <summary>
    /// Gets or sets the list of log events
    /// </summary>
    [JsonPropertyName("logEvents")]
    public IList<CloudWatchLogEvent>? LogEvents { get; set; }

    /// <summary>
    /// Returns true if this is a control message (connectivity check)
    /// </summary>
    [JsonIgnore]
    public bool IsControlMessage => MessageType == "CONTROL_MESSAGE";

    /// <summary>
    /// Returns true if this is a data message containing actual log events
    /// </summary>
    [JsonIgnore]
    public bool IsDataMessage => MessageType == "DATA_MESSAGE";
}
