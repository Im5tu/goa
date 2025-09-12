using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Handles the IgnoreAttribute.
/// </summary>
public class IgnoreAttributeHandler : IAttributeHandler
{
    public string AttributeTypeName => "Goa.Clients.Dynamo.IgnoreAttribute";
    
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
        
        var direction = IgnoreDirection.Always; // Default
        
        // Get direction from named arguments
        foreach (var namedArg in attributeData.NamedArguments)
        {
            if (namedArg.Key == "Direction")
            {
                if (namedArg.Value.Value is int directionValue)
                {
                    direction = (IgnoreDirection)directionValue;
                }
            }
        }
        
        return new IgnoreAttributeInfo
        {
            AttributeData = attributeData,
            AttributeTypeName = AttributeTypeName,
            Direction = direction
        };
    }
    
    public void ValidateAttribute(AttributeInfo attributeInfo, ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        if (attributeInfo is not IgnoreAttributeInfo)
        {
            return;
        }
        
        // Validate that the attribute is applied to properties only
        if (symbol is not IPropertySymbol)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO007",
                title: "Invalid Ignore usage",
                messageFormat: "Ignore attribute can only be applied to properties. Symbol '{0}' is not a property.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
            reportDiagnostic(diagnostic);
        }
    }
}