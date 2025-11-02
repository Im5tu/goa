using Goa.Clients.Dynamo.Generator.Models;
using Goa.Clients.Dynamo.Generator.TypeHandlers;

namespace Goa.Clients.Dynamo.Generator.CodeGeneration;

/// <summary>
/// Generates DynamoKeyFactory classes for creating primary and GSI keys.
/// </summary>
public class KeyFactoryGenerator : ICodeGenerator
{
    private readonly TypeHandlerRegistry _typeHandlerRegistry;

    public KeyFactoryGenerator(TypeHandlerRegistry typeHandlerRegistry)
    {
        _typeHandlerRegistry = typeHandlerRegistry;
    }

    public string GenerateCode(IEnumerable<DynamoTypeInfo> types, GenerationContext context)
    {
        // Group types by namespace to generate separate files
        // Include types that have DynamoModel either directly or inherited from base types
        var typesWithDynamoModel = types.Where(HasDynamoModelAttribute).ToList();
        var typesByNamespace = typesWithDynamoModel.GroupBy(t => t.Namespace).ToList();

        if (!typesByNamespace.Any())
            return string.Empty;

        var builder = new CodeBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Collections.Generic;");
        builder.AppendLine("using Goa.Clients.Dynamo;");

        foreach (var ns in typesByNamespace)
        {
            var targetNamespace = string.IsNullOrEmpty(ns.Key) ? "Generated" : ns.Key;
            builder.AppendLine();
            builder.AppendLine($"namespace {targetNamespace}");
            builder.OpenBrace();
            {
                builder.OpenBraceWithLine("public static class DynamoKeyFactory");

                foreach (var type in ns)
                {
                    var dynamoModelAttr = GetDynamoModelAttribute(type);
                    if (dynamoModelAttr == null)
                        continue; // Skip types without DynamoModel attribute

                    GenerateTypeKeyFactory(builder, type, dynamoModelAttr);
                }

                builder.CloseBrace();
            }
            builder.CloseBrace();
        }
        return builder.ToString();
    }

    private void GenerateTypeKeyFactory(CodeBuilder builder, DynamoTypeInfo type, DynamoModelAttributeInfo dynamoAttr)
    {
        var normalizedTypeName = NamingHelpers.NormalizeTypeName(type.Name);

        builder.AppendLine();
        builder.OpenBraceWithLine($"public static class {normalizedTypeName}");

        // Generate primary key methods
        GeneratePrimaryKeyMethods(builder, type, dynamoAttr);

        // Generate GSI key methods
        var gsiAttributes = type.Attributes.OfType<GSIAttributeInfo>().ToList();
        foreach (var gsiAttr in gsiAttributes)
        {
            GenerateGSIKeyMethods(builder, type, gsiAttr);
        }

        builder.CloseBrace();
    }

    private void GeneratePrimaryKeyMethods(CodeBuilder builder, DynamoTypeInfo type, DynamoModelAttributeInfo dynamoAttr)
    {
        // Generate PK method
        var pkPlaceholders = NamingHelpers.ExtractPlaceholders(dynamoAttr.PK);
        GenerateKeyMethod(builder, "PK", dynamoAttr.PK, pkPlaceholders, type);

        // Generate SK method
        var skPlaceholders = NamingHelpers.ExtractPlaceholders(dynamoAttr.SK);
        GenerateKeyMethod(builder, "SK", dynamoAttr.SK, skPlaceholders, type);

        // Only generate combined key method if it makes sense (not already covered by individual methods)
        // Skip it for now to avoid complexity - users can call PK() and SK() separately
    }

    private void GenerateGSIKeyMethods(CodeBuilder builder, DynamoTypeInfo type, GSIAttributeInfo gsiAttr)
    {
        var normalizedIndexName = NormalizeGSIIndexName(gsiAttr.IndexName);

        // Generate GSI index name method
        builder.AppendLine($"public static string GSI_{normalizedIndexName}_Name() => \"{gsiAttr.IndexName}\";");

        // Generate GSI PK method with GSI_ prefix
        var pkPlaceholders = NamingHelpers.ExtractPlaceholders(gsiAttr.PK);
        GenerateKeyMethod(builder, $"GSI_{normalizedIndexName}_PK", gsiAttr.PK, pkPlaceholders, type);

        // Generate GSI SK method with GSI_ prefix
        var skPlaceholders = NamingHelpers.ExtractPlaceholders(gsiAttr.SK);
        GenerateKeyMethod(builder, $"GSI_{normalizedIndexName}_SK", gsiAttr.SK, skPlaceholders, type);
    }

