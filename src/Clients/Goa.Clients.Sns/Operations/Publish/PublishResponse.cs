using Goa.Clients.Core;
using System.Xml;

namespace Goa.Clients.Sns.Operations.Publish;

/// <summary>
/// Response from the Publish operation.
/// </summary>
public sealed class PublishResponse : IDeserializeFromXml
{
    /// <summary>
    /// Unique identifier assigned to the published message.
    /// </summary>
    public string? MessageId { get; set; }

    /// <summary>
    /// This parameter applies only to FIFO topics. The sequence number assigned to the message.
    /// </summary>
    public string? SequenceNumber { get; set; }

    /// <summary>
    /// Initializes a new instance of the PublishResponse class.
    /// </summary>
    public PublishResponse()
    {
    }

    /// <summary>
    /// Deserializes XML response content into the current PublishResponse instance.
    /// </summary>
    /// <param name="xml">The XML response content to deserialize.</param>
    /// <remarks>
    /// Handles the standard AWS SNS Publish response format:
    /// <code>
    /// &lt;PublishResponse&gt;
    ///     &lt;PublishResult&gt;
    ///         &lt;MessageId&gt;12345678-1234-1234-1234-123456789012&lt;/MessageId&gt;
    ///         &lt;SequenceNumber&gt;1234567890&lt;/SequenceNumber&gt;
    ///     &lt;/PublishResult&gt;
    ///     &lt;ResponseMetadata&gt;
    ///         &lt;RequestId&gt;12345678-1234-1234-1234-123456789012&lt;/RequestId&gt;
    ///     &lt;/ResponseMetadata&gt;
    /// &lt;/PublishResponse&gt;
    /// </code>
    /// This method populates the current instance's properties with the parsed values from the XML.
    /// Missing elements will result in null values for the corresponding properties.
    /// If parsing fails, the properties will retain their current values and no exception is thrown.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xml"/> is null.</exception>
    public void DeserializeFromXml(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return;

        try
        {
            using var reader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(reader);

            string? messageId = null;
            string? sequenceNumber = null;

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "MessageId":
                            messageId = xmlReader.ReadElementContentAsString();
                            break;
                        case "SequenceNumber":
                            sequenceNumber = xmlReader.ReadElementContentAsString();
                            break;
                    }
                }
            }

            MessageId = messageId;
            SequenceNumber = sequenceNumber;
        }
        catch
        {
            // Ignore parsing errors for graceful degradation
            // Properties will retain their current values
        }
    }
}
