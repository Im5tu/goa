using System.Text.Json.Serialization;

namespace Goa.Functions.Sqs;

/// <summary>
/// JSON serialization context for SQS events
/// </summary>
[JsonSerializable(typeof(SqsEvent))]
[JsonSerializable(typeof(SqsMessage))]
[JsonSerializable(typeof(SqsMessageAttribute))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, SqsMessageAttribute>))]
[JsonSerializable(typeof(IList<SqsMessage>))]
[JsonSerializable(typeof(IList<string>))]
internal partial class SqsEventSerializationContext : JsonSerializerContext
{
}