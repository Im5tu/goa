using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles TimeOnly types (including nullable variants).
/// </summary>
public class TimeOnlyTypeHandler : ITypeHandler
{
    public int Priority => 150; // Same as DateTimeTypeHandler - higher than primitive but lower than attribute-specific
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        var underlyingType = propertyInfo.UnderlyingType;
        return underlyingType?.Name == "TimeOnly";
    }
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
            ? $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString(\"HH:mm:ss.fffffff\") }} : new AttributeValue {{ NULL = true }}"
            : $"new AttributeValue {{ S = model.{propertyName}.ToString(\"HH:mm:ss.fffffff\") }}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
            ? $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && TimeOnly.TryParse({memberName.ToLowerInvariant()}Str, out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : (TimeOnly?)null"
            : $"{recordVariableName}.TryGetString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && TimeOnly.TryParse({memberName.ToLowerInvariant()}Str, out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<TimeOnly>(\"{memberName}\", {pkVariable}, {skVariable})";
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        return $"model.{propertyInfo.Name}.ToString(\"HH:mm:ss.fffffff\")";
    }
}