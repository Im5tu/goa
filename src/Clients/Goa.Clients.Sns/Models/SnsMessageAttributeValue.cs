using System.Xml;

namespace Goa.Clients.Sns.Models;

/// <summary>
/// Represents a message attribute value in SNS.
/// </summary>
public sealed class SnsMessageAttributeValue
{
    /// <summary>
    /// The type of the attribute value (e.g., "String", "Number", "Binary", "String.Array", etc.).
    /// </summary>
    public required string DataType { get; set; }

    /// <summary>
    /// The attribute value for String data types.
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// The attribute value for Binary data types (base64 encoded).
    /// </summary>
    public string? BinaryValue { get; set; }

    /// <summary>
    /// Writes the current SnsMessageAttributeValue to the specified XmlWriter.
    /// </summary>
    /// <param name="xmlWriter">The XmlWriter to write to.</param>
    /// <remarks>
    /// Generates XML in the format expected by AWS SNS:
    /// <code>
    /// &lt;DataType&gt;String&lt;/DataType&gt;
    /// &lt;StringValue&gt;My Value&lt;/StringValue&gt;
    /// </code>
    /// or
    /// <code>
    /// &lt;DataType&gt;Binary&lt;/DataType&gt;
    /// &lt;BinaryValue&gt;base64encodeddata&lt;/BinaryValue&gt;
    /// </code>
    /// Only includes StringValue or BinaryValue based on which one is provided.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xmlWriter"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when DataType is null/empty, or when neither StringValue nor BinaryValue is provided.
    /// </exception>
    public void WriteToXml(XmlWriter xmlWriter)
    {
        ArgumentNullException.ThrowIfNull(xmlWriter);

        if (string.IsNullOrWhiteSpace(DataType))
            throw new InvalidOperationException("DataType is required for SnsMessageAttributeValue.");

        if (string.IsNullOrWhiteSpace(StringValue) && string.IsNullOrWhiteSpace(BinaryValue))
            throw new InvalidOperationException("Either StringValue or BinaryValue must be provided for SnsMessageAttributeValue.");

        // Write DataType (required)
        xmlWriter.WriteElementString("DataType", DataType);

        // Write StringValue if provided
        if (!string.IsNullOrWhiteSpace(StringValue))
        {
            xmlWriter.WriteElementString("StringValue", StringValue);
        }

        // Write BinaryValue if provided
        if (!string.IsNullOrWhiteSpace(BinaryValue))
        {
            xmlWriter.WriteElementString("BinaryValue", BinaryValue);
        }
    }

    /// <summary>
    /// Creates a new SnsMessageAttributeValue for a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <returns>A new SnsMessageAttributeValue configured for string data.</returns>
    public static SnsMessageAttributeValue Create(string value)
    {
        return Create(value, "String");
    }

    /// <summary>
    /// Creates a new SnsMessageAttributeValue for a string value.
    /// </summary>
    /// <param name="value">The string value.</param>
    /// <param name="dataType">The data type (defaults to "String").</param>
    /// <returns>A new SnsMessageAttributeValue configured for string data.</returns>
    public static SnsMessageAttributeValue Create(string value, string dataType)
    {
        return new SnsMessageAttributeValue
        {
            DataType = dataType,
            StringValue = value
        };
    }

    /// <summary>
    /// Creates a new SnsMessageAttributeValue for a number value.
    /// </summary>
    /// <param name="value">The numeric value.</param>
    /// <returns>A new SnsMessageAttributeValue configured for numeric data.</returns>
    public static SnsMessageAttributeValue Create(object value)
    {
        return new SnsMessageAttributeValue
        {
            DataType = "Number",
            StringValue = value.ToString()
        };
    }

    /// <summary>
    /// Creates a new SnsMessageAttributeValue for binary data.
    /// </summary>
    /// <param name="base64Value">The base64-encoded binary data.</param>
    /// <returns>A new SnsMessageAttributeValue configured for binary data.</returns>
    public static SnsMessageAttributeValue CreateBase64(string base64Value)
    {
        return new SnsMessageAttributeValue
        {
            DataType = "Binary",
            BinaryValue = base64Value
        };
    }

    /// <summary>
    /// Creates a new SnsMessageAttributeValue for binary data from a byte array.
    /// </summary>
    /// <param name="data">The binary data as a byte array.</param>
    /// <returns>A new SnsMessageAttributeValue configured for binary data.</returns>
    public static SnsMessageAttributeValue Create(byte[] data)
    {
        return new SnsMessageAttributeValue
        {
            DataType = "Binary",
            BinaryValue = Convert.ToBase64String(data)
        };
    }
}
