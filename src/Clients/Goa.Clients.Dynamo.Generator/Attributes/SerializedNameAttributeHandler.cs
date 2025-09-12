using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Handles the SerializedNameAttribute.
/// </summary>
public class SerializedNameAttributeHandler : IAttributeHandler
{
    public string AttributeTypeName => "Goa.Clients.Dynamo.SerializedNameAttribute";
    
    public bool CanHandle(AttributeData attributeData)
    {
        return attributeData.AttributeClass?.ToDisplayString() == AttributeTypeName;
    }
    
    public AttributeInfo? ParseAttribute(AttributeData attributeData)
    {
        if (!CanHandle(attributeData))
        {
            return null;
        }
        
        var name = string.Empty;
        
        // Get name from constructor arguments
        if (attributeData.ConstructorArguments.Length > 0)
        {
            var firstArg = attributeData.ConstructorArguments[0];
            if (firstArg.Value is string nameValue)
            {
                name = nameValue;
            }
        }
        
        // Validate that name is not empty
        if (string.IsNullOrEmpty(name))
        {
            return null; // Invalid attribute, will be caught in validation
        }
        
        return new SerializedNameAttributeInfo
        {
            AttributeData = attributeData,
            AttributeTypeName = AttributeTypeName,
            Name = name
        };
    }
    
    public void ValidateAttribute(AttributeInfo attributeInfo, ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        if (attributeInfo is not SerializedNameAttributeInfo serializedNameInfo)
        {
            return;
        }
        
        // Validate that the attribute is applied to properties only
        if (symbol is not IPropertySymbol property)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO008",
                title: "Invalid SerializedName usage",
                messageFormat: "SerializedName attribute can only be applied to properties. Symbol '{0}' is not a property.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
            reportDiagnostic(diagnostic);
            return;
        }
        
        // Validate that the serialized name is not empty
        if (string.IsNullOrEmpty(serializedNameInfo.Name))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO009",
                title: "Invalid SerializedName value",
                messageFormat: "SerializedName attribute requires a non-empty name. Property '{0}' has an empty serialized name.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, property.Name);
            reportDiagnostic(diagnostic);
        }
    }
}