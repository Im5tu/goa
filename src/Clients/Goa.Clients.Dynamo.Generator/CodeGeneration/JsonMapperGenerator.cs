using Microsoft.CodeAnalysis;
using Goa.Clients.Dynamo.Generator.Models;
using Goa.Clients.Dynamo.Generator.TypeHandlers;

namespace Goa.Clients.Dynamo.Generator.CodeGeneration;

/// <summary>
/// Generates DynamoJsonMapper classes for directly reading/writing DynamoDB JSON wire format
/// to/from entities, bypassing intermediate DynamoRecord allocations.
/// </summary>
public class JsonMapperGenerator : ICodeGenerator
{
    private readonly TypeHandlerRegistry _typeHandlerRegistry;
    private int _variableCounter;

    public JsonMapperGenerator(TypeHandlerRegistry typeHandlerRegistry)
    {
        _typeHandlerRegistry = typeHandlerRegistry;
    }

    public string GenerateCode(IEnumerable<DynamoTypeInfo> types, GenerationContext context)
    {
        var typesByNamespace = types.GroupBy(t => t.Namespace).Where(x => x.Any()).ToList();

        if (!typesByNamespace.Any())
            return string.Empty;

        var builder = new CodeBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Buffers.Text;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using System.Globalization;");
        builder.AppendLine("using System.Text.Json;");

        foreach (var ns in typesByNamespace)
        {
            var targetNamespace = string.IsNullOrEmpty(ns.Key) ? "Generated" : ns.Key;
            builder.AppendLine();
            builder.AppendLine($"namespace {targetNamespace}");
            builder.OpenBrace();
            {
                builder.OpenBraceWithLine("public static class DynamoJsonMapper");

                foreach (var type in ns)
                {
                    builder.AppendLine($"// {type.FullName}");
                    GenerateTypeMapper(builder, type, context);
                }

                builder.CloseBrace();
            }
            builder.CloseBrace();
        }

        return builder.ToString();
    }

    private void GenerateTypeMapper(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        var dynamoModelAttr = type.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault();

        // Skip abstract types that don't have concrete subtypes and don't have [DynamoModel] directly
        if (type.IsAbstract && !HasConcreteSubtypes(type, context) && dynamoModelAttr == null)
            return;

        var normalizedTypeName = NamingHelpers.NormalizeTypeName(type.Name);

        builder.AppendLine();
        builder.OpenBraceWithLine($"public static class {normalizedTypeName}");

        GenerateWriteToJson(builder, type, context);
        GenerateReadFromJson(builder, type, context);

        builder.CloseBrace();
    }

    // ─── WriteToJson ────────────────────────────────────────────────────

    private void GenerateWriteToJson(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        builder.AppendLine();
        builder.OpenBraceWithLine($"public static void WriteToJson(System.Text.Json.Utf8JsonWriter writer, {type.FullName} model)");

        if (type.IsAbstract && HasConcreteSubtypes(type, context))
        {
            GenerateAbstractWriteDispatch(builder, type, context);
        }
        else
        {
            GenerateConcreteWriteToJson(builder, type, context);
        }

        builder.CloseBrace();
    }

    private void GenerateAbstractWriteDispatch(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        builder.OpenBraceWithLine("switch (model)");

        if (context.TypeRegistry.TryGetValue(type.FullName, out var concreteTypes))
        {
            foreach (var concreteType in concreteTypes)
            {
                var normalizedTypeName = NamingHelpers.NormalizeTypeName(concreteType.Name);
                builder.AppendLine($"case {concreteType.FullName} concrete:");
                builder.Indent();
                builder.AppendLine($"DynamoJsonMapper.{normalizedTypeName}.WriteToJson(writer, concrete);");
                builder.AppendLine("return;");
                builder.Unindent();
            }
        }

        builder.AppendLine("default:");
        builder.Indent();
        builder.AppendLine($"throw new InvalidOperationException($\"Unknown concrete type: {{model.GetType().FullName}} for abstract type {type.FullName}\");");
        builder.Unindent();
        builder.CloseBrace();
    }

    private void GenerateConcreteWriteToJson(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        builder.AppendLine("writer.WriteStartObject();");

        // Add type discriminator for inheritance
        var needsDiscriminator = type.IsAbstract || HasConcreteSubtypes(type, context) || InheritsFromAbstractType(type);
        if (needsDiscriminator)
        {
            var dynamoModelAttr = type.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault()
                                  ?? GetInheritedDynamoModelAttribute(type);
            var typeNameField = (dynamoModelAttr?.TypeName != "Type" ? dynamoModelAttr?.TypeName : null)
                                ?? GetInheritedDynamoModelAttribute(type)?.TypeName
                                ?? "Type";

            builder.AppendLine($"writer.WritePropertyName(\"{typeNameField}\");");
            builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", \"{type.FullName}\"); writer.WriteEndObject();");
        }

        var allProperties = GetAllProperties(type);
        var supportedProperties = allProperties
            .Where(p => (p.ConverterTypeName != null || _typeHandlerRegistry.CanHandle(p)) && !p.IsIgnored(IgnoreDirection.WhenWriting));

        foreach (var property in supportedProperties)
        {
            var attrName = property.GetDynamoAttributeName();
            GenerateWriteProperty(builder, property, attrName, "model");
        }

