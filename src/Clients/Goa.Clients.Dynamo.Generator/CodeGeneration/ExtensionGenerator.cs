using Goa.Clients.Dynamo.Generator.Models;

namespace Goa.Clients.Dynamo.Generator.CodeGeneration;

/// <summary>
/// Generates extension methods for DynamoDB models that delegate to DynamoMapper.
/// </summary>
public class ExtensionGenerator : ICodeGenerator
{
    private readonly bool _autoGenerateExtensions;

    public ExtensionGenerator(bool autoGenerateExtensions = false)
    {
        _autoGenerateExtensions = autoGenerateExtensions;
    }

    public string GenerateCode(IEnumerable<DynamoTypeInfo> types, GenerationContext context)
    {
        // Collect types that should have extension methods generated
        var extensionTypes = types.Where(ShouldGenerateExtension).ToList();

        if (!extensionTypes.Any())
            return string.Empty;

        // Group types by namespace
        var typesByNamespace = extensionTypes.GroupBy(t => t.Namespace).Where(x => x.Any()).ToList();

        if (!typesByNamespace.Any())
            return string.Empty;

        var builder = new CodeBuilder();
        builder.AppendLine("#nullable enable");
        builder.AppendLine("using Goa.Clients.Dynamo.Models;");

        foreach (var ns in typesByNamespace)
        {
            var targetNamespace = string.IsNullOrEmpty(ns.Key) ? "Generated" : ns.Key;
            builder.AppendLine();
            builder.AppendLine($"namespace {targetNamespace}");
            builder.OpenBrace();
            {
                builder.AppendLine("/// <summary>");
                builder.AppendLine("/// Extension methods for converting DynamoDB models to DynamoRecord.");
                builder.AppendLine("/// </summary>");
                builder.OpenBraceWithLine("public static class DynamoExtensions");

                foreach (var type in ns)
                {
                    GenerateExtensionMethod(builder, type);
                }

                builder.CloseBrace();
            }
            builder.CloseBrace();
        }

        return builder.ToString();
    }

    private bool ShouldGenerateExtension(DynamoTypeInfo type)
    {
        // Skip abstract types - they can't be instantiated directly
        if (type.IsAbstract)
            return false;

        // Check if type has [Extension] attribute directly or inherited
        var hasExtensionAttribute = type.Attributes.Any(a => a is ExtensionAttributeInfo) ||
                                    HasInheritedExtension(type);

        // Check if type has [DynamoModel] attribute (or inherits it) and auto-generate is enabled
        var hasDynamoModel = type.Attributes.Any(a => a is DynamoModelAttributeInfo) ||
                            HasInheritedDynamoModel(type);

        return hasExtensionAttribute || (_autoGenerateExtensions && hasDynamoModel);
    }

    private bool HasInheritedExtension(DynamoTypeInfo type)
    {
        var current = type.BaseType;
        while (current != null)
        {
            if (current.Attributes.Any(a => a is ExtensionAttributeInfo))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    private bool HasInheritedDynamoModel(DynamoTypeInfo type)
    {
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

    private void GenerateExtensionMethod(CodeBuilder builder, DynamoTypeInfo type)
    {
        var normalizedTypeName = NamingHelpers.NormalizeTypeName(type.Name);

        builder.AppendLine();
        builder.AppendLine("/// <summary>");
        builder.AppendLine($"/// Converts the <see cref=\"{type.FullName}\"/> to a <see cref=\"DynamoRecord\"/>.");
        builder.AppendLine("/// </summary>");
        builder.AppendLine("/// <param name=\"model\">The model to convert.</param>");
        builder.AppendLine("/// <returns>A DynamoRecord representation of the model.</returns>");
        builder.AppendLine($"public static DynamoRecord ToDynamoRecord(this {type.FullName} model)");
        builder.Indent();
        builder.AppendLine($"=> DynamoMapper.{normalizedTypeName}.ToDynamoRecord(model);");
        builder.Unindent();
    }
}
