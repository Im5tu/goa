using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Handles the ExtensionAttribute.
/// </summary>
public class ExtensionAttributeHandler : IAttributeHandler
{
    public string AttributeTypeName => "Goa.Clients.Dynamo.ExtensionAttribute";

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

        return new ExtensionAttributeInfo
        {
            AttributeData = attributeData,
            AttributeTypeName = AttributeTypeName
        };
    }

    public void ValidateAttribute(AttributeInfo attributeInfo, ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        if (attributeInfo is not ExtensionAttributeInfo)
        {
            return;
        }

        // Validate that the attribute is applied to classes only
        if (symbol is not INamedTypeSymbol)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO012",
                title: "Invalid Extension usage",
                messageFormat: "Extension attribute can only be applied to classes. Symbol '{0}' is not a class.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
            reportDiagnostic(diagnostic);
        }
    }
}
