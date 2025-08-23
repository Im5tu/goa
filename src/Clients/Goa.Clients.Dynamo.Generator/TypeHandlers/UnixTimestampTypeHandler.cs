using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles DateTime and DateTimeOffset types with UnixTimestamp attribute.
/// </summary>
public class UnixTimestampTypeHandler : ITypeHandler
{
    public int Priority => 200; // Highest priority - attribute-specific behavior
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        var underlyingType = propertyInfo.UnderlyingType;
        var hasUnixTimestamp = propertyInfo.Attributes.Any(a => a is UnixTimestampAttributeInfo);
        
        return hasUnixTimestamp && 
               (underlyingType.Name == nameof(DateTime) || underlyingType.Name == nameof(DateTimeOffset));
    }
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        var unixAttr = propertyInfo.Attributes.OfType<UnixTimestampAttributeInfo>().First();
        var isMilliseconds = unixAttr.Format == UnixTimestampFormat.Milliseconds;
        
        if (isNullable)
        {
            return isMilliseconds
                ? $"model.{propertyName}.HasValue ? new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}.Value).ToUnixTimeMilliseconds().ToString() }} : new AttributeValue {{ NULL = true }}"
                : $"model.{propertyName}.HasValue ? new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}.Value).ToUnixTimeSeconds().ToString() }} : new AttributeValue {{ NULL = true }}";
        }
        else
        {
            return isMilliseconds
                ? $"new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}).ToUnixTimeMilliseconds().ToString() }}"
                : $"new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}).ToUnixTimeSeconds().ToString() }}";
        }
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;
        var unixAttr = propertyInfo.Attributes.OfType<UnixTimestampAttributeInfo>().First();
        var isMilliseconds = unixAttr.Format == UnixTimestampFormat.Milliseconds;
        
        if (underlyingType.Name == nameof(DateTime))
        {
            if (isNullable)
            {
                return isMilliseconds
                    ? $"{recordVariableName}.TryGetNullableUnixTimestampMilliseconds(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null"
                    : $"{recordVariableName}.TryGetNullableUnixTimestampSeconds(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null";
            }
            else
            {
                return isMilliseconds
                    ? $"{recordVariableName}.TryGetUnixTimestampMilliseconds(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateTime>(\"{memberName}\", {pkVariable}, {skVariable})"
                    : $"{recordVariableName}.TryGetUnixTimestampSeconds(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateTime>(\"{memberName}\", {pkVariable}, {skVariable})";
            }
        }
        else if (underlyingType.Name == nameof(DateTimeOffset))
        {
            if (isNullable)
            {
                return isMilliseconds
                    ? $"{recordVariableName}.TryGetNullableUnixTimestampMillisecondsAsOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null"
                    : $"{recordVariableName}.TryGetNullableUnixTimestampSecondsAsOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null";
            }
            else
            {
                return isMilliseconds
                    ? $"{recordVariableName}.TryGetUnixTimestampMillisecondsAsOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateTimeOffset>(\"{memberName}\", {pkVariable}, {skVariable})"
                    : $"{recordVariableName}.TryGetUnixTimestampSecondsAsOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateTimeOffset>(\"{memberName}\", {pkVariable}, {skVariable})";
            }
        }
        
        return $"default({propertyInfo.Type.ToDisplayString()})";
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        var unixAttr = propertyInfo.Attributes.OfType<UnixTimestampAttributeInfo>().First();
        var isMilliseconds = unixAttr.Format == UnixTimestampFormat.Milliseconds;
        
        return isMilliseconds
            ? $"((DateTimeOffset)model.{propertyInfo.Name}).ToUnixTimeMilliseconds().ToString()"
            : $"((DateTimeOffset)model.{propertyInfo.Name}).ToUnixTimeSeconds().ToString()";
    }
}