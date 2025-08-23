using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

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
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;
        
        return underlyingType.SpecialType switch
        {
            SpecialType.System_String => $"new AttributeValue {{ S = model.{propertyName} ?? string.Empty }}",
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double when isNullable => 
                $"model.{propertyName}.HasValue ? new AttributeValue {{ N = model.{propertyName}.Value.ToString() }} : new AttributeValue {{ NULL = true }}",
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double => 
                $"new AttributeValue {{ N = model.{propertyName}.ToString() }}",
            SpecialType.System_Boolean when isNullable => 
                $"model.{propertyName}.HasValue ? new AttributeValue {{ BOOL = model.{propertyName}.Value }} : new AttributeValue {{ NULL = true }}",
            SpecialType.System_Boolean => 
                $"new AttributeValue {{ BOOL = model.{propertyName} }}",
            SpecialType.System_Char when isNullable => 
                $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString() }} : new AttributeValue {{ NULL = true }}",
            SpecialType.System_Char => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString() }}",
            SpecialType.System_DateTime when isNullable => 
                $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString(\\\"o\\\") }} : new AttributeValue {{ NULL = true }}",
            SpecialType.System_DateTime => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString(\\\"o\\\") }}",
            _ when underlyingType.Name == nameof(Guid) && isNullable => 
                $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString() }} : new AttributeValue {{ NULL = true }}",
            _ when underlyingType.Name == nameof(Guid) => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString() }}",
            _ when underlyingType.Name == nameof(TimeSpan) && isNullable => 
                $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString() }} : new AttributeValue {{ NULL = true }}",
            _ when underlyingType.Name == nameof(TimeSpan) => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString() }}",
            _ when underlyingType.Name == nameof(DateTimeOffset) && isNullable => 
                $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString(\\\"o\\\") }} : new AttributeValue {{ NULL = true }}",
            _ when underlyingType.Name == nameof(DateTimeOffset) => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString(\\\"o\\\") }}",
            _ when underlyingType.TypeKind == TypeKind.Enum && isNullable => 
                $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString() }} : new AttributeValue {{ NULL = true }}",
            _ when underlyingType.TypeKind == TypeKind.Enum => 
                $"new AttributeValue {{ S = model.{propertyName}.ToString() }}",
            _ => string.Empty
        };
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var underlyingType = propertyInfo.UnderlyingType;
        var isNullable = propertyInfo.IsNullable;
        
        return underlyingType.SpecialType switch
        {
            SpecialType.System_String when isNullable => $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_String => $"{recordVariableName}.TryGetString(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<string>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Byte when isNullable => $"{recordVariableName}.TryGetNullableByte(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_Byte => $"{recordVariableName}.TryGetByte(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<byte>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_SByte when isNullable => $"{recordVariableName}.TryGetNullableSByte(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_SByte => $"{recordVariableName}.TryGetSByte(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<sbyte>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Char when isNullable => $"{recordVariableName}.TryGetNullableString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && !string.IsNullOrEmpty({memberName.ToLowerInvariant()}Str) ? {memberName.ToLowerInvariant()}Str[0] : null",
            SpecialType.System_Char => $"{recordVariableName}.TryGetString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && !string.IsNullOrEmpty({memberName.ToLowerInvariant()}Str) ? {memberName.ToLowerInvariant()}Str[0] : MissingAttributeException.Throw<char>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Int16 when isNullable => $"{recordVariableName}.TryGetNullableShort(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_Int16 => $"{recordVariableName}.TryGetShort(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<short>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_UInt16 when isNullable => $"{recordVariableName}.TryGetNullableUShort(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_UInt16 => $"{recordVariableName}.TryGetUShort(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<ushort>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Int32 when isNullable => $"{recordVariableName}.TryGetNullableInt(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_Int32 => $"{recordVariableName}.TryGetInt(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<int>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_UInt32 when isNullable => $"{recordVariableName}.TryGetNullableUInt(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_UInt32 => $"{recordVariableName}.TryGetUInt(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<uint>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Int64 when isNullable => $"{recordVariableName}.TryGetNullableLong(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_Int64 => $"{recordVariableName}.TryGetLong(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<long>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_UInt64 when isNullable => $"{recordVariableName}.TryGetNullableULong(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_UInt64 => $"{recordVariableName}.TryGetULong(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<ulong>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Decimal when isNullable => $"{recordVariableName}.TryGetNullableDecimal(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_Decimal => $"{recordVariableName}.TryGetDecimal(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<decimal>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Single when isNullable => $"{recordVariableName}.TryGetNullableFloat(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_Single => $"{recordVariableName}.TryGetFloat(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<float>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Double when isNullable => $"{recordVariableName}.TryGetNullableDouble(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_Double => $"{recordVariableName}.TryGetDouble(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<double>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_Boolean when isNullable => $"{recordVariableName}.TryGetNullableBool(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_Boolean => $"{recordVariableName}.TryGetBool(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<bool>(\"{memberName}\", {pkVariable}, {skVariable})",
            SpecialType.System_DateTime when isNullable => $"{recordVariableName}.TryGetNullableDateTime(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            SpecialType.System_DateTime => $"{recordVariableName}.TryGetDateTime(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateTime>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ when underlyingType.Name == nameof(Guid) && isNullable => $"{recordVariableName}.TryGetNullableGuid(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            _ when underlyingType.Name == nameof(Guid) => $"{recordVariableName}.TryGetGuid(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<Guid>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ when underlyingType.Name == nameof(TimeSpan) && isNullable => $"{recordVariableName}.TryGetNullableTimeSpan(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            _ when underlyingType.Name == nameof(TimeSpan) => $"{recordVariableName}.TryGetTimeSpan(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<TimeSpan>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ when underlyingType.Name == nameof(DateTimeOffset) && isNullable => $"{recordVariableName}.TryGetNullableDateTimeOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            _ when underlyingType.Name == nameof(DateTimeOffset) => $"{recordVariableName}.TryGetDateTimeOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<DateTimeOffset>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ when underlyingType.TypeKind == TypeKind.Enum && isNullable => $"{recordVariableName}.TryGetNullableEnum<{underlyingType.ToDisplayString()}>(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
            _ when underlyingType.TypeKind == TypeKind.Enum => $"{recordVariableName}.TryGetEnum<{underlyingType.ToDisplayString()}>(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<{underlyingType.ToDisplayString()}>(\"{memberName}\", {pkVariable}, {skVariable})",
            _ => $"default({propertyInfo.Type.ToDisplayString()})"
        };
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        return $"model.{propertyInfo.Name}?.ToString() ?? \"\"";
    }
}