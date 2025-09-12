using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;
using Goa.Clients.Dynamo.Generator.TypeHandlers;

namespace Goa.Clients.Dynamo.Generator.CodeGeneration;

/// <summary>
/// Generates DynamoMapper classes for converting to/from DynamoDB records.
/// </summary>
public class MapperGenerator : ICodeGenerator
{
    private readonly TypeHandlerRegistry _typeHandlerRegistry;
    
    public MapperGenerator(TypeHandlerRegistry typeHandlerRegistry)
    {
        _typeHandlerRegistry = typeHandlerRegistry;
    }
    
    public string GenerateCode(IEnumerable<DynamoTypeInfo> types, GenerationContext context)
    {
        // Group types by namespace to generate separate files
        var typesByNamespace = types.GroupBy(t => t.Namespace).ToList();
        
        if (!typesByNamespace.Any())
            return string.Empty;
            
        // For now, generate for the first namespace. Later we can modify the generator
        // to return multiple files or handle this differently
        var firstNamespaceGroup = typesByNamespace.First();
        var targetNamespace = string.IsNullOrEmpty(firstNamespaceGroup.Key) ? "Generated" : firstNamespaceGroup.Key;
        
        var builder = new CodeBuilder();
        
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine("using Goa.Clients.Dynamo;");
        builder.AppendLine("using Goa.Clients.Dynamo.Models;");
        builder.AppendLine("using Goa.Clients.Dynamo.Exceptions;");
        builder.AppendLine("using Goa.Clients.Dynamo.Extensions;");
        builder.AppendLine();
        builder.AppendLine($"namespace {targetNamespace};");
        builder.AppendLine();
        builder.OpenBraceWithLine("public static class DynamoMapper");
        
        foreach (var type in firstNamespaceGroup)
        {
            GenerateTypeMapper(builder, type, context);
        }
        
        builder.CloseBrace();
        
        return builder.ToString();
    }
    
    private void GenerateTypeMapper(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        var normalizedTypeName = NamingHelpers.NormalizeTypeName(type.Name);
        var dynamoModelAttr = type.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault();
        
        builder.AppendLine();
        builder.OpenBraceWithLine($"public static class {normalizedTypeName}");
        
        // Generate ToDynamoRecord method
        GenerateToDynamoRecord(builder, type, context);
        
        // Check if type inherits [DynamoModel] behavior from base class
        var inheritsDynamoModel = HasInheritedDynamoModel(type);
        
        // Generate FromDynamoRecord methods if:
        // 1. Type has [DynamoModel] attribute (like BaseEntity), OR
        // 2. Type is abstract with concrete subtypes (like BaseDocument with Report/Invoice), OR  
        // 3. Type is a complex type referenced by DynamoDB types (like SecondaryEntity, ThirdEntity), OR
        // 4. Type inherits [DynamoModel] behavior from base class (like UserProfile from BaseEntity)
        var shouldGenerateFromDynamoRecord = dynamoModelAttr != null || 
                                           (type.IsAbstract && HasConcreteSubtypes(type, context)) ||
                                           (!type.IsAbstract && dynamoModelAttr == null && !inheritsDynamoModel) || // Complex referenced types only
                                           inheritsDynamoModel; // Types inheriting DynamoDB behavior
        
        if (shouldGenerateFromDynamoRecord)
        {
            GenerateFromDynamoRecord(builder, type, context);
        }
        
        builder.CloseBrace();
    }
    
    private void GenerateToDynamoRecord(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        var dynamoModelAttr = type.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault();
        
        // Check for inherited [DynamoModel] attribute if not found on current type
        if (dynamoModelAttr == null)
        {
            dynamoModelAttr = GetInheritedDynamoModelAttribute(type);
        }
        
        builder.AppendLine();
        builder.OpenBraceWithLine($"public static DynamoRecord ToDynamoRecord({type.FullName} model)");
        
        // For abstract types, use switch statement to delegate to concrete implementations
        if (type.IsAbstract && context.TypeRegistry.TryGetValue(type.FullName, out var concreteTypes))
        {
            GenerateAbstractToDynamoRecordDispatch(builder, type, concreteTypes, context);
        }
        else
        {
            // Generate regular record construction for concrete types
            GenerateConcreteToDynamoRecord(builder, type, dynamoModelAttr, context);
        }
        
        builder.CloseBrace();
    }
    
