using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Models;

/// <summary>
/// Represents an SQS message.
/// </summary>
public sealed class SqsMessage
{
    /// <summary>
    /// A unique identifier for the message.
    /// </summary>
    [JsonPropertyName("MessageId")]
    public string? MessageId { get; set; }

    /// <summary>
    /// An identifier associated with retrieving the message.
    /// </summary>
    [JsonPropertyName("ReceiptHandle")]
    public string? ReceiptHandle { get; set; }

    /// <summary>
    /// The message's contents.
    /// </summary>
    [JsonPropertyName("Body")]
    public string? Body { get; set; }

    /// <summary>
    /// Message attributes.
    /// </summary>
    [JsonPropertyName("MessageAttributes")]
    public Dictionary<string, MessageAttributeValue>? MessageAttributes { get; set; }

    /// <summary>
    /// System attributes for the message.
    /// </summary>
    [JsonPropertyName("Attributes")]
    public Dictionary<string, string>? Attributes { get; set; }

    /// <summary>
    /// An MD5 digest of the non-URL-encoded message body string.
    /// </summary>
    [JsonPropertyName("MD5OfBody")]
    public string? MD5OfBody { get; set; }

    /// <summary>
    /// An MD5 digest of the non-URL-encoded message attributes.
    /// </summary>
    [JsonPropertyName("MD5OfMessageAttributes")]
    public string? MD5OfMessageAttributes { get; set; }
}