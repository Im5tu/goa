using System.Xml;

namespace Goa.Clients.Core;

/// <summary>
/// Provides functionality to deserialize an object from XML format.
/// </summary>
public interface IDeserializeFromXml
{
    /// <summary>
    /// Deserializes XML content into the current object instance, populating its properties.
    /// </summary>
    /// <param name="xml">
    /// The XML string to deserialize. Must be a valid XML document or fragment
    /// that conforms to the expected schema for the target type.
    /// </param>
    /// <remarks>
    /// The deserialization process follows AWS service-specific XML conventions.
    /// Missing elements in the XML will result in default values for the corresponding properties.
    /// The method is designed to handle AWS service response formats and query-style XML structures.
    /// This method modifies the current instance by populating its properties with values from the XML.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="xml"/> is null.
    /// </exception>
    /// <exception cref="xml">
    /// Thrown when <paramref name="xml"/> is not valid XML or does not conform to the expected schema.
    /// </exception>
    /// <exception cref="XmlException">
    /// Thrown when the XML structure cannot be mapped to the target type,
    /// such as when required elements are missing or have incompatible data types.
    /// </exception>
    void DeserializeFromXml(string xml);
}