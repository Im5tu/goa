using Goa.Clients.Core.Http;
using System.Xml;

namespace Goa.Clients.Core;

/// <summary>
/// Represents an XML API error response from AWS services, extending the base ApiError with XML deserialization capabilities.
/// </summary>
public class XmlApiError : IDeserializeFromXml
{
    /// <summary>
    /// Gets or sets the AWS request ID associated with this error.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets the error message describing what went wrong.
    /// </summary>
    public string Message { get; private set; }

    /// <summary>
    /// Gets the error type, typically "Sender" or "Receiver", indicating whether the error was caused by the client or server.
    /// </summary>
    public string Type { get; private set; }

    /// <summary>
    /// Gets the AWS-specific error code that identifies the type of error that occurred.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Initializes a new instance of the XmlApiError class with default values.
    /// </summary>
    public XmlApiError()
    {
        Message = string.Empty;
        Type = string.Empty;
        Code = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the XmlApiError class with specified values.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="type">The error type (Sender/Receiver).</param>
    /// <param name="code">The AWS error code.</param>
    /// <param name="requestId">The AWS request ID.</param>
    public XmlApiError(string message, string type = "", string code = "", string? requestId = null)
    {
        Message = message ?? string.Empty;
        Type = type ?? string.Empty;
        Code = code ?? string.Empty;
        RequestId = requestId;
    }

    /// <summary>
    /// Deserializes XML error response content into the current instance.
    /// </summary>
    /// <param name="xml">The XML error response content to deserialize.</param>
    /// <remarks>
    /// Handles the standard AWS XML error format:
    /// <code>
    /// &lt;ErrorResponse&gt;
    ///     &lt;Error&gt;
    ///         &lt;Type&gt;Sender&lt;/Type&gt;
    ///         &lt;Code&gt;InvalidParameterValue&lt;/Code&gt;
    ///         &lt;Message&gt;Invalid parameter value&lt;/Message&gt;
    ///     &lt;/Error&gt;
    ///     &lt;RequestId&gt;b25f48e8-84fd-11e6-a5c6-d31b64724a3d&lt;/RequestId&gt;
    /// &lt;/ErrorResponse&gt;
    /// </code>
    /// This method populates the current instance's properties with the parsed values from the XML.
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

            var message = "";
            var type = "";
            var code = "";
            var requestId = "";

            while (xmlReader.Read())
            {
                if (xmlReader.NodeType == XmlNodeType.Element)
                {
                    switch (xmlReader.Name)
                    {
                        case "Message":
                            message = xmlReader.ReadElementContentAsString();
                            break;
                        case "Type":
                            type = xmlReader.ReadElementContentAsString();
                            break;
                        case "Code":
                            code = xmlReader.ReadElementContentAsString();
                            break;
                        case "RequestId":
                            requestId = xmlReader.ReadElementContentAsString();
                            break;
                    }
                }
            }

            Message = message;
            Type = type;
            Code = code;
            RequestId = requestId;
        }
        catch
        {
            // Ignore parsing errors for graceful degradation
        }
    }

    /// <summary>
    /// Converts this XmlApiError to the base ApiError record type for compatibility with existing error handling code.
    /// </summary>
    /// <returns>
    /// An ApiError instance with the same Message, Type, and Code values as this XmlApiError.
    /// The RequestId property is not included in the conversion as it's not part of the base ApiError structure.
    /// </returns>
    public ApiError ToApiError()
    {
        return new ApiError(Message, Type, Code);
    }
}