    /// <summary>
    /// Normalizes GSI index names for method generation, avoiding GSI_GSI_ prefixes.
    /// Examples: "gsi-1" -> "1", "my-index" -> "My_Index", "GSI1" -> "1"
    /// </summary>
    private string NormalizeGSIIndexName(string indexName)
    {
        // Remove common GSI prefixes to avoid GSI_GSI_ pattern
        var normalized = indexName;

        // Remove GSI prefix (case insensitive)
        if (normalized.StartsWith("GSI", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(3);
        }
        else if (normalized.StartsWith("gsi-", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(4);
        }
        else if (normalized.StartsWith("gsi_", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(4);
        }

        // Replace hyphens and spaces with underscores
        normalized = normalized.Replace("-", "_").Replace(" ", "_");

        // Convert to PascalCase for each segment
        var segments = normalized.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        var result = string.Join("_", segments.Select(segment =>
            char.ToUpper(segment[0]) + (segment.Length > 1 ? segment.Substring(1) : "")));

        return string.IsNullOrEmpty(result) ? "Index" : result;
    }

    private void GenerateKeyMethod(CodeBuilder builder, string methodName, string pattern, List<string> placeholders, DynamoTypeInfo type)
    {
        if (!placeholders.Any())
        {
            // Static key
            builder.AppendLine($"public static string {methodName}() => \"{pattern}\";");
            return;
        }

        // Build parameter list
        var parameters = new List<string>();
        var replacements = new Dictionary<string, string>();

        foreach (var placeholder in placeholders)
        {
            var property = FindProperty(type, placeholder);
            if (property != null)
            {
                var paramType = property.Type.ToDisplayString();
                var paramName = NamingHelpers.ToVariableName(placeholder);
                parameters.Add($"{paramType} {paramName}");

                // Generate formatting code for this property
                var formatting = _typeHandlerRegistry.GenerateKeyFormatting(property);
                var formattedValue = formatting.Replace($"model.{property.Name}", paramName);
                replacements[placeholder] = $"{{ {formattedValue} }}";
            }
            else
            {
                // Fallback for missing properties
                parameters.Add($"object {NamingHelpers.ToVariableName(placeholder)}");
                replacements[placeholder] = $"{{ {NamingHelpers.ToVariableName(placeholder)}?.ToString() ?? \"\" }}";
            }
        }

        var formattedPattern = NamingHelpers.FormatKeyPattern(pattern, replacements);

        builder.AppendLine($"public static string {methodName}({string.Join(", ", parameters)})");
        builder.Indent().AppendLine($"=> $\"{formattedPattern}\";").Unindent();
    }

    private void GenerateCombinedKeyMethod(CodeBuilder builder, DynamoTypeInfo type, DynamoModelAttributeInfo dynamoAttr, List<string> placeholders)
    {
        if (!placeholders.Any())
        {
            // Static keys
            builder.AppendLine($"public static (string PK, string SK) Key() => (\"{dynamoAttr.PK}\", \"{dynamoAttr.SK}\");");
            return;
        }

        var parameters = new List<string>();
        foreach (var placeholder in placeholders)
        {
            var property = FindProperty(type, placeholder);
            if (property != null)
            {
                var paramType = property.Type.ToDisplayString();
                var paramName = NamingHelpers.ToVariableName(placeholder);
                parameters.Add($"{paramType} {paramName}");
            }
            else
            {
                parameters.Add($"object {NamingHelpers.ToVariableName(placeholder)}");
            }
        }

        var pkCall = $"PK({string.Join(", ", placeholders.Select(NamingHelpers.ToVariableName))})";
        var skCall = $"SK({string.Join(", ", placeholders.Select(NamingHelpers.ToVariableName))})";

        builder.AppendLine($"public static (string PK, string SK) Key({string.Join(", ", parameters)})");
        builder.Indent().AppendLine($"=> ({pkCall}, {skCall});").Unindent();
    }

    private void GenerateCombinedGSIKeyMethod(CodeBuilder builder, DynamoTypeInfo type, GSIAttributeInfo gsiAttr, List<string> placeholders, string indexName)
    {
        if (!placeholders.Any())
        {
            // Static keys
            builder.AppendLine($"public static (string PK, string SK) {indexName}Key() => (\"{gsiAttr.PK}\", \"{gsiAttr.SK}\");");
            return;
        }

        var parameters = new List<string>();
        foreach (var placeholder in placeholders)
        {
            var property = FindProperty(type, placeholder);
            if (property != null)
            {
                var paramType = property.Type.ToDisplayString();
                var paramName = NamingHelpers.ToVariableName(placeholder);
                parameters.Add($"{paramType} {paramName}");
            }
            else
            {
                parameters.Add($"object {NamingHelpers.ToVariableName(placeholder)}");
            }
        }

        var pkCall = $"{indexName}PK({string.Join(", ", placeholders.Select(NamingHelpers.ToVariableName))})";
        var skCall = $"{indexName}SK({string.Join(", ", placeholders.Select(NamingHelpers.ToVariableName))})";

        builder.AppendLine($"public static (string PK, string SK) {indexName}Key({string.Join(", ", parameters)})");
        builder.Indent().AppendLine($"=> ({pkCall}, {skCall});").Unindent();
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

    /// <summary>
    /// Checks if a type has a DynamoModel attribute either directly or inherited from base types.
    /// </summary>
    private bool HasDynamoModelAttribute(DynamoTypeInfo type)
    {
        var current = type;
        while (current != null)
        {
            if (current.Attributes.OfType<DynamoModelAttributeInfo>().Any())
                return true;
            current = current.BaseType;
        }
        return false;
    }

    /// <summary>
    /// Gets the DynamoModel attribute from the type or its base types.
    /// </summary>
    private DynamoModelAttributeInfo? GetDynamoModelAttribute(DynamoTypeInfo type)
    {
        var current = type;
        while (current != null)
        {
            var attr = current.Attributes.OfType<DynamoModelAttributeInfo>().FirstOrDefault();
            if (attr != null)
                return attr;
            current = current.BaseType;
        }
        return null;
    }
}
