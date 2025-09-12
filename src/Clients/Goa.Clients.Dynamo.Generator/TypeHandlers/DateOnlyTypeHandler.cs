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
    
    public string? GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
#pragma warning disable CS8603 // Possible null reference return - intentional for conditional assignment
            ? null // Use conditional assignment instead
#pragma warning restore CS8603
            : $"new AttributeValue {{ S = model.{propertyName}.ToString(\"yyyy-MM-dd\") }}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var isNullable = propertyInfo.IsNullable;
        
        // Avoid variable name conflicts with pk/sk extraction variables
        var varName = GetSafeVariableName(memberName);
        
        return isNullable
            ? $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {varName}Str) && DateOnly.TryParse({varName}Str, out var {varName}) ? {varName} : (DateOnly?)null"
            : $"{recordVariableName}.TryGetString(\"{memberName}\", out var {varName}Str) && DateOnly.TryParse({varName}Str, out var {varName}) ? {varName} : MissingAttributeException.Throw<DateOnly>(\"{memberName}\", {pkVariable}, {skVariable})";
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        var isNullable = propertyInfo.IsNullable;
        
        return isNullable
            ? $"(model.{propertyInfo.Name}.HasValue ? model.{propertyInfo.Name}.Value.ToString(\"yyyy-MM-dd\") : \"NULL\")"
            : $"model.{propertyInfo.Name}.ToString(\"yyyy-MM-dd\")";
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
    {recordVariable}[""{propertyName}""] = new AttributeValue {{ S = model.{propertyName}.Value.ToString(""yyyy-MM-dd"") }};
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