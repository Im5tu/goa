using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles unsupported dictionary types (non-string keys) by returning NULL.
/// </summary>
public class UnsupportedDictionaryHandler : ITypeHandler
{
    public int Priority => 60; // Higher than ComplexTypeHandler but lower than others
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        // Handle only unsupported dictionaries (non-string keys)
        if (propertyInfo.IsDictionary && propertyInfo.DictionaryTypes.HasValue)
        {
            var (keyType, _) = propertyInfo.DictionaryTypes.Value;
            return keyType.SpecialType != SpecialType.System_String;
        }
        
        return false;
    }
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        // Unsupported dictionary types always serialize as NULL
        return "new AttributeValue { NULL = true }";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var typeName = propertyInfo.Type.ToDisplayString();
        var isNullable = propertyInfo.IsNullable;
        
        if (isNullable)
        {
            return "null";
        }
        else
        {
            return $"MissingAttributeException.Throw<{typeName}>(\"{propertyInfo.Name}\", {pkVariable}, {skVariable})";
        }
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        // Unsupported dictionaries in keys - return empty string
        return "\"\"";
    }
}