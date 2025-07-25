using System.Text.Json.Serialization;
using Goa.Clients.Sqs.Models;

namespace Goa.Clients.Sqs.Operations.ReceiveMessage;

/// <summary>
/// Response from the ReceiveMessage operation.
/// </summary>
public sealed class ReceiveMessageResponse
{
    /// <summary>
    /// A list of messages.
    /// </summary>
    [JsonPropertyName("Messages")]
    public List<SqsMessage> Messages { get; set; } = [];
}