using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Handles the DynamoModelAttribute.
/// </summary>
public class DynamoModelAttributeHandler : IAttributeHandler
{
    public string AttributeTypeName => "Goa.Clients.Dynamo.DynamoModelAttribute";
    
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
        
        // Extract constructor arguments and named arguments
        string? pk = null;
        string? sk = null;
        string pkName = "PK";
        string skName = "SK";
        string typeName = "Type";
        
        // Get PK and SK from constructor arguments
        if (attributeData.ConstructorArguments.Length >= 2)
        {
            pk = attributeData.ConstructorArguments[0].Value?.ToString();
            sk = attributeData.ConstructorArguments[1].Value?.ToString();
        }
        
        // Get named arguments for PKName, SKName, and TypeName
        foreach (var namedArg in attributeData.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "PK":
                    pk = namedArg.Value.Value?.ToString();
                    break;
                case "SK":
                    sk = namedArg.Value.Value?.ToString();
                    break;
                case "PKName":
                    pkName = namedArg.Value.Value?.ToString() ?? "PK";
                    break;
                case "SKName":
                    skName = namedArg.Value.Value?.ToString() ?? "SK";
                    break;
                case "TypeName":
                    typeName = namedArg.Value.Value?.ToString() ?? "Type";
                    break;
            }
        }
        
        if (string.IsNullOrEmpty(pk) || string.IsNullOrEmpty(sk))
        {
            return null; // Invalid attribute
        }
        
        return new DynamoModelAttributeInfo
        {
            AttributeData = attributeData,
            AttributeTypeName = AttributeTypeName,
            PK = pk!,
            SK = sk!,
            PKName = pkName,
            SKName = skName,
            TypeName = typeName
        };
    }
    
    public void ValidateAttribute(AttributeInfo attributeInfo, ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        if (attributeInfo is not DynamoModelAttributeInfo dynamoAttr)
        {
            return;
        }
        
        // Validate that PK and SK are not empty
        if (string.IsNullOrWhiteSpace(dynamoAttr.PK))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO002",
                title: "Invalid PK pattern",
                messageFormat: "PK pattern cannot be null or empty on type '{0}'.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
            reportDiagnostic(diagnostic);
        }
        
        if (string.IsNullOrWhiteSpace(dynamoAttr.SK))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO003",
                title: "Invalid SK pattern",
                messageFormat: "SK pattern cannot be null or empty on type '{0}'.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
            reportDiagnostic(diagnostic);
        }
    }
}