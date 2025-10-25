using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;
using System.Globalization;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles primitive types: string, numbers, bool, char, Guid, TimeSpan.
/// </summary>
public class PrimitiveTypeHandler : ITypeHandler
{
    public int Priority => 100; // Lower priority than attribute-specific handlers
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        var underlyingType = propertyInfo.UnderlyingType;
        
        return underlyingType.SpecialType switch
        {
            SpecialType.System_String => true,
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double => true,
            SpecialType.System_Boolean => true,
            SpecialType.System_Char => true,
            SpecialType.System_DateTime => true,
            _ => underlyingType.Name == nameof(Guid) || underlyingType.Name == nameof(TimeSpan) || 
                 underlyingType.Name == nameof(DateTimeOffset) || underlyingType.TypeKind == TypeKind.Enum
        };
    }
    
    public string? GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;
        
        return underlyingType.SpecialType switch
        {
            SpecialType.System_String =>
                null, // Use conditional assignment to skip empty strings
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double when isNullable =>
                null, // Use conditional assignment instead
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double =>
                $"new AttributeValue {{ N = model.{propertyName}.ToString(CultureInfo.InvariantCulture) }}",
            SpecialType.System_Boolean when isNullable => 
                null, // Use conditional assignment instead
            SpecialType.System_Boolean => 
                $"new AttributeValue {{ BOOL = model.{propertyName} }}",
            SpecialType.System_Char when isNullable => 
                null, // Use conditional assignment instead
            SpecialType.System_Char => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString() }}",
            SpecialType.System_DateTime when isNullable => 
                null, // Use conditional assignment instead (handled by DateTimeTypeHandler)
            SpecialType.System_DateTime => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString(\\\"o\\\") }}",
            _ when underlyingType.Name == nameof(Guid) && isNullable => 
                null, // Use conditional assignment instead
            _ when underlyingType.Name == nameof(Guid) => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString() }}",
            _ when underlyingType.Name == nameof(TimeSpan) && isNullable => 
                null, // Use conditional assignment instead
            _ when underlyingType.Name == nameof(TimeSpan) => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString() }}",
            _ when underlyingType.Name == nameof(DateTimeOffset) && isNullable => 
                null, // Use conditional assignment instead
            _ when underlyingType.Name == nameof(DateTimeOffset) => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString(\\\"o\\\") }}",
            _ when underlyingType.TypeKind == TypeKind.Enum && isNullable => 
                null, // Use conditional assignment instead
            _ when underlyingType.TypeKind == TypeKind.Enum => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString() }}",
            _ => string.Empty
        };
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.GetDynamoAttributeName();
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;
        
        // Avoid variable name conflicts with pk/sk extraction variables
        var varName = GetSafeVariableName(memberName);
        
        return underlyingType.SpecialType switch
        {
            SpecialType.System_String when isNullable => $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_String => $"{recordVariableName}.TryGetString(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<string>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Byte when isNullable => $"{recordVariableName}.TryGetNullableByte(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Byte => $"{recordVariableName}.TryGetByte(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<byte>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_SByte when isNullable => $"{recordVariableName}.TryGetNullableSByte(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_SByte => $"{recordVariableName}.TryGetSByte(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<sbyte>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Char when isNullable => $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {varName}Str) && !string.IsNullOrEmpty({varName}Str) ? {varName}Str[0] : null",
            SpecialType.System_Char => $"{recordVariableName}.TryGetString(\"{memberName}\", out var {varName}Str) && !string.IsNullOrEmpty({varName}Str) ? {varName}Str[0] : MissingAttributeException.Throw<char>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Int16 when isNullable => $"{recordVariableName}.TryGetNullableShort(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Int16 => $"{recordVariableName}.TryGetShort(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<short>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_UInt16 when isNullable => $"{recordVariableName}.TryGetNullableUShort(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_UInt16 => $"{recordVariableName}.TryGetUShort(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<ushort>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Int32 when isNullable => $"{recordVariableName}.TryGetNullableInt(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Int32 => $"{recordVariableName}.TryGetInt(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<int>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_UInt32 when isNullable => $"{recordVariableName}.TryGetNullableUInt(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_UInt32 => $"{recordVariableName}.TryGetUInt(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<uint>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Int64 when isNullable => $"{recordVariableName}.TryGetNullableLong(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Int64 => $"{recordVariableName}.TryGetLong(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<long>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_UInt64 when isNullable => $"{recordVariableName}.TryGetNullableULong(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_UInt64 => $"{recordVariableName}.TryGetULong(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<ulong>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Decimal when isNullable => $"{recordVariableName}.TryGetNullableDecimal(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Decimal => $"{recordVariableName}.TryGetDecimal(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<decimal>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Single when isNullable => $"{recordVariableName}.TryGetNullableFloat(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Single => $"{recordVariableName}.TryGetFloat(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<float>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Double when isNullable => $"{recordVariableName}.TryGetNullableDouble(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Double => $"{recordVariableName}.TryGetDouble(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<double>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Boolean when isNullable => $"{recordVariableName}.TryGetNullableBool(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Boolean => $"{recordVariableName}.TryGetBool(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<bool>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_DateTime when isNullable => $"{recordVariableName}.TryGetNullableDateTime(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_DateTime => $"{recordVariableName}.TryGetDateTime(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<DateTime>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ when underlyingType.Name == nameof(Guid) && isNullable => $"{recordVariableName}.TryGetNullableGuid(\"{memberName}\", out var {varName}) ? {varName} : null",
            _ when underlyingType.Name == nameof(Guid) => $"{recordVariableName}.TryGetGuid(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<Guid>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ when underlyingType.Name == nameof(TimeSpan) && isNullable => $"{recordVariableName}.TryGetNullableTimeSpan(\"{memberName}\", out var {varName}) ? {varName} : null",
            _ when underlyingType.Name == nameof(TimeSpan) => $"{recordVariableName}.TryGetTimeSpan(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<TimeSpan>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ when underlyingType.Name == nameof(DateTimeOffset) && isNullable => $"{recordVariableName}.TryGetNullableDateTimeOffset(\"{memberName}\", out var {varName}) ? {varName} : null",
            _ when underlyingType.Name == nameof(DateTimeOffset) => $"{recordVariableName}.TryGetDateTimeOffset(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<DateTimeOffset>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ when underlyingType.TypeKind == TypeKind.Enum && isNullable => $"{recordVariableName}.TryGetNullableEnum<{underlyingType.ToDisplayString()}>(\"{memberName}\", out var {varName}) ? {varName} : null",
            _ when underlyingType.TypeKind == TypeKind.Enum => $"{recordVariableName}.TryGetEnum<{underlyingType.ToDisplayString()}>(\"{memberName}\", out var {varName}) ? {varName} : MissingAttributeException.Throw<{underlyingType.ToDisplayString()}>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ => $"default({propertyInfo.Type.ToDisplayString()})"
        };
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        return $"model.{propertyInfo.Name}?.ToString() ?? \"\"";
    }
    
    public string? GenerateConditionalAssignment(PropertyInfo propertyInfo, string recordVariable)
    {
        var propertyName = propertyInfo.Name;
        var dynamoAttributeName = propertyInfo.GetDynamoAttributeName();
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;

        // String types (both nullable and non-nullable) need conditional assignment to skip empty strings
        var isString = underlyingType.SpecialType == SpecialType.System_String;

        if (!isNullable && !isString)
        {
            return null; // Non-nullable non-string properties don't need conditional assignment
        }
        
        var attributeValue = underlyingType.SpecialType switch
        {
            SpecialType.System_String =>
                $"new AttributeValue {{ S = model.{propertyName} }}",
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double =>
                $"new AttributeValue {{ N = model.{propertyName}.Value.ToString(CultureInfo.InvariantCulture) }}",
            SpecialType.System_Boolean =>
                $"new AttributeValue {{ BOOL = model.{propertyName}.Value }}",
            SpecialType.System_Char =>
                $"new AttributeValue {{ S = model.{propertyName}.Value.ToString() }}",
            SpecialType.System_DateTime =>
                $"new AttributeValue {{ S = model.{propertyName}.Value.ToString(\\\"o\\\") }}",
            _ when underlyingType.Name == nameof(Guid) =>
                $"new AttributeValue {{ S = model.{propertyName}.Value.ToString() }}",
            _ when underlyingType.Name == nameof(TimeSpan) =>
                $"new AttributeValue {{ S = model.{propertyName}.Value.ToString() }}",
            _ when underlyingType.Name == nameof(DateTimeOffset) =>
                $"new AttributeValue {{ S = model.{propertyName}.Value.ToString(\\\"o\\\") }}",
            _ when underlyingType.TypeKind == TypeKind.Enum =>
                $"new AttributeValue {{ S = model.{propertyName}.Value.ToString() }}",
            _ => null
        };

        if (attributeValue == null)
        {
            return null;
        }

        // For strings (nullable or non-nullable), check if not null or empty; for other nullable types, check HasValue
        var condition = underlyingType.SpecialType == SpecialType.System_String
            ? $"!string.IsNullOrEmpty(model.{propertyName})"
            : $"model.{propertyName}.HasValue";

        return $@"if ({condition})
            {{
                {recordVariable}[""{dynamoAttributeName}""] = {attributeValue};
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