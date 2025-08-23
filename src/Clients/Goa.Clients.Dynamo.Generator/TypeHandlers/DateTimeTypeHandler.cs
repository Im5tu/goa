using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles DateTime and DateTimeOffset types (without unix timestamp attribute).
/// </summary>
public class DateTimeTypeHandler : ITypeHandler
{
    public int Priority => 150; // Higher than primitive but lower than attribute-specific
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        var underlyingType = propertyInfo.UnderlyingType;
        
        // Only handle if there's NO UnixTimestamp attribute
        var hasUnixTimestamp = propertyInfo.Attributes.Any(a => a is UnixTimestampAttributeInfo);
        if (hasUnixTimestamp)
        {
            return false;
        }
        
        return underlyingType.Name == nameof(DateTime) || underlyingType.Name == nameof(DateTimeOffset);
    }
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
            ? $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString(\"o\") }} : new AttributeValue {{ NULL = true }}"
            : $"new AttributeValue {{ S = model.{propertyName}.ToString(\"o\") }}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;
        
        if (underlyingType.Name == nameof(DateTime))
        {
            return isNullable
                ? $"{recordVariableName}.TryGetNullableDateTime(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null"
                : $"{recordVariableName}.TryGetDateTime(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateTime>(\"{memberName}\", {pkVariable}, {skVariable})";
        }
        else if (underlyingType.Name == nameof(DateTimeOffset))
        {
            return isNullable
                ? $"{recordVariableName}.TryGetNullableDateTimeOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null"
                : $"{recordVariableName}.TryGetDateTimeOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateTimeOffset>(\"{memberName}\", {pkVariable}, {skVariable})";
        }
        
        return $"default({propertyInfo.Type.ToDisplayString()})";
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        return $"model.{propertyInfo.Name}.ToString(\"o\")";
    }
}