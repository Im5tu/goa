using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Handles collection types: arrays, lists, sets, etc.
/// Supports composition for complex element types through the registry.
/// </summary>
public class CollectionTypeHandler : ICompositeTypeHandler
{
    private TypeHandlerRegistry? _registry;
    
    public int Priority => 110; // Higher than primitive
    
    public void SetRegistry(TypeHandlerRegistry registry)
    {
        _registry = registry;
    }
    
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        return propertyInfo.IsCollection;
    }
    
    public string GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var propertyName = propertyInfo.Name;
        
        if (propertyInfo.ElementType == null || _registry == null)
        {
            return "new AttributeValue { NULL = true }"; // Skip invalid collections
        }
        
        var elementType = propertyInfo.ElementType;
        
        // Handle simple primitive types directly for performance
        var primitiveResult = TryGeneratePrimitiveCollection(propertyName, elementType);
        if (primitiveResult != null)
        {
            return primitiveResult;
        }
        
        // For complex element types, we need to handle them differently
        // For now, fallback to NULL for unsupported complex collections
        return "new AttributeValue { NULL = true }";
    }
    
    private string? TryGeneratePrimitiveCollection(string propertyName, ITypeSymbol elementType)
    {
        return elementType.SpecialType switch
        {
            SpecialType.System_String => $"new AttributeValue {{ SS = model.{propertyName}?.ToList() ?? new List<string>() }}",
            SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or
            SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double => $"new AttributeValue {{ NS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}",
            SpecialType.System_Boolean => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}",
            SpecialType.System_DateTime => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString(\"o\")).ToList() ?? new List<string>() }}",
            _ when elementType.Name == nameof(Guid) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}",
            _ when elementType.Name == nameof(TimeSpan) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}",
            _ when elementType.Name == nameof(DateTimeOffset) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString(\"o\")).ToList() ?? new List<string>() }}",
            _ when elementType.TypeKind == TypeKind.Enum => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}",
            _ => null // Use composition for complex types
        };
    }
    
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var memberName = propertyInfo.Name;
        var collectionType = propertyInfo.Type.ToDisplayString();
        
        if (propertyInfo.ElementType == null || _registry == null)
        {
            return $"default({collectionType})";
        }
        
        var elementType = propertyInfo.ElementType;
        var elementTypeName = elementType.ToDisplayString();
        
        // Try primitive extraction first for performance
        var primitiveExtraction = TryGetPrimitiveCollectionExtraction(memberName, elementType, recordVariableName);
        if (primitiveExtraction != null)
        {
            var conversion = ConvertToTargetCollectionType(propertyInfo.Type, elementType, primitiveExtraction);
            return conversion;
        }
        
        // For complex element types, use default empty collection for now
        // TODO: Implement full composition support for complex nested collections
        return GenerateDefaultCollection(propertyInfo.Type, elementType);
    }
    
    private string? TryGetPrimitiveCollectionExtraction(string memberName, ITypeSymbol elementType, string recordVariableName)
    {
        var varName = memberName.ToLowerInvariant();
        
        return elementType.SpecialType switch
        {
            SpecialType.System_String => $"({recordVariableName}.TryGetStringSet(\"{memberName}\", out var {varName}) ? {varName} : null)",
            // For byte, sbyte, short, ushort, uint, ulong, decimal, float - we need to use int/long/double and convert
            SpecialType.System_Byte => $"({recordVariableName}.TryGetIntSet(\"{memberName}\", out var {varName}) ? {varName}.Select(x => (byte)x) : null)",
            SpecialType.System_SByte => $"({recordVariableName}.TryGetIntSet(\"{memberName}\", out var {varName}) ? {varName}.Select(x => (sbyte)x) : null)",
            SpecialType.System_Int16 => $"({recordVariableName}.TryGetIntSet(\"{memberName}\", out var {varName}) ? {varName}.Select(x => (short)x) : null)",
            SpecialType.System_UInt16 => $"({recordVariableName}.TryGetIntSet(\"{memberName}\", out var {varName}) ? {varName}.Select(x => (ushort)x) : null)",
            SpecialType.System_Int32 => $"({recordVariableName}.TryGetIntSet(\"{memberName}\", out var {varName}) ? {varName} : null)",
            SpecialType.System_UInt32 => $"({recordVariableName}.TryGetLongSet(\"{memberName}\", out var {varName}) ? {varName}.Select(x => (uint)x) : null)",
            SpecialType.System_Int64 => $"({recordVariableName}.TryGetLongSet(\"{memberName}\", out var {varName}) ? {varName} : null)",
            SpecialType.System_UInt64 => $"({recordVariableName}.TryGetLongSet(\"{memberName}\", out var {varName}) ? {varName}.Select(x => (ulong)x) : null)",
            SpecialType.System_Decimal => $"({recordVariableName}.TryGetDoubleSet(\"{memberName}\", out var {varName}) ? {varName}.Select(x => (decimal)x) : null)",
            SpecialType.System_Single => $"({recordVariableName}.TryGetDoubleSet(\"{memberName}\", out var {varName}) ? {varName}.Select(x => (float)x) : null)",
            SpecialType.System_Double => $"({recordVariableName}.TryGetDoubleSet(\"{memberName}\", out var {varName}) ? {varName} : null)",
            SpecialType.System_Boolean => $"({recordVariableName}.TryGetStringSet(\"{memberName}\", out var {varName}Strs) ? {varName}Strs.Select(bool.Parse) : null)",
            SpecialType.System_DateTime => $"({recordVariableName}.TryGetDateTimeSet(\"{memberName}\", out var {varName}) ? {varName} : null)",
            _ when elementType.Name == nameof(Guid) => $"({recordVariableName}.TryGetStringSet(\"{memberName}\", out var {varName}Strs) ? {varName}Strs.Select(Guid.Parse) : null)",
            _ when elementType.Name == nameof(TimeSpan) => $"({recordVariableName}.TryGetStringSet(\"{memberName}\", out var {varName}Strs) ? {varName}Strs.Select(TimeSpan.Parse) : null)",
            _ when elementType.Name == nameof(DateTimeOffset) => $"({recordVariableName}.TryGetStringSet(\"{memberName}\", out var {varName}Strs) ? {varName}Strs.Select(DateTimeOffset.Parse) : null)",
            _ when elementType.TypeKind == TypeKind.Enum => $"({recordVariableName}.TryGetEnumSet<{elementType.ToDisplayString()}>(\"{memberName}\", out var {varName}) ? {varName} : null)",
            _ => null // Return null for unsupported element types
        };
    }
    
    private string ConvertToTargetCollectionType(ITypeSymbol targetType, ITypeSymbol elementType, string sourceExpression)
    {
        var elementTypeName = elementType.ToDisplayString();
        
        if (targetType is IArrayTypeSymbol)
        {
            return $"({sourceExpression}?.ToArray() ?? Array.Empty<{elementTypeName}>())";
        }
        
        if (targetType is INamedTypeSymbol namedType)
        {
            var typeName = namedType.Name;
            var fullName = namedType.ToDisplayString();
            
            // Handle interfaces and concrete types
            return typeName switch
            {
                "List" => $"({sourceExpression}?.ToList() ?? new List<{elementTypeName}>())",
                "IList" => $"({sourceExpression}?.ToList() ?? new List<{elementTypeName}>())",
                "ICollection" => $"({sourceExpression}?.ToList() ?? new List<{elementTypeName}>())",
                "HashSet" => $"new HashSet<{elementTypeName}>({sourceExpression} ?? Enumerable.Empty<{elementTypeName}>())",
                "ISet" => $"new HashSet<{elementTypeName}>({sourceExpression} ?? Enumerable.Empty<{elementTypeName}>())",
                "IReadOnlyCollection" => $"({sourceExpression}?.ToList() ?? new List<{elementTypeName}>())",
                "IReadOnlyList" => $"({sourceExpression}?.ToList() ?? new List<{elementTypeName}>())",
                "IReadOnlySet" => $"new HashSet<{elementTypeName}>({sourceExpression} ?? Enumerable.Empty<{elementTypeName}>())",
                "Collection" => $"new System.Collections.ObjectModel.Collection<{elementTypeName}>({sourceExpression}?.ToList() ?? new List<{elementTypeName}>())",
                "IEnumerable" => $"({sourceExpression} ?? Enumerable.Empty<{elementTypeName}>())",
                _ => $"({sourceExpression}?.ToList() ?? new List<{elementTypeName}>())"
            };
        }
        
        return $"({sourceExpression} ?? Enumerable.Empty<{elementTypeName}>())";
    }
    
    private string GenerateDefaultCollection(ITypeSymbol targetType, ITypeSymbol elementType)
    {
        var elementTypeName = elementType.ToDisplayString();
        
        if (targetType is IArrayTypeSymbol)
        {
            return $"Array.Empty<{elementTypeName}>()";
        }
        
        if (targetType is INamedTypeSymbol namedType)
        {
            var typeName = namedType.Name;
            
            // Handle interfaces and concrete types
            return typeName switch
            {
                "List" => $"new List<{elementTypeName}>()",
                "IList" => $"new List<{elementTypeName}>()",
                "ICollection" => $"new List<{elementTypeName}>()",
                "HashSet" => $"new HashSet<{elementTypeName}>()",
                "ISet" => $"new HashSet<{elementTypeName}>()",
                "IReadOnlyCollection" => $"new List<{elementTypeName}>()",
                "IReadOnlyList" => $"new List<{elementTypeName}>()",
                "IReadOnlySet" => $"new HashSet<{elementTypeName}>()",
                "Collection" => $"new System.Collections.ObjectModel.Collection<{elementTypeName}>()",
                "IEnumerable" => $"Enumerable.Empty<{elementTypeName}>()",
                _ => $"new List<{elementTypeName}>()"
            };
        }
        
        return $"Enumerable.Empty<{elementTypeName}>()";
    }
    
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        // Collections can't be used directly in keys - convert to string representation
        return $"string.Join(\",\", model.{propertyInfo.Name} ?? Enumerable.Empty<object>())";
    }
}