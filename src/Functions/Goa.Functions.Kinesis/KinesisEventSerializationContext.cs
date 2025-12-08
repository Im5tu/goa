using System.Text.Json.Serialization;
using Goa.Functions.Core;

namespace Goa.Functions.Kinesis;

/// <summary>
/// JSON serialization context for Kinesis events to support AOT compilation
/// </summary>
[JsonSerializable(typeof(KinesisEvent))]
[JsonSerializable(typeof(KinesisRecord))]
[JsonSerializable(typeof(KinesisData))]
[JsonSerializable(typeof(BatchItemFailureResponse))]
[JsonSerializable(typeof(BatchItemFailure))]
[JsonSerializable(typeof(List<BatchItemFailure>))]
internal partial class KinesisEventSerializationContext : JsonSerializerContext
{
}
