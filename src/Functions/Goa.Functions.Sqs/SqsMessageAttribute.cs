using System.Text.Json.Serialization;

namespace Goa.Functions.Sqs;

/// <summary>
/// Represents an SQS message attribute with typed values
/// </summary>
public class SqsMessageAttribute
{
    /// <summary>
    /// Gets or sets the string value of the attribute
    /// </summary>
    [JsonPropertyName("stringValue")]
    public string? StringValue { get; set; }

    /// <summary>
    /// Gets or sets the binary value of the attribute (base64 encoded)
    /// </summary>
    [JsonPropertyName("binaryValue")]
    public string? BinaryValue { get; set; }

    /// <summary>
    /// Gets or sets the list of string values for the attribute
    /// </summary>
    [JsonPropertyName("stringListValues")]
    public IList<string>? StringListValues { get; set; }

    /// <summary>
    /// Gets or sets the list of binary values for the attribute (base64 encoded)
    /// </summary>
    [JsonPropertyName("binaryListValues")]
    public IList<string>? BinaryListValues { get; set; }

    /// <summary>
    /// Gets or sets the data type of the attribute (String, Number, Binary)
    /// </summary>
    [JsonPropertyName("dataType")]
    public string? DataType { get; set; }
}