using System.Text.Json.Serialization;

namespace Goa.Functions.Kinesis;

/// <summary>
/// JSON serialization context for Kinesis events to support AOT compilation
/// </summary>
[JsonSerializable(typeof(KinesisEvent))]
[JsonSerializable(typeof(KinesisRecord))]
[JsonSerializable(typeof(KinesisData))]
[JsonSerializable(typeof(object))]
internal partial class KinesisEventSerializationContext : JsonSerializerContext
{
}