    private void GenerateAbstractToDynamoRecordDispatch(CodeBuilder builder, DynamoTypeInfo type, List<DynamoTypeInfo> concreteTypes, GenerationContext context)
    {
        builder.OpenBraceWithLine("return model switch");
        
        foreach (var concreteType in concreteTypes)
        {
            var normalizedTypeName = NamingHelpers.NormalizeTypeName(concreteType.Name);
            builder.AppendLine($"{concreteType.FullName} concrete => DynamoMapper.{normalizedTypeName}.ToDynamoRecord(concrete),");
        }
        
        builder.AppendLine($"_ => throw new InvalidOperationException($\"Unknown concrete type: {{model.GetType().FullName}} for abstract type {type.FullName}\")");
        builder.CloseBrace().Append(";");
    }
    
    private void GenerateConcreteToDynamoRecord(CodeBuilder builder, DynamoTypeInfo type, DynamoModelAttributeInfo? dynamoModelAttr, GenerationContext context)
    {
        builder.AppendLine("var record = new DynamoRecord();");
        
        // Add primary keys if this type has or inherits DynamoModel attribute
        if (dynamoModelAttr != null)
        {
            GeneratePrimaryKeyAssignment(builder, type, dynamoModelAttr);
        }
        
        // Add GSI keys
        var gsiAttributes = type.Attributes.OfType<GSIAttributeInfo>().ToList();
        foreach (var gsiAttr in gsiAttributes)
        {
            GenerateGSIKeyAssignment(builder, type, gsiAttr);
        }
        
        // Add type discriminator for inheritance
        if (type.IsAbstract || HasConcreteSubtypes(type, context))
        {
            builder.AppendLine($"record[\"Type\"] = new AttributeValue {{ S = \"{type.FullName}\" }};");
        }
        
        // Add property mappings
        GeneratePropertyMappings(builder, type);
        
        builder.AppendLine("return record;");
    }
    
    private void GeneratePrimaryKeyAssignment(CodeBuilder builder, DynamoTypeInfo type, DynamoModelAttributeInfo dynamoAttr)
    {
        var pkCode = GenerateKeyCode(type, dynamoAttr.PK);
        var skCode = GenerateKeyCode(type, dynamoAttr.SK);
        
        builder.AppendLine($"record[\"{dynamoAttr.PKName}\"] = new AttributeValue {{ S = {pkCode} }};");
        builder.AppendLine($"record[\"{dynamoAttr.SKName}\"] = new AttributeValue {{ S = {skCode} }};");
    }
    
    private void GenerateGSIKeyAssignment(CodeBuilder builder, DynamoTypeInfo type, GSIAttributeInfo gsiAttr)
    {
        // Skip GSI attributes that don't have assigned names (shouldn't happen after numbering)
        if (string.IsNullOrEmpty(gsiAttr.PKName) || string.IsNullOrEmpty(gsiAttr.SKName))
            return;
            
        var pkCode = GenerateKeyCode(type, gsiAttr.PK);
        var skCode = GenerateKeyCode(type, gsiAttr.SK);
        
        builder.AppendLine($"record[\"{gsiAttr.PKName}\"] = new AttributeValue {{ S = {pkCode} }};");
        builder.AppendLine($"record[\"{gsiAttr.SKName}\"] = new AttributeValue {{ S = {skCode} }};");
    }
    
    private string GenerateKeyCode(DynamoTypeInfo type, string pattern)
    {
        var placeholders = NamingHelpers.ExtractPlaceholders(pattern);
        if (!placeholders.Any())
        {
            return $"\"{pattern}\"";
        }
        
        var replacements = new Dictionary<string, string>();
        foreach (var placeholder in placeholders)
        {
            var property = FindProperty(type, placeholder);
            if (property != null)
            {
                var formatting = _typeHandlerRegistry.GenerateKeyFormatting(property);
                replacements[placeholder] = $"{{ {formatting} }}";
            }
            else
            {
                replacements[placeholder] = "{UnknownProperty}";
            }
        }
        
        var formattedPattern = NamingHelpers.FormatKeyPattern(pattern, replacements);
        return $"$\"{formattedPattern}\"";
    }
    
