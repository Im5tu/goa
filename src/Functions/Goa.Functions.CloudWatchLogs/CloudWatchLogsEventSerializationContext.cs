using System.Text.Json.Serialization;

namespace Goa.Functions.CloudWatchLogs;

/// <summary>
/// JSON serialization context for CloudWatch Logs events to support AOT compilation
/// </summary>
[JsonSerializable(typeof(CloudWatchLogsRawEvent))]
[JsonSerializable(typeof(CloudWatchLogsCompressedData))]
[JsonSerializable(typeof(CloudWatchLogsEvent))]
[JsonSerializable(typeof(CloudWatchLogEvent))]
[JsonSerializable(typeof(IList<CloudWatchLogEvent>))]
[JsonSerializable(typeof(IList<string>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(object))]
internal partial class CloudWatchLogsEventSerializationContext : JsonSerializerContext
{
}
