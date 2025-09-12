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
        
        // For nullable properties, return null to indicate conditional assignment should be used
        // For non-nullable properties, return the attribute value expression
#pragma warning disable CS8603 // Possible null reference return - intentional for conditional assignment
        return isNullable
            ? null  // This will trigger conditional assignment generation in MapperGenerator
            : $"new AttributeValue {{ S = model.{propertyName}.ToString(\"o\") }}";
#pragma warning restore CS8603
    }
    
    public string GenerateConditionalAssignment(PropertyInfo propertyInfo, string recordVariable)
    {
        var propertyName = propertyInfo.Name;
        return $@"if (model.{propertyName}.HasValue)
            {{
                {recordVariable}[""{propertyName}""] = new AttributeValue {{ S = model.{propertyName}.Value.ToString(""o"") }};
            }}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;
        
        // Avoid variable name conflicts with pk/sk extraction variables
        var varName = GetSafeVariableName(memberName);
        
        if (underlyingType.Name == nameof(DateTime))
        {
            return isNullable
                ? $"{recordVariableName}.TryGetNullableDateTime(\"{memberName}\", out var {varName}) ? {varName} : null"
                : $"{recordVariableName}.TryGetDateTime(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<DateTime>(\"{memberName}\", {pkVariable}, {skVariable})";
        }
        else if (underlyingType.Name == nameof(DateTimeOffset))
        {
            return isNullable
                ? $"{recordVariableName}.TryGetNullableDateTimeOffset(\"{memberName}\", out var {varName}) ? {varName} : null"
                : $"{recordVariableName}.TryGetDateTimeOffset(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<DateTimeOffset>(\"{memberName}\", {pkVariable}, {skVariable})";
        }
        
        return $"default({propertyInfo.Type.ToDisplayString()})";
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        var isNullable = propertyInfo.IsNullable;
        
        if (isNullable)
        {
            // For nullable DateTime, use .HasValue and .Value for proper null handling
            // Wrap in parentheses for string interpolation
            return $"(model.{propertyInfo.Name}.HasValue ? model.{propertyInfo.Name}.Value.ToString(\"o\") : \"NULL\")";
        }
        else
        {
            return $"model.{propertyInfo.Name}.ToString(\"o\")";
        }
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