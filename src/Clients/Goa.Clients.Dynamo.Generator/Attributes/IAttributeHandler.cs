using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Interface for handling specific attribute types during code generation.
/// </summary>
public interface IAttributeHandler
{
    /// <summary>
    /// The full name of the attribute type this handler processes.
    /// </summary>
    string AttributeTypeName { get; }
    
    /// <summary>
    /// Determines if this handler can process the given attribute.
    /// </summary>
    bool CanHandle(AttributeData attributeData);
    
    /// <summary>
    /// Parses the attribute data into strongly-typed information.
    /// </summary>
    AttributeInfo? ParseAttribute(AttributeData attributeData);
    
    /// <summary>
    /// Validates the attribute usage on the given symbol and reports any diagnostics.
    /// </summary>
    void ValidateAttribute(AttributeInfo attributeInfo, ISymbol symbol, Action<Diagnostic> reportDiagnostic);
}