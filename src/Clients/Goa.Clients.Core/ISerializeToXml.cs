namespace Goa.Clients.Core;

/// <summary>
/// Provides functionality to serialize an object to XML format.
/// </summary>
public interface ISerializeToXml
{
    /// <summary>
    /// Serializes the current object to an XML string representation.
    /// </summary>
    /// <returns>
    /// A string containing the XML representation of the object.
    /// The XML does not include an XML declaration and uses UTF-8 encoding.
    /// </returns>
    /// <remarks>
    /// The generated XML follows AWS service-specific formatting conventions.
    /// Null or empty properties are typically omitted from the output.
    /// Collections and complex objects are serialized according to the target service's schema requirements.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the object is in an invalid state for serialization,
    /// such as when required properties are null or when circular references are detected.
    /// </exception>
    string SerializeToXml();
}