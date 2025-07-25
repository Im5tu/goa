using System.Text.Json.Serialization;
using Goa.Clients.Sqs.Models;
using Goa.Clients.Sqs.Operations.DeleteMessage;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Operations.SendMessage;

namespace Goa.Clients.Sqs.Serialization;

/// <summary>
/// JSON serialization context for SQS operations, optimized for AOT compilation.
/// </summary>
[JsonSerializable(typeof(SendMessageRequest))]
[JsonSerializable(typeof(SendMessageResponse))]
[JsonSerializable(typeof(ReceiveMessageRequest))]
[JsonSerializable(typeof(ReceiveMessageResponse))]
[JsonSerializable(typeof(DeleteMessageRequest))]
[JsonSerializable(typeof(DeleteMessageResponse))]
[JsonSerializable(typeof(SqsMessage))]
[JsonSerializable(typeof(MessageAttributeValue))]
[JsonSerializable(typeof(List<SqsMessage>))]
[JsonSerializable(typeof(Dictionary<string, MessageAttributeValue>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
internal partial class SqsJsonContext : JsonSerializerContext
{
}