    private void GeneratePropertyMappings(CodeBuilder builder, DynamoTypeInfo type)
    {
        // Get all properties including inherited ones
        var allProperties = GetAllProperties(type);
        
        // Filter out properties that don't have handlers and aren't ignored for writing
        var supportedProperties = allProperties
            .Where(p => _typeHandlerRegistry.CanHandle(p) && !p.IsIgnored(IgnoreDirection.WhenWriting));
        
        foreach (var property in supportedProperties)
        {
            var attributeValueCode = _typeHandlerRegistry.GenerateToAttributeValue(property);
            if (!string.IsNullOrEmpty(attributeValueCode))
            {
                // Non-nullable property - use direct assignment
                builder.AppendLine($"record[\"{property.GetDynamoAttributeName()}\"] = {attributeValueCode};");
            }
            else
            {
                // Nullable property - use conditional assignment for sparse GSI compatibility
                var conditionalCode = _typeHandlerRegistry.GenerateConditionalAssignment(property, "record");
                if (conditionalCode != null && !string.IsNullOrEmpty(conditionalCode))
                {
                    builder.AppendLine(conditionalCode);
                }
            }
        }
    }
    
    private List<PropertyInfo> GetAllProperties(DynamoTypeInfo type)
    {
        var properties = new List<PropertyInfo>();
        var current = type;
        
        // Walk up the inheritance chain and collect all properties
        while (current != null)
        {
            properties.AddRange(current.Properties);
            current = current.BaseType;
        }
        
        // Remove duplicates (in case of property hiding/overriding)
        // Keep the most derived version
        var uniqueProperties = new Dictionary<string, PropertyInfo>();
        foreach (var prop in properties)
        {
            if (!uniqueProperties.ContainsKey(prop.Name))
            {
                uniqueProperties[prop.Name] = prop;
            }
        }
        
        return uniqueProperties.Values.ToList();
    }
    
    private void GenerateFromDynamoRecord(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        var dynamoModelAttr = type.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault();
        
        // Check for inherited [DynamoModel] attribute if not found on current type
        if (dynamoModelAttr == null)
        {
            dynamoModelAttr = GetInheritedDynamoModelAttribute(type);
        }
        
        // Generate single FromDynamoRecord method with default parameters
        builder.AppendLine();
        builder.OpenBraceWithLine($"public static {type.FullName} FromDynamoRecord(DynamoRecord record, string? parentPkValue = null, string? parentSkValue = null)");
        
        if (dynamoModelAttr != null)
        {
            builder.AppendLine($"var pkValue = record.TryGetNullableString(\"{dynamoModelAttr.PKName}\", out var pk) ? pk : parentPkValue ?? string.Empty;");
            builder.AppendLine($"var skValue = record.TryGetNullableString(\"{dynamoModelAttr.SKName}\", out var sk) ? sk : parentSkValue ?? string.Empty;");
        }
        else
        {
            builder.AppendLine("var pkValue = parentPkValue ?? string.Empty;");
            builder.AppendLine("var skValue = parentSkValue ?? string.Empty;");
        }
        
        if (type.IsAbstract && HasConcreteSubtypes(type, context))
        {
            GenerateAbstractTypeDispatch(builder, type, context);
        }
        else
        {
            GenerateConcreteTypeConstruction(builder, type);
        }
        
        builder.CloseBrace();
    }
    
    private void GenerateAbstractTypeDispatch(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        builder.AppendLine("if (!record.TryGetNullableString(\"Type\", out var typeValue) || typeValue == null)");
        builder.Indent().AppendLine($"throw new InvalidOperationException(\"Missing Type discriminator for abstract type {type.FullName}\");").Unindent();
        builder.AppendLine();
        builder.OpenBraceWithLine("return typeValue switch");
        
        if (context.TypeRegistry.TryGetValue(type.FullName, out var concreteTypes))
        {
            foreach (var concreteType in concreteTypes)
            {
                var normalizedTypeName = NamingHelpers.NormalizeTypeName(concreteType.Name);
                builder.AppendLine($"\"{concreteType.FullName}\" => DynamoMapper.{normalizedTypeName}.FromDynamoRecord(record, parentPkValue, parentSkValue),");
            }
        }
        
        builder.AppendLine($"_ => throw new InvalidOperationException($\"Unknown type: {{typeValue}} for abstract type {type.FullName}\")");
        builder.CloseBrace().Append(";");
    }
    
    private void GenerateConcreteTypeConstruction(CodeBuilder builder, DynamoTypeInfo type)
    {
        // Find the best constructor
        var constructor = FindBestConstructor(type);
        if (constructor == null)
        {
            GenerateParameterlessConstruction(builder, type);
            return;
        }
        
        // Check if we need mixed construction (constructor + property setters)
        var constructorParams = new HashSet<string>(constructor.Parameters.Select(p => p.Name));
        
        // Get all properties including inherited ones, only supported ones that aren't ignored for reading
        var allProperties = GetAllProperties(type)
            .Where(p => _typeHandlerRegistry.CanHandle(p) && !p.IsIgnored(IgnoreDirection.WhenReading))
            .ToList();
        var additionalProperties = allProperties.Where(p => !constructorParams.Contains(p.Name)).ToList();
        
        if (additionalProperties.Any())
        {
            GenerateMixedConstruction(builder, type, constructor, additionalProperties);
        }
        else
        {
            GenerateConstructorOnlyConstruction(builder, type, constructor);
        }
    }
    
