using System.Text.Json.Serialization;

namespace Goa.Clients.Sns.Models;

/// <summary>
/// Represents a message attribute value in SNS.
/// </summary>
public sealed class SnsMessageAttributeValue
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
}