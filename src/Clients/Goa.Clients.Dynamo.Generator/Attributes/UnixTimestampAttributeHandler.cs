using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Handles the UnixTimestampAttribute.
/// </summary>
public class UnixTimestampAttributeHandler : IAttributeHandler
{
    public string AttributeTypeName => "Goa.Clients.Dynamo.UnixTimestampAttribute";
    
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
        
        var format = UnixTimestampFormat.Seconds; // Default
        
        // Get format from named arguments
        foreach (var namedArg in attributeData.NamedArguments)
        {
            if (namedArg.Key == "Format")
            {
                if (namedArg.Value.Value is int formatValue)
                {
                    format = (UnixTimestampFormat)formatValue;
                }
            }
        }
        
        return new UnixTimestampAttributeInfo
        {
            AttributeData = attributeData,
            AttributeTypeName = AttributeTypeName,
            Format = format
        };
    }
    
    public void ValidateAttribute(AttributeInfo attributeInfo, ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        if (attributeInfo is not UnixTimestampAttributeInfo)
        {
            return;
        }
        
        // Validate that the attribute is applied to DateTime or DateTimeOffset properties
        if (symbol is IPropertySymbol property)
        {
            var underlyingType = property.Type;
            if (property.Type is INamedTypeSymbol nullableType && nullableType.IsGenericType)
            {
                underlyingType = nullableType.TypeArguments[0];
            }
            
            if (underlyingType.Name != nameof(DateTime) && underlyingType.Name != nameof(DateTimeOffset))
            {
                var descriptor = new DiagnosticDescriptor(
                    id: "DYNAMO006",
                    title: "Invalid UnixTimestamp usage",
                    messageFormat: "UnixTimestamp attribute can only be applied to DateTime or DateTimeOffset properties. Property '{0}' has type '{1}'.",
                    category: "DynamoDB",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true);
                
                var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
                var diagnostic = Diagnostic.Create(descriptor, location, property.Name, property.Type.ToDisplayString());
                reportDiagnostic(diagnostic);
            }
        }
    }
}