    private void GenerateConstructorOnlyConstruction(CodeBuilder builder, DynamoTypeInfo type, IMethodSymbol constructor)
    {
        var parameterCode = new List<string>();
        
        foreach (var param in constructor.Parameters)
        {
            // Generate conversion directly from parameter information (like the original generator)
            var conversionCode = GenerateParameterConversion(param, "record", "pkValue", "skValue", type);
            parameterCode.Add(conversionCode);
        }
        
        builder.AppendLine($"return new {type.FullName}(");
        builder.Indent();
        for (int i = 0; i < parameterCode.Count; i++)
        {
            var comma = i < parameterCode.Count - 1 ? "," : "";
            builder.AppendLine($"{parameterCode[i]}{comma}");
        }
        builder.Unindent();
        builder.AppendLine(");");
    }
    
    private void GenerateMixedConstruction(CodeBuilder builder, DynamoTypeInfo type, IMethodSymbol constructor, List<PropertyInfo> additionalProperties)
    {
        var parameterCode = new List<string>();
        
        foreach (var param in constructor.Parameters)
        {
            // Generate conversion directly from parameter information (like the original generator)
            var conversionCode = GenerateParameterConversion(param, "record", "pkValue", "skValue", type);
            parameterCode.Add(conversionCode);
        }
        
        builder.AppendLine($"return new {type.FullName}(");
        builder.Indent();
        for (int i = 0; i < parameterCode.Count; i++)
        {
            var comma = i < parameterCode.Count - 1 ? "," : "";
            builder.AppendLine($"{parameterCode[i]}{comma}");
        }
        builder.Unindent();
        builder.AppendLine(")");
        builder.AppendLine("{");
        builder.Indent();
        
        // Set additional properties using object initializer syntax, but only supported ones
        var settableProperties = additionalProperties
            .Where(p => p.Symbol.SetMethod != null && _typeHandlerRegistry.CanHandle(p))
            .ToList();
        foreach (var prop in settableProperties)
        {
            var conversionCode = GeneratePropertyConversion(prop, "record", "pkValue", "skValue");
            builder.AppendLine($"{prop.Name} = {conversionCode},");
        }
        
        builder.Unindent();
        builder.AppendLine("};");
    }
    
    private void GenerateParameterlessConstruction(CodeBuilder builder, DynamoTypeInfo type)
    {
        // Always use object initializer syntax (like the original generator)
        builder.AppendLine($"return new {type.FullName}()");
        builder.AppendLine("{");
        builder.Indent();
        
        // Get all properties including inherited ones, but only include those that have setters, are supported, and aren't ignored for reading
        var allProperties = GetAllProperties(type);
        var settableProperties = allProperties
            .Where(p => p.Symbol.SetMethod != null && _typeHandlerRegistry.CanHandle(p) && !p.IsIgnored(IgnoreDirection.WhenReading))
            .ToList();
        
        for (int i = 0; i < settableProperties.Count; i++)
        {
            var prop = settableProperties[i];
            var conversionCode = GeneratePropertyConversion(prop, "record", "pkValue", "skValue");
            builder.AppendLine($"{prop.Name} = {conversionCode},");
        }
        
        builder.Unindent();
        builder.AppendLine("};");
    }
    
    private string GeneratePropertyConversion(PropertyInfo property, string recordVar, string pkVar, string skVar)
    {
        return _typeHandlerRegistry.GenerateFromDynamoRecord(property, recordVar, pkVar, skVar);
    }
    
