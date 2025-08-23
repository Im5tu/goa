using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles enum types.
/// </summary>
public class EnumTypeHandler : ITypeHandler
{
    public int Priority => 120; // Higher than primitive
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        var underlyingType = propertyInfo.UnderlyingType;
        return underlyingType.TypeKind == TypeKind.Enum;
    }
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
            ? $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString() }} : new AttributeValue {{ NULL = true }}"
            : $"new AttributeValue {{ S = model.{propertyName}.ToString() }}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var enumType = propertyInfo.UnderlyingType.ToDisplayString();
        var isNullable = propertyInfo.IsNullable;
        
        if (isNullable)
        {
            return $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && Enum.TryParse<{enumType}>({memberName.ToLowerInvariant()}Str, out var {memberName.ToLowerInvariant()}Enum) ? {memberName.ToLowerInvariant()}Enum : null";
        }
        else
        {
            return $"{recordVariableName}.TryGetString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && Enum.TryParse<{enumType}>({memberName.ToLowerInvariant()}Str, out var {memberName.ToLowerInvariant()}Enum) ? {memberName.ToLowerInvariant()}Enum : MissingAttributeException.Throw<{enumType}>(\"{memberName}\", {pkVariable}, {skVariable})";
        }
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        return $"model.{propertyInfo.Name}.ToString()";
    }
}