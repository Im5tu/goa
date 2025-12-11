using Goa.Clients.Core.Http;
using System.Text.Json.Serialization;
using Goa.Clients.Sqs.Models;
using Goa.Clients.Sqs.Operations.DeleteMessage;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Operations.SendMessage;
using Goa.Clients.Sqs.Operations.SendMessageBatch;

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
[JsonSerializable(typeof(SendMessageBatchRequest))]
[JsonSerializable(typeof(SendMessageBatchResponse))]
[JsonSerializable(typeof(SendMessageBatchRequestEntry))]
[JsonSerializable(typeof(SendMessageBatchResultEntry))]
[JsonSerializable(typeof(BatchResultErrorEntry))]
[JsonSerializable(typeof(List<SendMessageBatchRequestEntry>))]
[JsonSerializable(typeof(List<SendMessageBatchResultEntry>))]
[JsonSerializable(typeof(List<BatchResultErrorEntry>))]
[JsonSerializable(typeof(SqsMessage))]
[JsonSerializable(typeof(MessageAttributeValue))]
[JsonSerializable(typeof(List<SqsMessage>))]
[JsonSerializable(typeof(Dictionary<string, MessageAttributeValue>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(ApiError))]
internal partial class SqsJsonContext : JsonSerializerContext
{
}
