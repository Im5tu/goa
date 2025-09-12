using Goa.Clients.Dynamo.Generator.Models;
using Microsoft.CodeAnalysis;

namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Central registry for all type handlers.
/// Provides a plugin-based system for converting types to/from DynamoDB.
/// </summary>
public class TypeHandlerRegistry
{
    private readonly List<ITypeHandler> _handlers = new();
    private const int MaxRecursionDepth = 10; // Prevent infinite recursion
    
    public void RegisterHandler(ITypeHandler handler)
    {
        _handlers.Add(handler);
        
        // Set registry reference for composite handlers
        if (handler is ICompositeTypeHandler compositeHandler)
        {
            compositeHandler.SetRegistry(this);
        }
        
        // Keep handlers sorted by priority (highest first)
        _handlers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }
    
    /// <summary>
    /// Finds the best handler for the given property type.
    /// Returns null if no handler can process the type.
    /// </summary>
    public ITypeHandler? GetHandler(PropertyInfo propertyInfo)
    {
        return _handlers.FirstOrDefault(h => h.CanHandle(propertyInfo));
    }
    
    /// <summary>
    /// Checks if any registered handler can handle the given property type.
    /// </summary>
    public bool CanHandle(PropertyInfo propertyInfo)
    {
        return GetHandler(propertyInfo) != null;
    }
    
    /// <summary>
    /// Generates code to convert from model property to DynamoDB AttributeValue.
    /// Returns null for nullable properties that should use conditional assignment.
    /// </summary>
    public string? GenerateToAttributeValue(PropertyInfo propertyInfo)
    {
        var handler = GetHandler(propertyInfo);
        if (handler == null)
        {
            return string.Empty; // Skip unsupported types
        }
        
        return handler.GenerateToAttributeValue(propertyInfo);
    }
    
    /// <summary>
    /// Generates conditional assignment code for nullable properties.
    /// </summary>
    public string? GenerateConditionalAssignment(PropertyInfo propertyInfo, string recordVariable)
    {
        var handler = GetHandler(propertyInfo);
        return handler?.GenerateConditionalAssignment(propertyInfo, recordVariable);
    }
    
    /// <summary>
    /// Generates code to convert from DynamoDB record to model property value.
    /// </summary>
    public string GenerateFromDynamoRecord(PropertyInfo propertyInfo, string recordVariableName, string pkVariable, string skVariable)
    {
        var handler = GetHandler(propertyInfo);
        if (handler == null)
        {
            // Fallback for unsupported types
            return $"default({propertyInfo.Type.ToDisplayString()})";
        }
        
        return handler.GenerateFromDynamoRecord(propertyInfo, recordVariableName, pkVariable, skVariable);
    }
    
    /// <summary>
    /// Generates code for formatting the property value in key patterns (PK/SK).
    /// </summary>
    public string GenerateKeyFormatting(PropertyInfo propertyInfo)
    {
        var handler = GetHandler(propertyInfo);
        if (handler == null)
        {
            // Fallback: toString()
            return $"model.{propertyInfo.Name}?.ToString() ?? \"\"";
        }
        
        return handler.GenerateKeyFormatting(propertyInfo);
    }
    
    /// <summary>
    /// Recursively generates code to convert a type to DynamoDB AttributeValue.
    /// Used by composite handlers for nested type processing.
    /// </summary>
    public string GenerateNestedToAttributeValue(ITypeSymbol type, string valueExpression, int depth = 0)
    {
        if (depth > MaxRecursionDepth)
        {
            return "new AttributeValue { NULL = true }"; // Prevent infinite recursion
        }
        
        // Create a temporary PropertyInfo for the nested type
        var nestedProperty = CreatePropertyInfoForType(type);
        var handler = GetHandler(nestedProperty);
        
        if (handler == null)
        {
            return "new AttributeValue { NULL = true }";
        }
        
        // Replace the property access with the provided value expression
        var attributeValueCode = handler.GenerateToAttributeValue(nestedProperty);
        return attributeValueCode?.Replace($"model.{nestedProperty.Name}", valueExpression) ?? "new AttributeValue { NULL = true }";
    }
    
    /// <summary>
    /// Recursively generates code to convert from DynamoDB to a specific type.
    /// Used by composite handlers for nested type processing.
    /// </summary>
    public string GenerateNestedFromDynamoRecord(ITypeSymbol type, string recordExpression, string pkVariable, string skVariable, int depth = 0)
    {
        if (depth > MaxRecursionDepth)
        {
            return $"default({type.ToDisplayString()})"; // Prevent infinite recursion
        }
        
        // Create a temporary PropertyInfo for the nested type
        var nestedProperty = CreatePropertyInfoForType(type);
        var handler = GetHandler(nestedProperty);
        
        if (handler == null)
        {
            return $"default({type.ToDisplayString()})";
        }
        
        // Generate the conversion code 
        var conversionCode = handler.GenerateFromDynamoRecord(nestedProperty, recordExpression, pkVariable, skVariable);
        
        // Replace the synthetic property name with a generic key name for nested access
        return conversionCode.Replace($"\"{nestedProperty.Name}\"", "\"Value\"");
    }
    
    private PropertyInfo CreatePropertyInfoForType(ITypeSymbol type)
    {
        var isNullable = type.NullableAnnotation == NullableAnnotation.Annotated;
        
        return new PropertyInfo
        {
            Name = "TempProperty", // Synthetic name for nested processing
            Type = type,
            IsNullable = isNullable,
            IsCollection = IsCollectionType(type, out var elementType),
            IsDictionary = IsDictionaryType(type, out var keyType, out var valueType),
            ElementType = elementType,
            DictionaryTypes = IsDictionaryType(type, out keyType, out valueType) && keyType != null && valueType != null
                ? (keyType, valueType) 
                : null,
            Symbol = null!, // Not needed for synthetic properties
            Attributes = new List<AttributeInfo>() // No attributes for synthetic properties
        };
    }
    
    private static bool IsCollectionType(ITypeSymbol type, out ITypeSymbol? elementType)
    {
        elementType = null;
        
        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }
        
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeArgs = namedType.TypeArguments;
            if (typeArgs.Length == 1)
            {
                var typeName = namedType.Name;
                // Check common collection type names
                if (typeName == "List" || typeName == "IList" || 
                    typeName == "ICollection" || typeName == "IEnumerable" ||
                    typeName == "HashSet" || typeName == "ISet" ||
                    typeName == "IReadOnlyCollection" || typeName == "IReadOnlyList" || 
                    typeName == "IReadOnlySet" || typeName == "Collection")
                {
                    elementType = typeArgs[0];
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private static bool IsDictionaryType(ITypeSymbol type, out ITypeSymbol? keyType, out ITypeSymbol? valueType)
    {
        keyType = null;
        valueType = null;
        
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length == 2)
        {
            var typeName = namedType.Name;
            if (typeName == "Dictionary" || typeName == "IDictionary" || typeName == "IReadOnlyDictionary")
            {
                keyType = namedType.TypeArguments[0];
                valueType = namedType.TypeArguments[1];
                return true;
            }
        }
        
        return false;
    }
}