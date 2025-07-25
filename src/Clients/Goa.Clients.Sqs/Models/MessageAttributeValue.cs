using System.Text.Json.Serialization;

namespace Goa.Clients.Sqs.Models;

/// <summary>
/// Represents a message attribute value in SQS.
/// </summary>
public sealed class MessageAttributeValue
{
    /// <summary>
    /// The type of the attribute value.
    /// </summary>
    [JsonPropertyName("DataType")]
    public required string DataType { get; set; }

    /// <summary>
    /// The attribute value for String data types.
    /// </summary>
    [JsonPropertyName("StringValue")]
    public string? StringValue { get; set; }

    /// <summary>
    /// The attribute value for Binary data types (base64 encoded).
    /// </summary>
    [JsonPropertyName("BinaryValue")]
    public string? BinaryValue { get; set; }

    /// <summary>
    /// The list values for String list data types.
    /// </summary>
    [JsonPropertyName("StringListValues")]
    public List<string>? StringListValues { get; set; }

    /// <summary>
    /// The list values for Binary list data types (base64 encoded).
    /// </summary>
    [JsonPropertyName("BinaryListValues")]
    public List<string>? BinaryListValues { get; set; }
}