        builder.AppendLine("writer.WriteEndObject();");
    }

    private void GenerateWriteProperty(CodeBuilder builder, PropertyInfo property, string attrName, string modelVar)
    {
        var accessExpr = $"{modelVar}.{property.Name}";
        var underlyingType = property.UnderlyingType;
        var isNullable = property.IsNullable;

        // Custom converter handling - delegate entirely to the converter
        if (property.ConverterTypeName != null)
        {
            builder.AppendLine($"writer.WritePropertyName(\"{attrName}\");");
            builder.AppendLine($"new {property.ConverterTypeName}().Write(writer, {accessExpr});");
            return;
        }

        // Check for [UnixTimestamp] attribute
        var hasUnixTimestamp = property.Attributes.Any(a => a is UnixTimestampAttributeInfo);

        // Dictionary handling
        if (property.IsDictionary && property.DictionaryTypes.HasValue)
        {
            GenerateWriteDictionary(builder, property, attrName, modelVar);
            return;
        }

        // Collection handling
        if (property.IsCollection && property.ElementType != null)
        {
            GenerateWriteCollection(builder, property, attrName, modelVar);
            return;
        }

        // Complex type handling (non-primitive, non-collection)
        if (IsComplexType(underlyingType))
        {
            GenerateWriteComplexType(builder, property, attrName, modelVar);
            return;
        }

        // Primitive handling
        builder.AppendLine($"writer.WritePropertyName(\"{attrName}\");");

        if (isNullable)
        {
            var nullCheck = $"{accessExpr} != null";

            builder.OpenBraceWithLine($"if ({nullCheck})");
            EmitPrimitiveTypeWrapper(builder, property, isNullable, accessExpr, hasUnixTimestamp);
            builder.CloseBrace();
            builder.OpenBraceWithLine("else");
            builder.AppendLine("writer.WriteStartObject(); writer.WriteBoolean(\"NULL\", true); writer.WriteEndObject();");
            builder.CloseBrace();
        }
        else
        {
            // Non-nullable strings are reference types that could still be null at runtime
            if (underlyingType.SpecialType == SpecialType.System_String)
            {
                builder.OpenBraceWithLine($"if ({accessExpr} != null)");
                EmitPrimitiveTypeWrapper(builder, property, isNullable, accessExpr, hasUnixTimestamp);
                builder.CloseBrace();
                builder.OpenBraceWithLine("else");
                builder.AppendLine("writer.WriteStartObject(); writer.WriteBoolean(\"NULL\", true); writer.WriteEndObject();");
                builder.CloseBrace();
            }
            else
            {
                EmitPrimitiveTypeWrapper(builder, property, isNullable, accessExpr, hasUnixTimestamp);
            }
        }
    }

    private void EmitPrimitiveTypeWrapper(CodeBuilder builder, PropertyInfo property, bool isNullable, string accessExpr, bool hasUnixTimestamp)
    {
        var underlyingType = property.UnderlyingType;
        var valueAccess = isNullable && underlyingType.SpecialType != SpecialType.System_String ? $"{accessExpr}.Value" : accessExpr;

        // UnixTimestamp DateTime/DateTimeOffset -> N
        if (hasUnixTimestamp)
        {
            var unixAttr = property.Attributes.OfType<UnixTimestampAttributeInfo>().First();
            var isMilliseconds = unixAttr.Format == UnixTimestampFormat.Milliseconds;
            var method = isMilliseconds ? "ToUnixTimeMilliseconds" : "ToUnixTimeSeconds";
            builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"N\", ((DateTimeOffset){valueAccess}).{method}().ToString()); writer.WriteEndObject();");
            return;
        }

        switch (underlyingType.SpecialType)
        {
            case SpecialType.System_String:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}); writer.WriteEndObject();");
                break;

            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"N\", {valueAccess}.ToString(CultureInfo.InvariantCulture)); writer.WriteEndObject();");
                break;

            case SpecialType.System_Boolean:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteBoolean(\"BOOL\", {valueAccess}); writer.WriteEndObject();");
                break;

            case SpecialType.System_Char:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString()); writer.WriteEndObject();");
                break;

            case SpecialType.System_DateTime:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString(\"o\")); writer.WriteEndObject();");
                break;

            default:
                if (underlyingType.Name == "DateTimeOffset")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString(\"o\")); writer.WriteEndObject();");
                else if (underlyingType.Name == "Guid")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString()); writer.WriteEndObject();");
                else if (underlyingType.Name == "TimeSpan")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString()); writer.WriteEndObject();");
                else if (underlyingType.Name == "DateOnly")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString(\"yyyy-MM-dd\")); writer.WriteEndObject();");
                else if (underlyingType.Name == "TimeOnly")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString(\"HH:mm:ss.fffffff\")); writer.WriteEndObject();");
                else if (underlyingType.TypeKind == TypeKind.Enum)
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString()); writer.WriteEndObject();");
                else
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {valueAccess}.ToString()); writer.WriteEndObject();");
                break;
        }
    }

    private void GenerateWriteComplexType(CodeBuilder builder, PropertyInfo property, string attrName, string modelVar)
    {
        var accessExpr = $"{modelVar}.{property.Name}";
        var normalizedTypeName = NamingHelpers.NormalizeTypeName(property.UnderlyingType.Name);

        builder.AppendLine($"writer.WritePropertyName(\"{attrName}\");");
        builder.OpenBraceWithLine($"if ({accessExpr} != null)");
        builder.AppendLine("writer.WriteStartObject(); writer.WritePropertyName(\"M\");");
        builder.AppendLine($"DynamoJsonMapper.{normalizedTypeName}.WriteToJson(writer, {accessExpr});");
        builder.AppendLine("writer.WriteEndObject();");
        builder.CloseBrace();
        builder.OpenBraceWithLine("else");
        builder.AppendLine("writer.WriteStartObject(); writer.WriteBoolean(\"NULL\", true); writer.WriteEndObject();");
        builder.CloseBrace();
    }

    private void GenerateWriteCollection(CodeBuilder builder, PropertyInfo property, string attrName, string modelVar)
    {
        var accessExpr = $"{modelVar}.{property.Name}";
        var elementType = property.ElementType!;

        builder.AppendLine($"writer.WritePropertyName(\"{attrName}\");");

        // Determine if this is a set type (HashSet, ISet, IReadOnlySet)
        var isSetType = IsSetType(property.Type);

        // String set -> SS
        if (isSetType && elementType.SpecialType == SpecialType.System_String)
        {
            builder.OpenBraceWithLine($"if ({accessExpr} != null)");
            builder.AppendLine("writer.WriteStartObject(); writer.WritePropertyName(\"SS\"); writer.WriteStartArray();");
            builder.AppendLine($"foreach (var item in {accessExpr}) writer.WriteStringValue(item);");
            builder.AppendLine("writer.WriteEndArray(); writer.WriteEndObject();");
            builder.CloseBrace();
            builder.OpenBraceWithLine("else");
            builder.AppendLine("writer.WriteStartObject(); writer.WriteBoolean(\"NULL\", true); writer.WriteEndObject();");
            builder.CloseBrace();
            return;
        }

        // Number set -> NS
        if (isSetType && IsNumericType(elementType))
        {
            builder.OpenBraceWithLine($"if ({accessExpr} != null)");
            builder.AppendLine("writer.WriteStartObject(); writer.WritePropertyName(\"NS\"); writer.WriteStartArray();");
            builder.AppendLine($"foreach (var item in {accessExpr}) writer.WriteStringValue(item.ToString(CultureInfo.InvariantCulture));");
            builder.AppendLine("writer.WriteEndArray(); writer.WriteEndObject();");
            builder.CloseBrace();
            builder.OpenBraceWithLine("else");
            builder.AppendLine("writer.WriteStartObject(); writer.WriteBoolean(\"NULL\", true); writer.WriteEndObject();");
            builder.CloseBrace();
            return;
        }

        // General list -> L
        builder.OpenBraceWithLine($"if ({accessExpr} != null)");
        builder.AppendLine("writer.WriteStartObject(); writer.WritePropertyName(\"L\"); writer.WriteStartArray();");
        builder.OpenBraceWithLine($"foreach (var item in {accessExpr})");
        EmitWriteElementValue(builder, elementType, "item");
        builder.CloseBrace();
        builder.AppendLine("writer.WriteEndArray(); writer.WriteEndObject();");
        builder.CloseBrace();
        builder.OpenBraceWithLine("else");
        builder.AppendLine("writer.WriteStartObject(); writer.WriteBoolean(\"NULL\", true); writer.WriteEndObject();");
        builder.CloseBrace();
    }

    /// <summary>
    /// Emits a single type-wrapped value for an element inside a List (L type).
    /// </summary>
    private void EmitWriteElementValue(CodeBuilder builder, ITypeSymbol elementType, string varName)
    {
        if (IsComplexType(elementType))
        {
            var normalizedTypeName = NamingHelpers.NormalizeTypeName(elementType.Name);
            builder.AppendLine($"writer.WriteStartObject(); writer.WritePropertyName(\"M\");");
            builder.AppendLine($"DynamoJsonMapper.{normalizedTypeName}.WriteToJson(writer, {varName});");
            builder.AppendLine("writer.WriteEndObject();");
            return;
        }

        switch (elementType.SpecialType)
        {
            case SpecialType.System_String:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}); writer.WriteEndObject();");
                break;
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"N\", {varName}.ToString(CultureInfo.InvariantCulture)); writer.WriteEndObject();");
                break;
            case SpecialType.System_Boolean:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteBoolean(\"BOOL\", {varName}); writer.WriteEndObject();");
                break;
            case SpecialType.System_Char:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString()); writer.WriteEndObject();");
                break;
            case SpecialType.System_DateTime:
                builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString(\"o\")); writer.WriteEndObject();");
                break;
            default:
                if (elementType.Name == "DateTimeOffset")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString(\"o\")); writer.WriteEndObject();");
                else if (elementType.Name == "Guid")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString()); writer.WriteEndObject();");
                else if (elementType.Name == "TimeSpan")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString()); writer.WriteEndObject();");
                else if (elementType.Name == "DateOnly")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString(\"yyyy-MM-dd\")); writer.WriteEndObject();");
                else if (elementType.Name == "TimeOnly")
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString(\"HH:mm:ss.fffffff\")); writer.WriteEndObject();");
                else if (elementType.TypeKind == TypeKind.Enum)
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString()); writer.WriteEndObject();");
                else
                    builder.AppendLine($"writer.WriteStartObject(); writer.WriteString(\"S\", {varName}.ToString()); writer.WriteEndObject();");
                break;
        }
    }

    private void GenerateWriteDictionary(CodeBuilder builder, PropertyInfo property, string attrName, string modelVar)
    {
        var accessExpr = $"{modelVar}.{property.Name}";
        var dictionaryTypes = property.DictionaryTypes!.Value;
        var keyType = dictionaryTypes.KeyType;
        var valueType = dictionaryTypes.ValueType;

        // Only string-keyed dictionaries are supported (DynamoDB limitation)
        if (keyType.SpecialType != SpecialType.System_String)
        {
            builder.AppendLine($"writer.WritePropertyName(\"{attrName}\");");
            builder.AppendLine("writer.WriteStartObject(); writer.WriteBoolean(\"NULL\", true); writer.WriteEndObject();");
            return;
        }

        builder.AppendLine($"writer.WritePropertyName(\"{attrName}\");");
        builder.OpenBraceWithLine($"if ({accessExpr} != null)");
        builder.AppendLine("writer.WriteStartObject(); writer.WritePropertyName(\"M\"); writer.WriteStartObject();");
        builder.OpenBraceWithLine($"foreach (var kvp in {accessExpr})");
        builder.AppendLine("writer.WritePropertyName(kvp.Key);");
        EmitWriteTypeWrappedValue(builder, valueType, "kvp.Value");
        builder.CloseBrace();
        builder.AppendLine("writer.WriteEndObject(); writer.WriteEndObject();");
        builder.CloseBrace();
        builder.OpenBraceWithLine("else");
        builder.AppendLine("writer.WriteStartObject(); writer.WriteBoolean(\"NULL\", true); writer.WriteEndObject();");
        builder.CloseBrace();
    }

    /// <summary>
    /// Emits a type-wrapped value (the {"S": ...} / {"N": ...} etc. wrapper) for a given value expression.
    /// Used for dictionary values and similar contexts.
    /// </summary>
    private void EmitWriteTypeWrappedValue(CodeBuilder builder, ITypeSymbol valueType, string valueExpr)
    {
        if (IsComplexType(valueType))
        {
            var normalizedTypeName = NamingHelpers.NormalizeTypeName(valueType.Name);
            builder.AppendLine($"writer.WriteStartObject(); writer.WritePropertyName(\"M\");");
            builder.AppendLine($"DynamoJsonMapper.{normalizedTypeName}.WriteToJson(writer, {valueExpr});");
            builder.AppendLine("writer.WriteEndObject();");
            return;
        }

        // Reuse the element write logic (same type-wrapper patterns)
        EmitWriteElementValue(builder, valueType, valueExpr);
    }

    // ─── ReadFromJson ───────────────────────────────────────────────────

    private void GenerateReadFromJson(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        var dynamoModelAttr = type.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault();
        if (dynamoModelAttr == null)
            dynamoModelAttr = GetInheritedDynamoModelAttribute(type);

        var inheritsDynamoModel = HasInheritedDynamoModel(type);
        var shouldGenerate = dynamoModelAttr != null ||
                             (type.IsAbstract && HasConcreteSubtypes(type, context)) ||
                             (!type.IsAbstract && dynamoModelAttr == null && !inheritsDynamoModel) ||
                             inheritsDynamoModel;

        if (!shouldGenerate)
            return;

        builder.AppendLine();
        builder.OpenBraceWithLine($"public static {type.FullName} ReadFromJson(ref System.Text.Json.Utf8JsonReader reader)");

        // Reset variable counter for each method
        _variableCounter = 0;

        if (type.IsAbstract && HasConcreteSubtypes(type, context))
        {
            GenerateAbstractReadDispatch(builder, type, context);
        }
        else
        {
            GenerateConcreteReadFromJson(builder, type);
        }

        builder.CloseBrace();
    }

    private void GenerateAbstractReadDispatch(CodeBuilder builder, DynamoTypeInfo type, GenerationContext context)
    {
        var dynamoModelAttr = type.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault()
                              ?? GetInheritedDynamoModelAttribute(type);
        var typeNameField = dynamoModelAttr?.TypeName ?? "Type";

        // For abstract types we need to peek at the type discriminator.
        // We copy the reader, scan for the discriminator field, then dispatch.
        builder.AppendLine("// Copy reader to peek at type discriminator");
        builder.AppendLine("var readerCopy = reader;");
        builder.AppendLine("string? typeDiscriminator = null;");
        builder.AppendLine("if (readerCopy.TokenType != JsonTokenType.StartObject)");
        builder.Indent().AppendLine("readerCopy.Read();").Unindent();
        builder.OpenBraceWithLine("while (readerCopy.Read())");
        builder.AppendLine("if (readerCopy.TokenType == JsonTokenType.EndObject) break;");
        builder.AppendLine("if (readerCopy.TokenType != JsonTokenType.PropertyName) continue;");
        builder.AppendLine("var peekPropName = readerCopy.GetString();");
        builder.OpenBraceWithLine($"if (peekPropName == \"{typeNameField}\")");
        builder.AppendLine("readerCopy.Read(); // StartObject of {\"S\": ...}");
        builder.AppendLine("readerCopy.Read(); // \"S\"");
        builder.AppendLine("readerCopy.Read(); // value");
        builder.AppendLine("typeDiscriminator = readerCopy.GetString();");
        builder.AppendLine("break;");
        builder.CloseBrace();
        builder.OpenBraceWithLine("else");
        builder.AppendLine("readerCopy.Read(); // move to value");
        builder.AppendLine("readerCopy.Skip(); // skip the value");
        builder.CloseBrace();
        builder.CloseBrace();
        builder.AppendLine();
        builder.AppendLine($"if (typeDiscriminator == null)");
        builder.Indent().AppendLine($"throw new InvalidOperationException(\"Missing {typeNameField} discriminator for abstract type {type.FullName}\");").Unindent();
        builder.AppendLine();
        builder.OpenBraceWithLine("return typeDiscriminator switch");

        if (context.TypeRegistry.TryGetValue(type.FullName, out var concreteTypes))
        {
            foreach (var concreteType in concreteTypes)
            {
                var normalizedTypeName = NamingHelpers.NormalizeTypeName(concreteType.Name);
                builder.AppendLine($"\"{concreteType.FullName}\" => DynamoJsonMapper.{normalizedTypeName}.ReadFromJson(ref reader),");
            }
        }

        builder.AppendLine($"_ => throw new InvalidOperationException($\"Unknown type: {{typeDiscriminator}} for abstract type {type.FullName}\")");
        builder.CloseBrace().Append(";");
        builder.AppendLine();
    }

    private void GenerateConcreteReadFromJson(CodeBuilder builder, DynamoTypeInfo type)
    {
        builder.AppendLine("if (reader.TokenType != JsonTokenType.StartObject)");
        builder.Indent().AppendLine("reader.Read();").Unindent();
        builder.AppendLine();

        var constructor = FindBestConstructor(type);
        var hasInitOnlyProperties = GetAllProperties(type)
            .Any(p => p.Symbol?.SetMethod is { IsInitOnly: true });

        if (constructor != null && (constructor.Parameters.Any() || hasInitOnlyProperties))
        {
            GenerateConstructorReadFromJson(builder, type, constructor);
        }
        else
        {
            GenerateParameterlessReadFromJson(builder, type);
        }
    }

    private void GenerateParameterlessReadFromJson(CodeBuilder builder, DynamoTypeInfo type)
    {
        builder.AppendLine($"var result = new {type.FullName}();");
        builder.OpenBraceWithLine("while (reader.Read())");
        builder.AppendLine("if (reader.TokenType == JsonTokenType.EndObject) break;");
        builder.AppendLine();

        var allProperties = GetAllProperties(type);
        var readableProperties = allProperties
            .Where(p => (p.ConverterTypeName != null || _typeHandlerRegistry.CanHandle(p)) && !p.IsIgnored(IgnoreDirection.WhenReading) && p.Symbol?.SetMethod != null && IsSupportedForJsonRead(p))
            .ToList();

        var isFirst = true;
        foreach (var property in readableProperties)
        {
            var attrName = property.GetDynamoAttributeName();
            var keyword = isFirst ? "if" : "else if";
            builder.OpenBraceWithLine($"{keyword} (reader.ValueTextEquals(\"{attrName}\"u8))");
            GenerateReadProperty(builder, property, "result");
            builder.CloseBrace();
            isFirst = false;
        }

        if (readableProperties.Count > 0)
        {
            builder.OpenBraceWithLine("else");
        }
        builder.AppendLine("reader.Read(); // Move past property name to value");
        builder.AppendLine("reader.Skip(); // Skip the type wrapper object");
        if (readableProperties.Count > 0)
        {
            builder.CloseBrace();
        }

        builder.CloseBrace(); // while
        builder.AppendLine("return result;");
    }

    private void GenerateConstructorReadFromJson(CodeBuilder builder, DynamoTypeInfo type, IMethodSymbol constructor)
    {
        var allProperties = GetAllProperties(type);

        // Build mapping of constructor parameters to properties
        var ctorParams = constructor.Parameters;
        var ctorParamProperties = new List<(IParameterSymbol Param, PropertyInfo? Property)>();
        var ctorParamNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var param in ctorParams)
        {
            var matchedProperty = FindPropertyIgnoreCase(type, param.Name);
            ctorParamProperties.Add((param, matchedProperty));
            if (matchedProperty != null)
                ctorParamNames.Add(matchedProperty.Name);
        }

        // Readable properties: include properties that match constructor params (even without setter)
        // or properties that have a setter
        var readableProperties = allProperties
            .Where(p => (p.ConverterTypeName != null || _typeHandlerRegistry.CanHandle(p))
                        && !p.IsIgnored(IgnoreDirection.WhenReading)
                        && (p.Symbol?.SetMethod != null || ctorParamNames.Contains(p.Name))
                        && IsSupportedForJsonRead(p))
            .ToList();

        // Additional settable properties: not matched to constructor params, and have a setter
        var additionalProperties = readableProperties
            .Where(p => !ctorParamNames.Contains(p.Name) && p.Symbol?.SetMethod != null)
            .ToList();

        // Declare local variables for all readable properties
        foreach (var property in readableProperties)
        {
            var varName = GetLocalVarName(property.Name);
            var typeName = GetFullyQualifiedTypeName(property);
            var defaultExpr = GetDefaultValueExpression(property.Type, property.IsNullable);
            builder.AppendLine($"{typeName} {varName} = {defaultExpr};");
        }

        builder.AppendLine();
        builder.OpenBraceWithLine("while (reader.Read())");
        builder.AppendLine("if (reader.TokenType == JsonTokenType.EndObject) break;");
        builder.AppendLine();

        var isFirst = true;
        foreach (var property in readableProperties)
        {
            var attrName = property.GetDynamoAttributeName();
            var keyword = isFirst ? "if" : "else if";
            builder.OpenBraceWithLine($"{keyword} (reader.ValueTextEquals(\"{attrName}\"u8))");
            GenerateReadProperty(builder, property, "result", assignmentTarget: GetLocalVarName(property.Name));
            builder.CloseBrace();
            isFirst = false;
        }

        if (readableProperties.Count > 0)
        {
            builder.OpenBraceWithLine("else");
        }
        builder.AppendLine("reader.Read(); // Move past property name to value");
        builder.AppendLine("reader.Skip(); // Skip the type wrapper object");
        if (readableProperties.Count > 0)
        {
            builder.CloseBrace();
        }

        builder.CloseBrace(); // while

        // Build constructor arguments
        var readablePropertyNames = new HashSet<string>(readableProperties.Select(p => p.Name));
        var ctorArgs = new List<string>();
        foreach (var (param, matchedProperty) in ctorParamProperties)
        {
            if (matchedProperty != null && readablePropertyNames.Contains(matchedProperty.Name))
            {
                ctorArgs.Add(GetLocalVarName(matchedProperty.Name));
            }
            else
            {
                // Use default! for non-nullable reference types to suppress nullable warnings
                var paramType = param.Type;
                var isParamNullable = paramType.NullableAnnotation == NullableAnnotation.Annotated;
                ctorArgs.Add(!paramType.IsValueType && !isParamNullable ? "default!" : "default");
            }
        }

        var ctorArgString = string.Join(", ", ctorArgs);

        if (additionalProperties.Count > 0)
        {
            builder.AppendLine($"return new {type.FullName}({ctorArgString})");
            builder.AppendLine("{");
            builder.Indent();
            foreach (var prop in additionalProperties)
            {
                builder.AppendLine($"{prop.Name} = {GetLocalVarName(prop.Name)},");
            }
            builder.Unindent();
            builder.AppendLine("};");
        }
        else
        {
            builder.AppendLine($"return new {type.FullName}({ctorArgString});");
        }
    }

    private void GenerateReadProperty(CodeBuilder builder, PropertyInfo property, string resultVar, string? assignmentTarget = null)
    {
        var underlyingType = property.UnderlyingType;
        var isNullable = property.IsNullable;
        var hasUnixTimestamp = property.Attributes.Any(a => a is UnixTimestampAttributeInfo);

        // Custom converter handling - delegate entirely to the converter
        if (property.ConverterTypeName != null)
        {
            var target = assignmentTarget ?? $"{resultVar}.{property.Name}";
            builder.AppendLine("reader.Read(); // Move past property name to value");
            builder.AppendLine($"{target} = new {property.ConverterTypeName}().Read(ref reader);");
            return;
        }

        // Dictionary handling
        if (property.IsDictionary && property.DictionaryTypes.HasValue)
        {
            GenerateReadDictionary(builder, property, resultVar, assignmentTarget);
            return;
        }

        // Collection handling
        if (property.IsCollection && property.ElementType != null)
        {
            GenerateReadCollection(builder, property, resultVar, assignmentTarget);
            return;
        }

        // Complex type handling
        if (IsComplexType(underlyingType))
        {
            GenerateReadComplexType(builder, property, resultVar, assignmentTarget);
            return;
        }

        // Primitive handling: read the type wrapper object
        builder.AppendLine("reader.Read(); // StartObject of type wrapper");
        builder.AppendLine("reader.Read(); // type descriptor (S, N, BOOL, NULL, etc.)");

        if (isNullable)
        {
            var target = assignmentTarget ?? $"{resultVar}.{property.Name}";
            // Check if NULL descriptor using zero-allocation UTF-8 comparison
            builder.OpenBraceWithLine("if (reader.ValueTextEquals(\"NULL\"u8))");
            builder.AppendLine("reader.Read(); // value (true)");
            builder.AppendLine($"{target} = null;");
            builder.CloseBrace();
            builder.OpenBraceWithLine("else");
            builder.AppendLine("reader.Read(); // value");
            EmitReadPrimitiveAssignment(builder, property, resultVar, hasUnixTimestamp, assignmentTarget);
            builder.CloseBrace();
        }
        else
        {
            builder.AppendLine("reader.Read(); // value");
            EmitReadPrimitiveAssignment(builder, property, resultVar, hasUnixTimestamp, assignmentTarget);
        }

        builder.AppendLine("reader.Read(); // EndObject of type wrapper");
    }

    private void EmitReadPrimitiveAssignment(CodeBuilder builder, PropertyInfo property, string resultVar, bool hasUnixTimestamp, string? assignmentTarget = null)
    {
        var underlyingType = property.UnderlyingType;
        var propAccess = assignmentTarget ?? $"{resultVar}.{property.Name}";

        // UnixTimestamp DateTime/DateTimeOffset -> read N as number
        if (hasUnixTimestamp)
        {
            var unixAttr = property.Attributes.OfType<UnixTimestampAttributeInfo>().First();
            var isMilliseconds = unixAttr.Format == UnixTimestampFormat.Milliseconds;
            if (underlyingType.Name == "DateTimeOffset")
            {
                var method = isMilliseconds ? "FromUnixTimeMilliseconds" : "FromUnixTimeSeconds";
                builder.AppendLine($"{propAccess} = DateTimeOffset.{method}(long.Parse(reader.GetString()!, CultureInfo.InvariantCulture));");
            }
            else
            {
                var method = isMilliseconds ? "FromUnixTimeMilliseconds" : "FromUnixTimeSeconds";
                builder.AppendLine($"{propAccess} = DateTimeOffset.{method}(long.Parse(reader.GetString()!, CultureInfo.InvariantCulture)).UtcDateTime;");
            }
            return;
        }

        // Try Utf8Parser for numeric types to avoid string allocation
        var utf8Expr = GetUtf8NumericParseExpression(underlyingType, property.Name);
        if (utf8Expr != null)
        {
            builder.AppendLine($"{utf8Expr};");
            builder.AppendLine($"{propAccess} = {property.Name}Parsed;");
            return;
        }

        switch (underlyingType.SpecialType)
        {
            case SpecialType.System_String:
                builder.AppendLine($"{propAccess} = reader.GetString()!;");
                break;
            case SpecialType.System_Boolean:
                builder.AppendLine($"{propAccess} = reader.GetBoolean();");
                break;
            case SpecialType.System_Char:
                builder.AppendLine($"{propAccess} = reader.GetString()![0];");
                break;
            case SpecialType.System_DateTime:
                builder.AppendLine($"{propAccess} = DateTime.ParseExact(reader.GetString()!, \"o\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);");
                break;
            default:
                if (underlyingType.Name == "DateTimeOffset")
                    builder.AppendLine($"{propAccess} = DateTimeOffset.ParseExact(reader.GetString()!, \"o\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);");
                else if (underlyingType.Name == "Guid")
                    builder.AppendLine($"{propAccess} = Guid.Parse(reader.GetString()!);");
                else if (underlyingType.Name == "TimeSpan")
                    builder.AppendLine($"{propAccess} = TimeSpan.Parse(reader.GetString()!, CultureInfo.InvariantCulture);");
                else if (underlyingType.Name == "DateOnly")
                    builder.AppendLine($"{propAccess} = DateOnly.Parse(reader.GetString()!, CultureInfo.InvariantCulture);");
                else if (underlyingType.Name == "TimeOnly")
                    builder.AppendLine($"{propAccess} = TimeOnly.Parse(reader.GetString()!, CultureInfo.InvariantCulture);");
                else if (underlyingType.TypeKind == TypeKind.Enum)
                    builder.AppendLine($"{propAccess} = Enum.Parse<{underlyingType.ToDisplayString()}>(reader.GetString()!);");
                else
                    builder.AppendLine($"{propAccess} = reader.GetString()!;");
                break;
        }
    }

    private void GenerateReadComplexType(CodeBuilder builder, PropertyInfo property, string resultVar, string? assignmentTarget = null)
    {
        var normalizedTypeName = NamingHelpers.NormalizeTypeName(property.UnderlyingType.Name);
        var target = assignmentTarget ?? $"{resultVar}.{property.Name}";

        builder.AppendLine("reader.Read(); // StartObject of type wrapper");
        builder.AppendLine("reader.Read(); // type descriptor (M or NULL)");
        builder.OpenBraceWithLine("if (reader.ValueTextEquals(\"M\"u8))");
        builder.AppendLine("reader.Read(); // StartObject of the nested entity");
        builder.AppendLine($"{target} = DynamoJsonMapper.{normalizedTypeName}.ReadFromJson(ref reader);");
        builder.CloseBrace();
        builder.OpenBraceWithLine("else");
        builder.AppendLine("reader.Read(); // value (true for NULL)");
        if (property.IsNullable)
            builder.AppendLine($"{target} = null;");
        builder.CloseBrace();
        builder.AppendLine("reader.Read(); // EndObject of type wrapper");
    }

    private void GenerateReadCollection(CodeBuilder builder, PropertyInfo property, string resultVar, string? assignmentTarget = null)
    {
        var elementType = property.ElementType!;
        var isSetType = IsSetType(property.Type);
        var elementTypeName = elementType.ToDisplayString();

        // Determine the collection type and DynamoDB wire type
        if (isSetType && elementType.SpecialType == SpecialType.System_String)
        {
            // SS type
            GenerateReadStringSet(builder, property, resultVar, assignmentTarget);
            return;
        }

        if (isSetType && IsNumericType(elementType))
        {
            // NS type
            GenerateReadNumberSet(builder, property, resultVar, elementType, assignmentTarget);
            return;
        }

        // L type (general list) — for set types with non-string/non-numeric elements,
        // wrap in HashSet<T> to satisfy the target type constraint.
        GenerateReadList(builder, property, resultVar, elementType, assignmentTarget, wrapAsSet: isSetType);
    }

    private void GenerateReadStringSet(CodeBuilder builder, PropertyInfo property, string resultVar, string? assignmentTarget = null)
    {
        var collectionTypeName = GetCollectionTypeName(property.Type);
        var varName = GetUniqueVarName("ss");
        var target = assignmentTarget ?? $"{resultVar}.{property.Name}";

        builder.AppendLine("reader.Read(); // StartObject of type wrapper");
        builder.AppendLine("reader.Read(); // type descriptor (SS or NULL)");
        builder.OpenBraceWithLine("if (reader.ValueTextEquals(\"SS\"u8))");
        builder.AppendLine("reader.Read(); // StartArray");
        builder.AppendLine($"var {varName}Set = new HashSet<string>();");
        builder.OpenBraceWithLine($"while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)");
        builder.AppendLine($"{varName}Set.Add(reader.GetString()!);");
        builder.CloseBrace();
        builder.AppendLine($"{target} = {ConvertToTargetCollection(property.Type, property.ElementType!, $"{varName}Set")};");
        builder.CloseBrace();
        builder.OpenBraceWithLine("else");
        builder.AppendLine("reader.Read(); // value (true for NULL)");
        if (property.IsNullable)
            builder.AppendLine($"{target} = null;");
        builder.CloseBrace();
        builder.AppendLine("reader.Read(); // EndObject of type wrapper");
    }

    private void GenerateReadNumberSet(CodeBuilder builder, PropertyInfo property, string resultVar, ITypeSymbol elementType, string? assignmentTarget = null)
    {
        var elementTypeName = elementType.ToDisplayString();
        var varName = GetUniqueVarName("ns");
        var target = assignmentTarget ?? $"{resultVar}.{property.Name}";

        builder.AppendLine("reader.Read(); // StartObject of type wrapper");
        builder.AppendLine("reader.Read(); // type descriptor (NS or NULL)");
        builder.OpenBraceWithLine("if (reader.ValueTextEquals(\"NS\"u8))");
        builder.AppendLine("reader.Read(); // StartArray");
        builder.AppendLine($"var {varName}Set = new HashSet<{elementTypeName}>();");
        builder.OpenBraceWithLine($"while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)");

        var utf8NsExpr = GetUtf8NumericParseExpression(elementType, $"{varName}Elem");
        if (utf8NsExpr != null)
        {
            builder.AppendLine($"{utf8NsExpr};");
            builder.AppendLine($"{varName}Set.Add({varName}ElemParsed);");
        }
        else
        {
            var parseExpr = GetNumericParseExpression(elementType, "reader.GetString()!");
            builder.AppendLine($"{varName}Set.Add({parseExpr});");
        }
        builder.CloseBrace();
        builder.AppendLine($"{target} = {ConvertToTargetCollection(property.Type, elementType, $"{varName}Set")};");
        builder.CloseBrace();
        builder.OpenBraceWithLine("else");
        builder.AppendLine("reader.Read(); // value (true for NULL)");
        if (property.IsNullable)
            builder.AppendLine($"{target} = null;");
        builder.CloseBrace();
        builder.AppendLine("reader.Read(); // EndObject of type wrapper");
    }

    private void GenerateReadList(CodeBuilder builder, PropertyInfo property, string resultVar, ITypeSymbol elementType, string? assignmentTarget = null, bool wrapAsSet = false)
    {
        var elementTypeName = elementType.ToDisplayString();
        var varName = GetUniqueVarName("l");
        var target = assignmentTarget ?? $"{resultVar}.{property.Name}";

        builder.AppendLine("reader.Read(); // StartObject of type wrapper");
        builder.AppendLine("reader.Read(); // type descriptor (L or NULL)");
        builder.OpenBraceWithLine("if (reader.ValueTextEquals(\"L\"u8))");
        builder.AppendLine("reader.Read(); // StartArray");
        builder.AppendLine($"var {varName}List = new List<{elementTypeName}>();");
        builder.OpenBraceWithLine($"while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)");

        // Each element in the array is a type-wrapped value
        EmitReadElementValue(builder, elementType, $"{varName}List");

        builder.CloseBrace(); // while
        if (wrapAsSet)
        {
            // Target is a set type but elements are not string/numeric,
            // so we need to wrap the list in a HashSet to match the target type.
            builder.AppendLine($"{target} = new HashSet<{elementTypeName}>({varName}List);");
        }
        else
        {
            builder.AppendLine($"{target} = {ConvertToTargetCollection(property.Type, elementType, $"{varName}List")};");
        }
        builder.CloseBrace(); // if L
        builder.OpenBraceWithLine("else");
        builder.AppendLine("reader.Read(); // value (true for NULL)");
        if (property.IsNullable)
            builder.AppendLine($"{target} = null;");
        builder.CloseBrace();
        builder.AppendLine("reader.Read(); // EndObject of type wrapper");
    }

    /// <summary>
    /// Emits code to read a single type-wrapped element from a list.
    /// The reader is positioned at the StartObject of the element's type wrapper.
    /// </summary>
    private void EmitReadElementValue(CodeBuilder builder, ITypeSymbol elementType, string listVar)
    {
        if (IsComplexType(elementType))
        {
            var normalizedTypeName = NamingHelpers.NormalizeTypeName(elementType.Name);
            builder.AppendLine("// Element is {\"M\": {...}}");
            builder.AppendLine("reader.Read(); // \"M\"");
            builder.AppendLine("reader.Read(); // StartObject of nested entity");
            builder.AppendLine($"{listVar}.Add(DynamoJsonMapper.{normalizedTypeName}.ReadFromJson(ref reader));");
            builder.AppendLine("reader.Read(); // EndObject of element wrapper");
            return;
        }

        // Primitive element: {"S": "..."} or {"N": "..."} or {"BOOL": ...}
        builder.AppendLine("reader.Read(); // type descriptor");
        builder.AppendLine("reader.Read(); // value");

        switch (elementType.SpecialType)
        {
            case SpecialType.System_String:
                builder.AppendLine($"{listVar}.Add(reader.GetString()!);");
                break;
            case SpecialType.System_Boolean:
                builder.AppendLine($"{listVar}.Add(reader.GetBoolean());");
                break;
            case SpecialType.System_Char:
                builder.AppendLine($"{listVar}.Add(reader.GetString()![0]);");
                break;
            case SpecialType.System_DateTime:
                builder.AppendLine($"{listVar}.Add(DateTime.ParseExact(reader.GetString()!, \"o\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));");
                break;
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Decimal:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            {
                var utf8Expr = GetUtf8NumericParseExpression(elementType, $"{listVar}Elem");
                if (utf8Expr != null)
                {
                    builder.AppendLine($"{utf8Expr};");
                    builder.AppendLine($"{listVar}.Add({listVar}ElemParsed);");
                }
                else
                {
                    builder.AppendLine($"{listVar}.Add({GetNumericParseExpression(elementType, "reader.GetString()!")});");
                }
                break;
            }
            default:
                if (elementType.Name == "DateTimeOffset")
                    builder.AppendLine($"{listVar}.Add(DateTimeOffset.ParseExact(reader.GetString()!, \"o\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));");
                else if (elementType.Name == "Guid")
                    builder.AppendLine($"{listVar}.Add(Guid.Parse(reader.GetString()!));");
                else if (elementType.Name == "TimeSpan")
                    builder.AppendLine($"{listVar}.Add(TimeSpan.Parse(reader.GetString()!, CultureInfo.InvariantCulture));");
                else if (elementType.Name == "DateOnly")
                    builder.AppendLine($"{listVar}.Add(DateOnly.Parse(reader.GetString()!, CultureInfo.InvariantCulture));");
                else if (elementType.Name == "TimeOnly")
                    builder.AppendLine($"{listVar}.Add(TimeOnly.Parse(reader.GetString()!, CultureInfo.InvariantCulture));");
                else if (elementType.TypeKind == TypeKind.Enum)
                    builder.AppendLine($"{listVar}.Add(Enum.Parse<{elementType.ToDisplayString()}>(reader.GetString()!));");
                else
                    builder.AppendLine($"{listVar}.Add(reader.GetString()!);");
                break;
        }

        builder.AppendLine("reader.Read(); // EndObject of element wrapper");
    }

    private void GenerateReadDictionary(CodeBuilder builder, PropertyInfo property, string resultVar, string? assignmentTarget = null)
    {
        var dictionaryTypes = property.DictionaryTypes!.Value;
        var keyType = dictionaryTypes.KeyType;
        var valueType = dictionaryTypes.ValueType;
        var keyTypeName = keyType.ToDisplayString();
        var valueTypeName = valueType.ToDisplayString();
        var varName = GetUniqueVarName("dict");
        var target = assignmentTarget ?? $"{resultVar}.{property.Name}";

        builder.AppendLine("reader.Read(); // StartObject of type wrapper");
        builder.AppendLine("reader.Read(); // type descriptor (M or NULL)");
        builder.OpenBraceWithLine("if (reader.ValueTextEquals(\"M\"u8))");
        builder.AppendLine("reader.Read(); // StartObject of the map");
        builder.AppendLine($"var {varName}Map = new Dictionary<{keyTypeName}, {valueTypeName}>();");
        builder.OpenBraceWithLine($"while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)");
        builder.AppendLine($"var {varName}Key = reader.GetString()!;");

        // Read the type-wrapped value for this dictionary entry
        EmitReadDictionaryValue(builder, valueType, $"{varName}Map", $"{varName}Key");

        builder.CloseBrace(); // while
        builder.AppendLine($"{target} = {varName}Map;");
        builder.CloseBrace(); // if M
        builder.OpenBraceWithLine("else");
        builder.AppendLine("reader.Read(); // value (true for NULL)");
        if (property.IsNullable)
            builder.AppendLine($"{target} = null;");
        builder.CloseBrace();
        builder.AppendLine("reader.Read(); // EndObject of type wrapper");
    }

    private void EmitReadDictionaryValue(CodeBuilder builder, ITypeSymbol valueType, string mapVar, string keyVar)
    {
        if (IsComplexType(valueType))
        {
            var normalizedTypeName = NamingHelpers.NormalizeTypeName(valueType.Name);
            builder.AppendLine("reader.Read(); // StartObject of value type wrapper");
            builder.AppendLine("reader.Read(); // \"M\"");
            builder.AppendLine("reader.Read(); // StartObject of nested entity");
            builder.AppendLine($"{mapVar}[{keyVar}] = DynamoJsonMapper.{normalizedTypeName}.ReadFromJson(ref reader);");
            builder.AppendLine("reader.Read(); // EndObject of value type wrapper");
            return;
        }

        // Primitive value: read type wrapper
        builder.AppendLine("reader.Read(); // StartObject of value type wrapper");
        builder.AppendLine("reader.Read(); // type descriptor");
        builder.AppendLine("reader.Read(); // value");

        // Try Utf8Parser for numeric dictionary values
        var dictUtf8Expr = GetUtf8NumericParseExpression(valueType, $"{mapVar}Val");
        if (dictUtf8Expr != null)
        {
            builder.AppendLine($"{dictUtf8Expr};");
            builder.AppendLine($"{mapVar}[{keyVar}] = {mapVar}ValParsed;");
            builder.AppendLine("reader.Read(); // EndObject of value type wrapper");
            return;
        }

        switch (valueType.SpecialType)
        {
            case SpecialType.System_String:
                builder.AppendLine($"{mapVar}[{keyVar}] = reader.GetString()!;");
                break;
            case SpecialType.System_Boolean:
                builder.AppendLine($"{mapVar}[{keyVar}] = reader.GetBoolean();");
                break;
            case SpecialType.System_DateTime:
                builder.AppendLine($"{mapVar}[{keyVar}] = DateTime.ParseExact(reader.GetString()!, \"o\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);");
                break;
            case SpecialType.System_Char:
                builder.AppendLine($"{mapVar}[{keyVar}] = reader.GetString()![0];");
                break;
            default:
                if (valueType.Name == "DateTimeOffset")
                    builder.AppendLine($"{mapVar}[{keyVar}] = DateTimeOffset.ParseExact(reader.GetString()!, \"o\", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);");
                else if (valueType.Name == "Guid")
                    builder.AppendLine($"{mapVar}[{keyVar}] = Guid.Parse(reader.GetString()!);");
                else if (valueType.Name == "TimeSpan")
                    builder.AppendLine($"{mapVar}[{keyVar}] = TimeSpan.Parse(reader.GetString()!, CultureInfo.InvariantCulture);");
                else if (valueType.Name == "DateOnly")
                    builder.AppendLine($"{mapVar}[{keyVar}] = DateOnly.Parse(reader.GetString()!, CultureInfo.InvariantCulture);");
                else if (valueType.Name == "TimeOnly")
                    builder.AppendLine($"{mapVar}[{keyVar}] = TimeOnly.Parse(reader.GetString()!, CultureInfo.InvariantCulture);");
                else if (valueType.TypeKind == TypeKind.Enum)
                    builder.AppendLine($"{mapVar}[{keyVar}] = Enum.Parse<{valueType.ToDisplayString()}>(reader.GetString()!);");
                else
                    builder.AppendLine($"{mapVar}[{keyVar}] = reader.GetString()!;");
                break;
        }

        builder.AppendLine("reader.Read(); // EndObject of value type wrapper");
    }

    // ─── Helper methods ─────────────────────────────────────────────────

    private static IMethodSymbol? FindBestConstructor(DynamoTypeInfo type)
    {
        var constructors = type.Symbol.Constructors
            .Where(c => c.DeclaredAccessibility == Accessibility.Public)
            // Exclude the synthesized record copy constructor (single parameter of the same type)
            .Where(c => !(c.Parameters.Length == 1 &&
                          SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, type.Symbol)))
            .ToList();

        // Prefer constructor with parameters (primary constructor)
        var paramConstructor = constructors.FirstOrDefault(c => c.Parameters.Any());
        if (paramConstructor != null)
            return paramConstructor;

        // Fallback to parameterless constructor
        return constructors.FirstOrDefault(c => !c.Parameters.Any());
    }

    private static PropertyInfo? FindPropertyIgnoreCase(DynamoTypeInfo type, string propertyName)
    {
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

    private static string GetLocalVarName(string propertyName)
    {
        return "__" + char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
    }

    /// <summary>
    /// Checks whether a property type is supported for JSON wire format reading.
    /// Nested collections and dictionaries with non-string keys are not yet supported.
    /// </summary>
    private static bool IsSupportedForJsonRead(PropertyInfo property)
    {
        // Nested collections (e.g., IEnumerable<IEnumerable<string>>) are not supported
        if (property.IsCollection && property.ElementType != null)
        {
            var elementType = property.ElementType;
            if (elementType is IArrayTypeSymbol)
                return false;
            if (elementType is INamedTypeSymbol namedElem && namedElem.IsGenericType)
            {
                var name = namedElem.Name;
                if (name is "List" or "IList" or "ICollection" or "IEnumerable" or
                    "HashSet" or "ISet" or "IReadOnlyCollection" or
                    "IReadOnlyList" or "IReadOnlySet" or "Collection" or
                    "Dictionary" or "IDictionary" or "IReadOnlyDictionary")
                    return false;
            }
        }

        // Dictionaries with non-string keys (e.g., Dictionary<Guid, string>) are not supported
        if (property.IsDictionary && property.DictionaryTypes.HasValue)
        {
            var keyType = property.DictionaryTypes.Value.KeyType;
            if (keyType.SpecialType != SpecialType.System_String)
                return false;

            // Also check for nested collection/dictionary values
            var valueType = property.DictionaryTypes.Value.ValueType;
            if (valueType is INamedTypeSymbol namedVal && namedVal.IsGenericType)
            {
                var name = namedVal.Name;
                if (name is "List" or "IList" or "ICollection" or "IEnumerable" or
                    "HashSet" or "ISet" or "IReadOnlyCollection" or
                    "IReadOnlyList" or "IReadOnlySet" or "Collection" or
                    "Dictionary" or "IDictionary" or "IReadOnlyDictionary")
                    return false;
            }
            if (valueType is IArrayTypeSymbol)
                return false;
        }

        return true;
    }

    private static string GetFullyQualifiedTypeName(PropertyInfo property)
    {
        var typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        // FullyQualifiedFormat does not include nullable reference type annotations,
        // so we need to append '?' manually for nullable reference types.
        if (property.IsNullable && !property.Type.IsValueType && !typeName.EndsWith("?"))
            typeName += "?";
        return typeName;
    }

    private static string GetDefaultValueExpression(ITypeSymbol type, bool isNullable)
    {
        if (isNullable)
            return "default";
        if (type.IsValueType)
            return "default";
        return "default!";
    }

    private List<PropertyInfo> GetAllProperties(DynamoTypeInfo type)
    {
        var properties = new List<PropertyInfo>();
        var current = type;

        while (current != null)
        {
            properties.AddRange(current.Properties);
            current = current.BaseType;
        }

        // Remove duplicates - keep most derived version
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

    private static bool HasConcreteSubtypes(DynamoTypeInfo type, GenerationContext context)
    {
        return context.TypeRegistry.ContainsKey(type.FullName);
    }

    private static bool HasInheritedDynamoModel(DynamoTypeInfo type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.Attributes.Any(a => a is DynamoModelAttributeInfo))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static DynamoModelAttributeInfo? GetInheritedDynamoModelAttribute(DynamoTypeInfo type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            var attr = current.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault();
            if (attr != null)
                return attr;
            current = current.BaseType;
        }
        return null;
    }

    private static bool InheritsFromAbstractType(DynamoTypeInfo type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.IsAbstract)
                return true;
            current = current.BaseType;
        }
        return false;
    }

    private static bool IsComplexType(ITypeSymbol type)
    {
        if (type.SpecialType != SpecialType.None)
            return false;

        if (type.Name == "Guid" || type.Name == "TimeSpan" || type.Name == "DateTimeOffset" ||
            type.Name == "DateOnly" || type.Name == "TimeOnly")
            return false;

        if (type.TypeKind == TypeKind.Enum)
            return false;

        // Exclude collections
        if (type is IArrayTypeSymbol)
            return false;
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var name = namedType.Name;
            if (name == "List" || name == "IList" || name == "ICollection" || name == "IEnumerable" ||
                name == "HashSet" || name == "ISet" || name == "IReadOnlyCollection" ||
                name == "IReadOnlyList" || name == "IReadOnlySet" || name == "Collection" ||
                name == "Dictionary" || name == "IDictionary" || name == "IReadOnlyDictionary" ||
                name == "Nullable")
                return false;
        }

        return type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct || type.IsRecord;
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

    private static bool IsSetType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var name = namedType.Name;
            return name == "HashSet" || name == "ISet" || name == "IReadOnlySet";
        }
        return false;
    }

    private static string GetNumericParseExpression(ITypeSymbol elementType, string readerExpr)
    {
        return elementType.SpecialType switch
        {
            SpecialType.System_Byte => $"byte.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_SByte => $"sbyte.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_Int16 => $"short.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_UInt16 => $"ushort.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_Int32 => $"int.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_UInt32 => $"uint.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_Int64 => $"long.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_UInt64 => $"ulong.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_Decimal => $"decimal.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_Single => $"float.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            SpecialType.System_Double => $"double.Parse({readerExpr}, CultureInfo.InvariantCulture)",
            _ => $"int.Parse({readerExpr}, CultureInfo.InvariantCulture)"
        };
    }

    /// <summary>
    /// Returns a Utf8Parser-based expression that parses a numeric value directly from reader.ValueSpan,
    /// avoiding the string allocation from reader.GetString().
    /// Returns null if the type is not supported by Utf8Parser (e.g. decimal, float, double with DynamoDB's string encoding).
    /// </summary>
    private static string? GetUtf8NumericParseExpression(ITypeSymbol type, string targetExpr)
    {
        // Utf8Parser supports: byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal
        // All parse from UTF-8 byte spans directly, avoiding string allocation.
        // DynamoDB N values are JSON strings like "123", so reader.ValueSpan gives the raw UTF-8 bytes.
        return type.SpecialType switch
        {
            SpecialType.System_Byte => $"Utf8Parser.TryParse(reader.ValueSpan, out byte {targetExpr}Parsed, out _)",
            SpecialType.System_SByte => $"Utf8Parser.TryParse(reader.ValueSpan, out sbyte {targetExpr}Parsed, out _)",
            SpecialType.System_Int16 => $"Utf8Parser.TryParse(reader.ValueSpan, out short {targetExpr}Parsed, out _)",
            SpecialType.System_UInt16 => $"Utf8Parser.TryParse(reader.ValueSpan, out ushort {targetExpr}Parsed, out _)",
            SpecialType.System_Int32 => $"Utf8Parser.TryParse(reader.ValueSpan, out int {targetExpr}Parsed, out _)",
            SpecialType.System_UInt32 => $"Utf8Parser.TryParse(reader.ValueSpan, out uint {targetExpr}Parsed, out _)",
            SpecialType.System_Int64 => $"Utf8Parser.TryParse(reader.ValueSpan, out long {targetExpr}Parsed, out _)",
            SpecialType.System_UInt64 => $"Utf8Parser.TryParse(reader.ValueSpan, out ulong {targetExpr}Parsed, out _)",
            SpecialType.System_Single => $"Utf8Parser.TryParse(reader.ValueSpan, out float {targetExpr}Parsed, out _)",
            SpecialType.System_Double => $"Utf8Parser.TryParse(reader.ValueSpan, out double {targetExpr}Parsed, out _)",
            SpecialType.System_Decimal => $"Utf8Parser.TryParse(reader.ValueSpan, out decimal {targetExpr}Parsed, out _)",
            _ => null
        };
    }

    private string GetUniqueVarName(string prefix)
    {
        return $"{prefix}{_variableCounter++}";
    }

    private static string GetCollectionTypeName(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
            return namedType.Name;
        if (type is IArrayTypeSymbol)
            return "Array";
        return "List";
    }

    /// <summary>
    /// Converts a source collection expression to the target collection type.
    /// The source is always a List&lt;T&gt; (for lists) or HashSet&lt;T&gt; (for sets),
    /// so we return the source directly when the target is compatible to avoid redundant copies.
    /// </summary>
    private static string ConvertToTargetCollection(ITypeSymbol targetType, ITypeSymbol elementType, string sourceExpr)
    {
        var elementTypeName = elementType.ToDisplayString();

        if (targetType is IArrayTypeSymbol)
            return $"{sourceExpr}.ToArray()";

        if (targetType is INamedTypeSymbol namedType)
        {
            return namedType.Name switch
            {
                // Concrete List<T> — assign directly, no copy needed
                "List" => sourceExpr,
                // Interface types — use T[] which is smaller than List<T> and implements IList<T>, IReadOnlyList<T>, etc.
                "IList" or "ICollection" or "IReadOnlyCollection" or "IReadOnlyList" => $"{sourceExpr}.ToArray()",
                // IEnumerable<T> — assign List directly, no need to copy
                "IEnumerable" => sourceExpr,
                // Concrete HashSet<T> — assign directly
                "HashSet" => sourceExpr,
                // Interface set types — assign HashSet directly (implements ISet<T>, IReadOnlySet<T>)
                "ISet" or "IReadOnlySet" => sourceExpr,
                "Collection" => $"new System.Collections.ObjectModel.Collection<{elementTypeName}>({sourceExpr})",
                _ => sourceExpr
            };
        }

        return sourceExpr;
    }
}
