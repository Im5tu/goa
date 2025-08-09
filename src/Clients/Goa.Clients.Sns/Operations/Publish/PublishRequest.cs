using Goa.Clients.Core;
using System.Text;
using System.Xml;
using Goa.Clients.Sns.Models;

namespace Goa.Clients.Sns.Operations.Publish;

/// <summary>
/// Request for the Publish operation.
/// </summary>
public sealed class PublishRequest : ISerializeToXml
{
    /// <summary>
    /// The topic you want to publish to, or the endpoint you want to publish a message to.
    /// </summary>
    public string? TopicArn { get; set; }

    /// <summary>
    /// The endpoint ARN to which you want to deliver the message.
    /// </summary>
    public string? TargetArn { get; set; }

    /// <summary>
    /// The phone number to which you want to deliver an SMS message.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// The message you want to send.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Optional parameter to be used as the "Subject" line when the message is delivered to email endpoints.
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Set to JSON to use JSON message formatting for the message body.
    /// </summary>
    public string? MessageStructure { get; set; }

    /// <summary>
    /// Message attributes for the publish action.
    /// </summary>
    public Dictionary<string, SnsMessageAttributeValue>? MessageAttributes { get; set; }

    /// <summary>
    /// This parameter applies only to FIFO topics. The token used for deduplication of messages.
    /// </summary>
    public string? MessageDeduplicationId { get; set; }

    /// <summary>
    /// This parameter applies only to FIFO topics. The tag that specifies that a message belongs to a specific message group.
    /// </summary>
    public string? MessageGroupId { get; set; }

    /// <summary>
    /// Serializes the current PublishRequest to an XML string representation.
    /// </summary>
    /// <returns>
    /// A string containing the XML representation of the PublishRequest.
    /// The XML follows AWS SNS Publish operation format.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the Message property is null or empty, as it's required for the Publish operation.
    /// </exception>
    public string SerializeToXml()
    {
        if (string.IsNullOrWhiteSpace(Message))
            throw new InvalidOperationException("Message is required for Publish operation.");

        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = true,
            Indent = false,
            Encoding = Encoding.UTF8
        };

        using var writer = new StringWriter();
        using var xmlWriter = XmlWriter.Create(writer, settings);

        xmlWriter.WriteStartElement("PublishRequest");

        // Write TopicArn if provided
        if (!string.IsNullOrWhiteSpace(TopicArn))
        {
            xmlWriter.WriteElementString("TopicArn", TopicArn);
        }

        // Write TargetArn if provided
        if (!string.IsNullOrWhiteSpace(TargetArn))
        {
            xmlWriter.WriteElementString("TargetArn", TargetArn);
        }

        // Write PhoneNumber if provided
        if (!string.IsNullOrWhiteSpace(PhoneNumber))
        {
            xmlWriter.WriteElementString("PhoneNumber", PhoneNumber);
        }

        // Write Message (required)
        xmlWriter.WriteElementString("Message", Message);

        // Write Subject if provided
        if (!string.IsNullOrWhiteSpace(Subject))
        {
            xmlWriter.WriteElementString("Subject", Subject);
        }

        // Write MessageStructure if provided
        if (!string.IsNullOrWhiteSpace(MessageStructure))
        {
            xmlWriter.WriteElementString("MessageStructure", MessageStructure);
        }

        // Write MessageAttributes if provided
        if (MessageAttributes != null && MessageAttributes.Count > 0)
        {
            xmlWriter.WriteStartElement("MessageAttributes");

            foreach (var kvp in MessageAttributes)
            {
                xmlWriter.WriteStartElement("entry");
                xmlWriter.WriteElementString("key", kvp.Key);

                xmlWriter.WriteStartElement("value");
                kvp.Value.WriteToXml(xmlWriter);
                xmlWriter.WriteEndElement(); // value

                xmlWriter.WriteEndElement(); // entry
            }

            xmlWriter.WriteEndElement(); // MessageAttributes
        }

        // Write MessageDeduplicationId if provided
        if (!string.IsNullOrWhiteSpace(MessageDeduplicationId))
        {
            xmlWriter.WriteElementString("MessageDeduplicationId", MessageDeduplicationId);
        }

        // Write MessageGroupId if provided
        if (!string.IsNullOrWhiteSpace(MessageGroupId))
        {
            xmlWriter.WriteElementString("MessageGroupId", MessageGroupId);
        }

        xmlWriter.WriteEndElement(); // PublishRequest
        xmlWriter.Flush();

        return writer.ToString();
    }
}
