using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles DateOnly types (including nullable variants).
/// </summary>
public class DateOnlyTypeHandler : ITypeHandler
{
    public int Priority => 150; // Same as DateTimeTypeHandler - higher than primitive but lower than attribute-specific
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        var underlyingType = propertyInfo.UnderlyingType;
        return underlyingType?.Name == "DateOnly";
    }
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
            ? $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString(\"yyyy-MM-dd\") }} : new AttributeValue {{ NULL = true }}"
            : $"new AttributeValue {{ S = model.{propertyName}.ToString(\"yyyy-MM-dd\") }}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
            ? $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && DateOnly.TryParse({memberName.ToLowerInvariant()}Str, out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : (DateOnly?)null"
            : $"{recordVariableName}.TryGetString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && DateOnly.TryParse({memberName.ToLowerInvariant()}Str, out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateOnly>(\"{memberName}\", {pkVariable}, {skVariable})";
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        return $"model.{propertyInfo.Name}.ToString(\"yyyy-MM-dd\")";
    }
}