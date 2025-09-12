using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles complex types (records, classes, dictionaries).
/// Supports composition for complex dictionary value types through the registry.
/// </summary>
public class ComplexTypeHandler : ICompositeTypeHandler
{
    private TypeHandlerRegistry? _registry;
    
    public int Priority => 50; // Lowest priority - fallback handler
    
    public void SetRegistry(TypeHandlerRegistry registry)
    {
        _registry = registry;
    }
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        // Handle supported dictionaries (string-keyed only)
        if (propertyInfo.IsDictionary && propertyInfo.DictionaryTypes.HasValue)
        {
            var (keyType, _) = propertyInfo.DictionaryTypes.Value;
            return keyType.SpecialType == SpecialType.System_String;
        }
        
        // Handle complex types (objects, records) but not collections, primitionaries, or unsupported dictionaries
        return (!propertyInfo.IsCollection && 
                !propertyInfo.IsDictionary &&
                !IsPrimitiveType(propertyInfo.Type) &&
                (propertyInfo.Type.TypeKind == TypeKind.Class || propertyInfo.Type.TypeKind == TypeKind.Struct || propertyInfo.Type.IsRecord));
    }
    
    private static bool IsPrimitiveType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Boolean or SpecialType.System_Char or
            SpecialType.System_SByte or SpecialType.System_Byte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double or
            SpecialType.System_String or SpecialType.System_DateTime => true,
            _ => type.Name == nameof(Guid) || type.Name == nameof(TimeSpan) || type.Name == nameof(DateTimeOffset) || type.TypeKind == TypeKind.Enum
        };
    }
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        
        // Check for dictionary types
        if (propertyInfo.IsDictionary && propertyInfo.DictionaryTypes.HasValue)
        {
            var (keyType, valueType) = propertyInfo.DictionaryTypes.Value;
            
            // Only handle string-keyed dictionaries (DynamoDB limitation)
            if (keyType.SpecialType == SpecialType.System_String)
            {
                // Special case: Dictionary<string, List<string>>
                if (valueType is INamedTypeSymbol namedValueType && 
                    namedValueType.Name == "List" && 
                    namedValueType.TypeArguments.Length == 1 &&
                    namedValueType.TypeArguments[0].SpecialType == SpecialType.System_String)
                {
                    return $"model.{propertyName} != null ? new AttributeValue {{ M = model.{propertyName}.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue {{ SS = kvp.Value ?? new List<string>() }}) }} : new AttributeValue {{ NULL = true }}";
                }
                
                // Special case: Dictionary<string, Dictionary<string, string>>  
                if (valueType is INamedTypeSymbol namedValueType2 && 
                    namedValueType2.Name == "Dictionary" && 
                    namedValueType2.TypeArguments.Length == 2 &&
                    namedValueType2.TypeArguments[0].SpecialType == SpecialType.System_String &&
                    namedValueType2.TypeArguments[1].SpecialType == SpecialType.System_String)
                {
                    return $"model.{propertyName} != null ? new AttributeValue {{ M = model.{propertyName}.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue {{ M = (kvp.Value ?? new Dictionary<string, string>()).ToDictionary(innerKvp => innerKvp.Key, innerKvp => new AttributeValue {{ S = innerKvp.Value }}) }}) }} : new AttributeValue {{ NULL = true }}";
                }
                
                // Fallback to primitive handling for other types
                return GeneratePrimitiveDictionary(propertyName, valueType);
            }
            
            // For unsupported dictionary types, return NULL
            return "new AttributeValue { NULL = true }";
        }
        
        // Handle complex types (nested objects)
        var normalizedTypeName = propertyInfo.UnderlyingType.Name.Replace(".", "_").Replace("`", "_");
        return $"model.{propertyName} != null ? new AttributeValue {{ M = DynamoMapper.{normalizedTypeName}.ToDynamoRecord(model.{propertyName}) }} : new AttributeValue {{ NULL = true }}";
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var typeName = propertyInfo.Type.ToDisplayString();
        
        // Check for dictionary types
        if (propertyInfo.IsDictionary && propertyInfo.DictionaryTypes.HasValue)
        {
            var (keyType, valueType) = propertyInfo.DictionaryTypes.Value;
            
            // Only handle string-keyed dictionaries (DynamoDB limitation)
            if (keyType.SpecialType == SpecialType.System_String)
            {
                if (valueType.SpecialType == SpecialType.System_String)
                {
                    // Use the existing TryGetStringDictionary
                    return GenerateDictionaryConversion(propertyInfo.Type, 
                        $"{recordVariableName}.TryGetStringDictionary(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null");
                }
                else if (valueType.SpecialType == SpecialType.System_Int32)
                {
                    // Use the existing TryGetStringIntDictionary
                    return GenerateDictionaryConversion(propertyInfo.Type,
                        $"{recordVariableName}.TryGetStringIntDictionary(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null");
                }
                else if (valueType.SpecialType == SpecialType.System_Double)
                {
                    // Handle Dictionary<string, double>
                    var dictVarName = memberName.ToLowerInvariant();
                    return $"{recordVariableName}.TryGetMap(\"{memberName}\", out var {dictVarName}Map) && {dictVarName}Map != null ? " +
                           $"{dictVarName}Map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.N != null ? double.Parse(kvp.Value.N) : 0.0) : " +
                           GenerateEmptyDictionary(propertyInfo.Type, keyType, valueType);
                }
                else if (valueType.SpecialType == SpecialType.System_DateTime)
                {
                    // Handle Dictionary<string, DateTime>
                    var dictVarName = memberName.ToLowerInvariant();
                    return $"{recordVariableName}.TryGetMap(\"{memberName}\", out var {dictVarName}Map) && {dictVarName}Map != null ? " +
                           $"{dictVarName}Map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.S != null ? DateTime.Parse(kvp.Value.S) : default(DateTime)) : " +
                           GenerateEmptyDictionary(propertyInfo.Type, keyType, valueType);
                }
                else if (valueType.TypeKind == TypeKind.Enum)
                {
                    // Handle Dictionary<string, TEnum>
                    var dictVarName = memberName.ToLowerInvariant();
                    var enumTypeName = valueType.ToDisplayString();
                    return $"{recordVariableName}.TryGetMap(\"{memberName}\", out var {dictVarName}Map) && {dictVarName}Map != null ? " +
                           $"{dictVarName}Map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.S != null ? Enum.Parse<{enumTypeName}>(kvp.Value.S) : default({enumTypeName})) : " +
                           GenerateEmptyDictionary(propertyInfo.Type, keyType, valueType);
                }
                else
                {
                    // For complex value types, handle specific common cases
                    var dictVarName = memberName.ToLowerInvariant();
                    
                    // Special case: Dictionary<string, List<string>>
                    if (valueType is INamedTypeSymbol namedValueType && 
                        namedValueType.Name == "List" && 
                        namedValueType.TypeArguments.Length == 1 &&
                        namedValueType.TypeArguments[0].SpecialType == SpecialType.System_String)
                    {
                        return $"{recordVariableName}.TryGetMap(\"{memberName}\", out var {dictVarName}Map) && {dictVarName}Map != null ? " +
                               $"{dictVarName}Map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.SS?.ToList() ?? new List<string>()) : " +
                               GenerateEmptyDictionary(propertyInfo.Type, keyType, valueType);
                    }
                    
                    // Special case: Dictionary<string, Dictionary<string, string>>  
                    if (valueType is INamedTypeSymbol namedValueType2 && 
                        namedValueType2.Name == "Dictionary" && 
                        namedValueType2.TypeArguments.Length == 2 &&
                        namedValueType2.TypeArguments[0].SpecialType == SpecialType.System_String &&
                        namedValueType2.TypeArguments[1].SpecialType == SpecialType.System_String)
                    {
                        return $"{recordVariableName}.TryGetMap(\"{memberName}\", out var {dictVarName}Map) && {dictVarName}Map != null ? " +
                               $"{dictVarName}Map.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.M?.ToDictionary(innerKvp => innerKvp.Key, innerKvp => innerKvp.Value.S ?? string.Empty) ?? new Dictionary<string, string>()) : " +
                               GenerateEmptyDictionary(propertyInfo.Type, keyType, valueType);
                    }
                    
                    // Fallback: return empty dictionary for unsupported complex value types
                    return GenerateEmptyDictionary(propertyInfo.Type, keyType, valueType);
                }
            }
            
            // Non-string keys not supported by DynamoDB
            return $"new Dictionary<{keyType.ToDisplayString()}, {valueType.ToDisplayString()}>()";
        }
        
        // Handle complex types (nested objects)
        var normalizedTypeName = propertyInfo.UnderlyingType.Name.Replace(".", "_").Replace("`", "_");
        var isNullable = propertyInfo.IsNullable;
        var varName = memberName.ToLowerInvariant();
        
        if (isNullable)
        {
            return $"{recordVariableName}.TryGetMap(\"{memberName}\", out var {varName}Map) && {varName}Map != null ? DynamoMapper.{normalizedTypeName}.FromDynamoRecord({varName}Map, {pkVariable}, {skVariable}) : null";
        }
        else
        {
            return $"{recordVariableName}.TryGetMap(\"{memberName}\", out var {varName}Map) && {varName}Map != null ? DynamoMapper.{normalizedTypeName}.FromDynamoRecord({varName}Map, {pkVariable}, {skVariable}) : MissingAttributeException.Throw<{typeName}>(\"{memberName}\", {pkVariable}, {skVariable})";
        }
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        // Complex types in keys typically use a string representation
        return $"model.{propertyInfo.Name}?.ToString() ?? \"\"";
    }
    
    public string? GenerateConditionalAssignment(PropertyInfo propertyInfo, string recordVariable)
    {
        // Complex types currently use null coalescing in GenerateToAttributeValue, not conditional assignment
        return null;
    }
    
    private static bool IsNumericType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Byte or SpecialType.System_SByte or
            SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or
            SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double => true,
            _ => false
        };
    }
    
    private string GetDictionaryValueParseLogic(ITypeSymbol valueType)
    {
        return valueType.SpecialType switch
        {
            SpecialType.System_Double => "kvp.Value.N != null ? double.Parse(kvp.Value.N) : 0.0",
            SpecialType.System_Int64 => "kvp.Value.N != null ? long.Parse(kvp.Value.N) : 0L",
            SpecialType.System_DateTime => "kvp.Value.S != null ? DateTime.Parse(kvp.Value.S) : default(DateTime)",
            _ when valueType.TypeKind == TypeKind.Enum => $"kvp.Value.S != null ? Enum.Parse<{valueType.ToDisplayString()}>(kvp.Value.S) : default({valueType.ToDisplayString()})",
            _ => GetComplexValueDefault(valueType)
        };
    }
    
    private string GetComplexValueDefault(ITypeSymbol valueType)
    {
        var typeDisplayString = valueType.ToDisplayString();
        
        // Handle collection types
        if (valueType is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.Name;
            if (typeName == "List" && namedType.TypeArguments.Length == 1)
            {
                var elementType = namedType.TypeArguments[0].ToDisplayString();
                return $"new List<{elementType}>()";
            }
            if ((typeName == "Dictionary" || typeName == "IDictionary") && namedType.TypeArguments.Length == 2)
            {
                var keyType = namedType.TypeArguments[0].ToDisplayString();
                var valueType2 = namedType.TypeArguments[1].ToDisplayString();
                return $"new Dictionary<{keyType}, {valueType2}>()";
            }
        }
        
        // For arrays
        if (valueType is IArrayTypeSymbol arrayType)
        {
            var elementType = arrayType.ElementType.ToDisplayString();
            return $"Array.Empty<{elementType}>()";
        }
        
        // For other complex types, try to create a new instance if it's a class
        if (valueType.TypeKind == TypeKind.Class && !valueType.IsAbstract)
        {
            return $"new {typeDisplayString}()";
        }
        
        // Fallback to default for value types and others
        return $"default({typeDisplayString})";
    }
    
    private string GenerateDictionaryConversion(ITypeSymbol targetType, string sourceExpression)
    {
        if (targetType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 2)
        {
            var keyType = namedType.TypeArguments[0].ToDisplayString();
            var valueType = namedType.TypeArguments[1].ToDisplayString();
            var typeName = namedType.Name;
            
            return typeName switch
            {
                "Dictionary" => $"({sourceExpression} ?? new Dictionary<{keyType}, {valueType}>())",
                "IDictionary" => $"({sourceExpression} ?? new Dictionary<{keyType}, {valueType}>())",
                "IReadOnlyDictionary" => $"({sourceExpression} ?? new Dictionary<{keyType}, {valueType}>())",
                _ => $"({sourceExpression} ?? new Dictionary<{keyType}, {valueType}>())"
            };
        }
        
        return "new Dictionary<string, object>()";
    }
    
    private string GenerateEmptyDictionary(ITypeSymbol targetType, ITypeSymbol keyType, ITypeSymbol valueType)
    {
        var keyTypeName = keyType.ToDisplayString();
        var valueTypeName = valueType.ToDisplayString();
        
        if (targetType is INamedTypeSymbol namedType)
        {
            var typeName = namedType.Name;
            return typeName switch
            {
                "Dictionary" => $"new Dictionary<{keyTypeName}, {valueTypeName}>()",
                "IDictionary" => $"new Dictionary<{keyTypeName}, {valueTypeName}>()",
                "IReadOnlyDictionary" => $"new Dictionary<{keyTypeName}, {valueTypeName}>()",
                _ => $"new Dictionary<{keyTypeName}, {valueTypeName}>()"
            };
        }
        
        return $"new Dictionary<{keyTypeName}, {valueTypeName}>()";
    }
    
    private string GeneratePrimitiveDictionary(string propertyName, ITypeSymbol valueType)
    {
        if (valueType.SpecialType == SpecialType.System_String)
        {
            return $"model.{propertyName} != null ? new AttributeValue {{ M = model.{propertyName}.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue {{ S = kvp.Value }}) }} : new AttributeValue {{ NULL = true }}";
        }
        else if (IsNumericType(valueType))
        {
            return $"model.{propertyName} != null ? new AttributeValue {{ M = model.{propertyName}.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue {{ N = kvp.Value.ToString() }}) }} : new AttributeValue {{ NULL = true }}";
        }
        else if (valueType.SpecialType == SpecialType.System_DateTime)
        {
            return $"model.{propertyName} != null ? new AttributeValue {{ M = model.{propertyName}.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue {{ S = kvp.Value.ToString(\"o\") }}) }} : new AttributeValue {{ NULL = true }}";
        }
        else if (valueType.TypeKind == TypeKind.Enum)
        {
            return $"model.{propertyName} != null ? new AttributeValue {{ M = model.{propertyName}.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue {{ S = kvp.Value.ToString() }}) }} : new AttributeValue {{ NULL = true }}";
        }
        
        // For unsupported types, return NULL
        return "new AttributeValue { NULL = true }";
    }
}