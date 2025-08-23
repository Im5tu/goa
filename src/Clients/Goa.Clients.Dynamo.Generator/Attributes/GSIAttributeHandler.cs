using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.Attributes;

/// <summary>
/// Handles the GlobalSecondaryIndexAttribute.
/// </summary>
public class GSIAttributeHandler : IAttributeHandler
{
    public string AttributeTypeName => "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute";
    
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
        string? indexName = null;
        string? pk = null;
        string? sk = null;
        string? pkName = null;
        string? skName = null;
        
        // Get from constructor arguments (typically IndexName, PK, SK)
        if (attributeData.ConstructorArguments.Length >= 3)
        {
            indexName = attributeData.ConstructorArguments[0].Value?.ToString();
            pk = attributeData.ConstructorArguments[1].Value?.ToString();
            sk = attributeData.ConstructorArguments[2].Value?.ToString();
        }
        
        // Get named arguments
        foreach (var namedArg in attributeData.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "Name":
                    indexName = namedArg.Value.Value?.ToString();
                    break;
                case "PK":
                    pk = namedArg.Value.Value?.ToString();
                    break;
                case "SK":
                    sk = namedArg.Value.Value?.ToString();
                    break;
                case "PKName":
                    pkName = namedArg.Value.Value?.ToString();
                    break;
                case "SKName":
                    skName = namedArg.Value.Value?.ToString();
                    break;
            }
        }
        
        if (string.IsNullOrEmpty(indexName) || string.IsNullOrEmpty(pk) || string.IsNullOrEmpty(sk))
        {
            return null; // Invalid attribute
        }
        
        return new GSIAttributeInfo
        {
            AttributeData = attributeData,
            AttributeTypeName = AttributeTypeName,
            IndexName = indexName!,
            PK = pk!,
            SK = sk!,
            PKName = pkName,
            SKName = skName
        };
    }
    
    public void ValidateAttribute(AttributeInfo attributeInfo, ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        if (attributeInfo is not GSIAttributeInfo gsiAttr)
        {
            return;
        }
        
        // Validate that IndexName is not empty
        if (string.IsNullOrWhiteSpace(gsiAttr.IndexName))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO007",
                title: "Invalid GSI IndexName",
                messageFormat: "GSI IndexName cannot be null or empty on type '{0}'.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
            reportDiagnostic(diagnostic);
        }
        
        // Validate that PK and SK are not empty
        if (string.IsNullOrWhiteSpace(gsiAttr.PK))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO008",
                title: "Invalid GSI PK pattern",
                messageFormat: "GSI PK pattern cannot be null or empty on type '{0}'.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
            reportDiagnostic(diagnostic);
        }
        
        if (string.IsNullOrWhiteSpace(gsiAttr.SK))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO009",
                title: "Invalid GSI SK pattern",
                messageFormat: "GSI SK pattern cannot be null or empty on type '{0}'.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var location = attributeInfo.AttributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation() ?? Location.None;
            var diagnostic = Diagnostic.Create(descriptor, location, symbol.Name);
            reportDiagnostic(diagnostic);
        }
    }
}