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
            // Return null to indicate conditional assignment should be used
#pragma warning disable CS8603 // Possible null reference return - intentional for conditional assignment
            return null;
#pragma warning restore CS8603
        }
        else
        {
            return isMilliseconds
                ? $"new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}).ToUnixTimeMilliseconds().ToString() }}"
                : $"new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}).ToUnixTimeSeconds().ToString() }}";
        }
    }
    
    public string? GenerateConditionalAssignment(PropertyInfo propertyInfo, string recordVariable)
    {
        var propertyName = propertyInfo.Name;
        var dynamoAttributeName = propertyInfo.GetDynamoAttributeName();
        var isNullable = propertyInfo.IsNullable;
        
        if (!isNullable)
        {
            return null; // Non-nullable properties don't need conditional assignment
        }
        
        var unixAttr = propertyInfo.Attributes.OfType<UnixTimestampAttributeInfo>().First();
        var isMilliseconds = unixAttr.Format == UnixTimestampFormat.Milliseconds;
        
        return isMilliseconds
            ? $@"if (model.{propertyName}.HasValue)
{{
    {recordVariable}[""{dynamoAttributeName}""] = new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}.Value).ToUnixTimeMilliseconds().ToString() }};
}}"
            : $@"if (model.{propertyName}.HasValue)
{{
    {recordVariable}[""{dynamoAttributeName}""] = new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}.Value).ToUnixTimeSeconds().ToString() }};
}}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.GetDynamoAttributeName();
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;
        var unixAttr = propertyInfo.Attributes.OfType<UnixTimestampAttributeInfo>().First();
        var isMilliseconds = unixAttr.Format == UnixTimestampFormat.Milliseconds;
        
        // Avoid variable name conflicts with pk/sk extraction variables
        var varName = GetSafeVariableName(memberName);
        
        if (underlyingType.Name == nameof(DateTime))
        {
            if (isNullable)
            {
                return isMilliseconds
                    ? $"{recordVariableName}.TryGetNullableUnixTimestampMilliseconds(\"{memberName}\", out var {varName}) ? {varName} : null"
                    : $"{recordVariableName}.TryGetNullableUnixTimestampSeconds(\"{memberName}\", out var {varName}) ? {varName} : null";
            }
            else
            {
                return isMilliseconds
                    ? $"{recordVariableName}.TryGetUnixTimestampMilliseconds(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<DateTime>(\"{memberName}\", {pkVariable}, {skVariable})"
                    : $"{recordVariableName}.TryGetUnixTimestampSeconds(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<DateTime>(\"{memberName}\", {pkVariable}, {skVariable})";
            }
        }
        else if (underlyingType.Name == nameof(DateTimeOffset))
        {
            if (isNullable)
            {
                return isMilliseconds
                    ? $"{recordVariableName}.TryGetNullableUnixTimestampMillisecondsAsOffset(\"{memberName}\", out var {varName}) ? {varName} : null"
                    : $"{recordVariableName}.TryGetNullableUnixTimestampSecondsAsOffset(\"{memberName}\", out var {varName}) ? {varName} : null";
            }
            else
            {
                return isMilliseconds
                    ? $"{recordVariableName}.TryGetUnixTimestampMillisecondsAsOffset(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<DateTimeOffset>(\"{memberName}\", {pkVariable}, {skVariable})"
                    : $"{recordVariableName}.TryGetUnixTimestampSecondsAsOffset(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<DateTimeOffset>(\"{memberName}\", {pkVariable}, {skVariable})";
            }
        }
        
        return $"default({propertyInfo.Type.ToDisplayString()})";
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
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        var unixAttr = propertyInfo.Attributes.OfType<UnixTimestampAttributeInfo>().First();
        var isMilliseconds = unixAttr.Format == UnixTimestampFormat.Milliseconds;
        var isNullable = propertyInfo.IsNullable;
        
        if (isNullable)
        {
            // For nullable DateTime, provide a default value when null
            // Wrap in parentheses for string interpolation
            return isMilliseconds
                ? $"(model.{propertyInfo.Name}.HasValue ? ((DateTimeOffset)model.{propertyInfo.Name}.Value).ToUnixTimeMilliseconds().ToString() : \"NULL\")"
                : $"(model.{propertyInfo.Name}.HasValue ? ((DateTimeOffset)model.{propertyInfo.Name}.Value).ToUnixTimeSeconds().ToString() : \"NULL\")";
        }
        else
        {
            return isMilliseconds
                ? $"((DateTimeOffset)model.{propertyInfo.Name}).ToUnixTimeMilliseconds().ToString()"
                : $"((DateTimeOffset)model.{propertyInfo.Name}).ToUnixTimeSeconds().ToString()";
        }
    }
}