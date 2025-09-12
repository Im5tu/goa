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
    
    public string? GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
#pragma warning disable CS8603 // Possible null reference return - intentional for conditional assignment
            ? null // Use conditional assignment instead
#pragma warning restore CS8603
            : $"new AttributeValue {{ S = model.{propertyName}.ToString() }}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var enumType = propertyInfo.UnderlyingType.ToDisplayString();
        var isNullable = propertyInfo.IsNullable;
        
        // Avoid variable name conflicts with pk/sk extraction variables
        var varName = GetSafeVariableName(memberName);
        
        if (isNullable)
        {
            return $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {varName}Str) && Enum.TryParse<{enumType}>({varName}Str, out var {varName}Enum) ? {varName}Enum : null";
        }
        else
        {
            return $"{recordVariableName}.TryGetString(\"{memberName}\", out var {varName}Str) && Enum.TryParse<{enumType}>({varName}Str, out var {varName}Enum) ? {varName}Enum : MissingAttributeException.Throw<{enumType}>(\"{memberName}\", {pkVariable}, {skVariable})";
        }
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
            ? $"(model.{propertyInfo.Name}.HasValue ? model.{propertyInfo.Name}.Value.ToString() : \"NULL\")"
            : $"model.{propertyInfo.Name}.ToString()";
    }
    
    public string? GenerateConditionalAssignment(PropertyInfo propertyInfo, string recordVariable)
    {
        var propertyName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        if (!isNullable)
        {
            return null;
        }
        
        return $@"if (model.{propertyName}.HasValue)
{{
    {recordVariable}[""{propertyName}""] = new AttributeValue {{ S = model.{propertyName}.Value.ToString() }};
}}";
    }
    
    private static string GetSafeVariableName(string memberName)
    {
        // Avoid conflicts with pk/sk extraction variables
        var lowerName = memberName.ToLowerInvariant();
        if (lowerName == "pk" || lowerName == "sk")
        {
            return lowerName + "Prop";  // Use "Prop" suffix to distinguish from extracted values
        }
        return lowerName;
    }
}