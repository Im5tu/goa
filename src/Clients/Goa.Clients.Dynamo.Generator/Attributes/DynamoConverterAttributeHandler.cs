using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Handles the DynamoConverterAttribute.
/// </summary>
public class DynamoConverterAttributeHandler : IAttributeHandler
{
    public string AttributeTypeName => "Goa.Clients.Dynamo.DynamoConverterAttribute";

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

        // Extract converter type from constructor arg
        if (attributeData.ConstructorArguments.Length > 0 &&
            attributeData.ConstructorArguments[0].Value is INamedTypeSymbol converterType)
        {
            return new DynamoConverterAttributeInfo
            {
                AttributeData = attributeData,
                AttributeTypeName = AttributeTypeName,
                ConverterTypeName = converterType.ToDisplayString()
            };
        }

        return null;
    }

    public void ValidateAttribute(AttributeInfo attributeInfo, ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        // No additional validation needed at this stage
    }
}