    private string GenerateParameterConversion(IParameterSymbol param, string recordVar, string pkVar, string skVar, DynamoTypeInfo type)
    {
        var isNullable = param.NullableAnnotation == NullableAnnotation.Annotated;
        
        // Special handling for PK/SK parameters - reuse already extracted values (like the original generator)
        if (param.Name == "PK" || param.Name == "Pk")
        {
            return isNullable 
                ? $"string.IsNullOrEmpty({pkVar}) ? null : {pkVar}"
                : $"string.IsNullOrEmpty({pkVar}) ? MissingAttributeException.Throw<string>(\"{param.Name}\", {pkVar}, {skVar}) : {pkVar}";
        }
        if (param.Name == "SK" || param.Name == "Sk")
        {
            return isNullable 
                ? $"string.IsNullOrEmpty({skVar}) ? null : {skVar}"
                : $"string.IsNullOrEmpty({skVar}) ? MissingAttributeException.Throw<string>(\"{param.Name}\", {pkVar}, {skVar}) : {skVar}";
        }
        
        // For all other parameters, use the type handler system
        // For record constructor parameters, we need to find the corresponding property to get its attributes
        // Use case-insensitive lookup since param names might be camelCase but properties are PascalCase
        var property = FindPropertyIgnoreCase(type, param.Name);
        var attributes = property?.Attributes ?? new List<AttributeInfo>();
        
        // Use the serialized name if available, otherwise the original property name (PascalCase) for the DynamoDB attribute lookup
        var attributeName = property?.GetDynamoAttributeName() ?? param.Name;
        
        var tempPropertyInfo = new PropertyInfo
        {
            Name = attributeName,
            Type = param.Type,
            IsNullable = isNullable,
            IsCollection = IsCollectionType(param.Type, out var elementType),
            IsDictionary = IsDictionaryType(param.Type, out var keyType, out var valueType),
            ElementType = elementType,
            DictionaryTypes = IsDictionaryType(param.Type, out keyType, out valueType) ? (keyType, valueType) : null,
            Symbol = null!, // Not needed for conversion
            Attributes = attributes // Use attributes from the corresponding property if it exists
        };
        
        return _typeHandlerRegistry.GenerateFromDynamoRecord(tempPropertyInfo, recordVar, pkVar, skVar);
    }
    
    private static bool IsCollectionType(ITypeSymbol type, out ITypeSymbol elementType)
    {
        elementType = null!;
        
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
                
                // Also check by full type name for more accuracy
                var fullName = namedType.ToDisplayString();
                if (fullName.StartsWith("System.Collections.Generic.IReadOnlyCollection<") ||
                    fullName.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
                    fullName.StartsWith("System.Collections.Generic.IReadOnlySet<") ||
                    fullName.StartsWith("System.Collections.ObjectModel.Collection<"))
                {
                    elementType = typeArgs[0];
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private static bool IsDictionaryType(ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType)
    {
        keyType = null!;
        valueType = null!;
        
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
    
    private IMethodSymbol? FindBestConstructor(DynamoTypeInfo type)
    {
        var constructors = type.Symbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            .ToList();
        
        // Prefer constructor with parameters
        var paramConstructor = constructors.FirstOrDefault(c => c.Parameters.Any());
        if (paramConstructor != null)
            return paramConstructor;
        
        // Fallback to parameterless constructor
        return constructors.FirstOrDefault(c => !c.Parameters.Any());
    }
    
    private bool HasConcreteSubtypes(DynamoTypeInfo type, GenerationContext context)
    {
        return context.TypeRegistry.ContainsKey(type.FullName);
    }
    
    private bool HasInheritedDynamoModel(DynamoTypeInfo type)
    {
        // Check if any base class has [DynamoModel] attribute
        var current = type.BaseType;
        while (current != null)
        {
            if (current.Attributes.Any(a => a is DynamoModelAttributeInfo))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }
    
    private DynamoModelAttributeInfo? GetInheritedDynamoModelAttribute(DynamoTypeInfo type)
    {
        // Find the first base class with [DynamoModel] attribute
        var current = type.BaseType;
        while (current != null)
        {
            var dynamoModelAttr = current.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault();
            if (dynamoModelAttr != null)
            {
                return dynamoModelAttr;
            }
            current = current.BaseType;
        }
        return null;
    }
    
    private PropertyInfo? FindProperty(DynamoTypeInfo type, string propertyName)
    {
        // Search in current type and base types
        var current = type;
        while (current != null)
        {
            var property = current.Properties.FirstOrDefault(p => p.Name == propertyName);
            if (property != null)
                return property;
                
            current = current.BaseType;
        }
        
        return null;
    }
    
    private PropertyInfo? FindPropertyIgnoreCase(DynamoTypeInfo type, string propertyName)
    {
        // Search in current type and base types with case-insensitive comparison
        var current = type;
        while (current != null)
        {
            var property = current.Properties.FirstOrDefault(p => 
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            if (property != null)
                return property;
                
            current = current.BaseType;
        }
        
        return null;
    }
}