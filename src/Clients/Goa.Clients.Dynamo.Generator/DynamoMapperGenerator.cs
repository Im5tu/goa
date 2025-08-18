using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;

namespace Goa.Clients.Dynamo.Generator;

[Generator]
public class DynamoMapperGenerator : ISourceGenerator
{
    private const string DynamoAttributeName = "Goa.Clients.Dynamo.DynamoModelAttribute";
    private const string GSIAttributeName = "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute";
    private static readonly Regex PlaceholderRegex = new(@"<([^>]+)>", RegexOptions.Compiled);

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var allTypes = context.Compilation.SyntaxTrees
            .SelectMany(st => st.GetRoot().DescendantNodes())
            .OfType<TypeDeclarationSyntax>()
            .Select(decl => context.Compilation.GetSemanticModel(decl.SyntaxTree).GetDeclaredSymbol(decl))
            .OfType<INamedTypeSymbol>()
            .ToList();

        var models = allTypes
            .Where(symbol => HasDynamoModelAttribute(symbol))
            .Where(symbol => !symbol.IsAbstract || symbol.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == DynamoAttributeName))
            .ToList();

        if (!models.Any()) return;

        var availableConversions = new Dictionary<string, string>();
        var typeRegistry = new Dictionary<string, List<INamedTypeSymbol>>();
        var keyFactoryBuilder = new StringBuilder();
        var mapperBuilder = new StringBuilder();

        // First pass: Build type registry for inheritance mapping
        foreach (var model in models)
        {
            if (!model.IsAbstract)
            {
                // Find the first abstract base type in the inheritance chain that has DynamoModelAttribute
                var current = model.BaseType;
                while (current != null)
                {
                    if (current.IsAbstract && current.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == DynamoAttributeName))
                    {
                        var abstractTypeName = current.ToDisplayString();
                        if (!typeRegistry.ContainsKey(abstractTypeName))
                            typeRegistry[abstractTypeName] = new List<INamedTypeSymbol>();

                        typeRegistry[abstractTypeName].Add(model);
                        break; // Only add to the immediate abstract parent
                    }
                    current = current.BaseType;
                }
            }
        }

        // Collect all referenced complex types
        var allTypesToGenerate = new HashSet<INamedTypeSymbol>(models, SymbolEqualityComparer.Default);
        foreach (var model in models.ToList())
        {
            CollectReferencedComplexTypes(model, allTypesToGenerate, context.Compilation);
        }

        // Second pass: Generate code for each model
        foreach (var model in allTypesToGenerate)
        {
            var dynamoModelAttr = GetDynamoModelAttribute(model);
            var gsiAttributes = model.GetAttributes().Where(attr => attr.AttributeClass?.ToDisplayString() == GSIAttributeName).ToList();

            // Skip GSI validation for types without DynamoModel attribute (these are referenced complex types)
            if (dynamoModelAttr != null && gsiAttributes.Count > 5)
            {
                var descriptor = new DiagnosticDescriptor(
                    id: "DYNAMO001",
                    title: "Too many GlobalSecondaryIndex attributes",
                    messageFormat: "Model '{0}' has {1} GlobalSecondaryIndex attributes. Maximum allowed is 5.",
                    category: "DynamoDB",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    description: "DynamoDB supports a maximum of 5 Global Secondary Indexes per table.");

                var location = Location.Create(
                    model.Locations.FirstOrDefault()?.SourceTree ?? context.Compilation.SyntaxTrees.First(),
                    model.Locations.FirstOrDefault()?.SourceSpan ?? default);

                var diagnostic = Diagnostic.Create(descriptor, location, model.Name, gsiAttributes.Count);
                context.ReportDiagnostic(diagnostic);
                continue;
            }

            var className = model.Name;
            var namespaceName = model.ContainingNamespace.ToDisplayString();

            // For types without DynamoModel attribute, create a minimal ModelInfo for mapper generation only
            ModelInfo modelInfo;
            if (dynamoModelAttr != null)
            {
                var info = ExtractModelInfo(dynamoModelAttr, gsiAttributes, model, context);
                if (info == null)
                    continue;

                modelInfo = info;
            }
            else
            {
                // Create minimal model info for complex types (no PK/SK/GSI)
                modelInfo = new ModelInfo();
            }

            // Only generate key factory for types with DynamoModel attribute
            if (dynamoModelAttr != null)
            {
                GenerateKeyFactory(keyFactoryBuilder, model, modelInfo, namespaceName);
            }

            GenerateMapper(mapperBuilder, model, modelInfo, namespaceName, typeRegistry, context);

            availableConversions.Add(model.ToDisplayString(), className);
        }

        var firstNamespace = availableConversions.First().Key.Substring(0, availableConversions.First().Key.LastIndexOf('.'));
        var generatedNamespace = $"{firstNamespace}.Generated";

        context.AddSource("DynamoKeyFactory.g.cs", SourceText.From(BuildKeyFactory(keyFactoryBuilder, generatedNamespace), Encoding.UTF8));
        context.AddSource("DynamoMapper.g.cs", SourceText.From(BuildMapper(mapperBuilder, generatedNamespace), Encoding.UTF8));
    }

    private ModelInfo? ExtractModelInfo(AttributeData dynamoModelAttr, List<AttributeData> gsiAttributes, INamedTypeSymbol model, GeneratorExecutionContext context)
    {
        var modelInfo = new ModelInfo();

        // Extract DynamoModel properties
        foreach (var namedArg in dynamoModelAttr.NamedArguments)
        {
            switch (namedArg.Key)
            {
                case "PK":
                    modelInfo.PK = namedArg.Value.Value?.ToString() ?? string.Empty;
                    break;
                case "SK":
                    modelInfo.SK = namedArg.Value.Value?.ToString() ?? string.Empty;
                    break;
                case "PKName":
                    modelInfo.PKName = namedArg.Value.Value?.ToString() ?? "PK";
                    break;
                case "SKName":
                    modelInfo.SKName = namedArg.Value.Value?.ToString() ?? "SK";
                    break;
            }
        }

        if (string.IsNullOrEmpty(modelInfo.PK))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO002",
                title: "Missing PK property in DynamoModel attribute",
                messageFormat: "Model '{0}' requires a PK property in DynamoModelAttribute.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "All DynamoDB models must specify a partition key pattern.");

            var location = Location.Create(
                model.Locations.FirstOrDefault()?.SourceTree ?? context.Compilation.SyntaxTrees.First(),
                model.Locations.FirstOrDefault()?.SourceSpan ?? default);

            var diagnostic = Diagnostic.Create(descriptor, location, model.Name);
            context.ReportDiagnostic(diagnostic);
            return null;
        }

        if (string.IsNullOrEmpty(modelInfo.SK))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO003",
                title: "Missing SK property in DynamoModel attribute",
                messageFormat: "Model '{0}' requires an SK property in DynamoModelAttribute.",
                category: "DynamoDB",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true,
                description: "All DynamoDB models must specify a sort key pattern.");

            var location = Location.Create(
                model.Locations.FirstOrDefault()?.SourceTree ?? context.Compilation.SyntaxTrees.First(),
                model.Locations.FirstOrDefault()?.SourceSpan ?? default);

            var diagnostic = Diagnostic.Create(descriptor, location, model.Name);
            context.ReportDiagnostic(diagnostic);
            return null;
        }

        // Extract GSI properties
        for (int i = 0; i < gsiAttributes.Count; i++)
        {
            var gsi = new GSIInfo { Index = i + 1 };

            foreach (var namedArg in gsiAttributes[i].NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Name":
                        gsi.Name = namedArg.Value.Value?.ToString() ?? string.Empty;
                        break;
                    case "PK":
                        gsi.PK = namedArg.Value.Value?.ToString() ?? string.Empty;
                        break;
                    case "SK":
                        gsi.SK = namedArg.Value.Value?.ToString() ?? string.Empty;
                        break;
                    case "PKName":
                        gsi.PKName = namedArg.Value.Value?.ToString() ?? $"GSI_{i + 1}_PK";
                        break;
                    case "SKName":
                        gsi.SKName = namedArg.Value.Value?.ToString() ?? $"GSI_{i + 1}_SK";
                        break;
                }
            }

            if (string.IsNullOrEmpty(gsi.PKName))
                gsi.PKName = $"GSI_{i + 1}_PK";
            if (string.IsNullOrEmpty(gsi.SKName))
                gsi.SKName = $"GSI_{i + 1}_SK";

            if (string.IsNullOrEmpty(gsi.Name))
            {
                var descriptor = new DiagnosticDescriptor(
                    id: "DYNAMO004",
                    title: "Missing Name property in GlobalSecondaryIndex attribute",
                    messageFormat: "Model '{0}' has a GlobalSecondaryIndex at position {1} that requires a Name property.",
                    category: "DynamoDB",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    description: "All GlobalSecondaryIndex attributes must specify a name.");

                var location = Location.Create(
                    model.Locations.FirstOrDefault()?.SourceTree ?? context.Compilation.SyntaxTrees.First(),
                    model.Locations.FirstOrDefault()?.SourceSpan ?? default);

                var diagnostic = Diagnostic.Create(descriptor, location, model.Name, i + 1);
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            if (string.IsNullOrEmpty(gsi.PK))
            {
                var descriptor = new DiagnosticDescriptor(
                    id: "DYNAMO005",
                    title: "Missing PK property in GlobalSecondaryIndex attribute",
                    messageFormat: "Model '{0}' has GlobalSecondaryIndex '{1}' that requires a PK property.",
                    category: "DynamoDB",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    description: "All GlobalSecondaryIndex attributes must specify a partition key pattern.");

                var location = Location.Create(
                    model.Locations.FirstOrDefault()?.SourceTree ?? context.Compilation.SyntaxTrees.First(),
                    model.Locations.FirstOrDefault()?.SourceSpan ?? default);

                var diagnostic = Diagnostic.Create(descriptor, location, model.Name, gsi.Name);
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            if (string.IsNullOrEmpty(gsi.SK))
            {
                var descriptor = new DiagnosticDescriptor(
                    id: "DYNAMO006",
                    title: "Missing SK property in GlobalSecondaryIndex attribute",
                    messageFormat: "Model '{0}' has GlobalSecondaryIndex '{1}' that requires an SK property.",
                    category: "DynamoDB",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    description: "All GlobalSecondaryIndex attributes must specify a sort key pattern.");

                var location = Location.Create(
                    model.Locations.FirstOrDefault()?.SourceTree ?? context.Compilation.SyntaxTrees.First(),
                    model.Locations.FirstOrDefault()?.SourceSpan ?? default);

                var diagnostic = Diagnostic.Create(descriptor, location, model.Name, gsi.Name);
                context.ReportDiagnostic(diagnostic);
                return null;
            }

            modelInfo.GSIs.Add(gsi);
        }

        return modelInfo;
    }

    private string NormalizeCSharpIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // Replace invalid characters with underscores
        var normalized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "_");

        // Ensure it starts with a letter or underscore
        if (char.IsDigit(normalized[0]))
        {
            normalized = "_" + normalized;
        }

        return normalized;
    }

    private string FormatAttributeKey(string attributeKeyPattern, INamedTypeSymbol model, GeneratorExecutionContext? context = null)
    {
        var matches = PlaceholderRegex.Matches(attributeKeyPattern);

        // If there are no placeholders, return the original string without modifications
        if (matches.Count == 0)
        {
            return $"\"{attributeKeyPattern}\""; // No placeholders, return as a simple string
        }

        // Replace placeholders with actual property values using string interpolation
        var result = PlaceholderRegex.Replace(attributeKeyPattern, match =>
        {
            var propertyName = match.Groups[1].Value;
            var propertySymbol = GetPropertyIncludingInherited(model, propertyName);
            if (propertySymbol != null)
            {
                // Handle different property types appropriately
                if (propertySymbol.Type.Name == nameof(DateTime))
                {
                    return $"{{model.{propertyName}.ToString(\"yyyy-MM-dd\")}}";
                }
                else if (propertySymbol.Type.Name == nameof(DateTimeOffset))
                {
                    return $"{{model.{propertyName}.ToString(\"yyyy-MM-dd\")}}";
                }
                else
                {
                    return $"{{model.{propertyName}}}";
                }
            }
            else
            {
                // Property not found - report diagnostic error
                if (context != null)
                {
                    var descriptor = new DiagnosticDescriptor(
                        id: "DYNAMO005",
                        title: "Property not found in placeholder",
                        messageFormat: "Property '{0}' referenced in key pattern '{1}' does not exist on type '{2}' or any of its base classes.",
                        category: "DynamoDB",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        description: "All placeholders in PK/SK patterns must reference existing properties on the model or its base classes.");

                    var location = Location.Create(
                        model.Locations.FirstOrDefault()?.SourceTree ?? context.Value.Compilation.SyntaxTrees.First(),
                        model.Locations.FirstOrDefault()?.SourceSpan ?? default);

                    var diagnostic = Diagnostic.Create(descriptor, location, propertyName, attributeKeyPattern, model.Name);
                    context.Value.ReportDiagnostic(diagnostic);
                }
            }
            return match.Value; // Keep original if property not found
        });

        // Return the formatted string with placeholders replaced by actual property values
        return $"$\"{result}\"";
    }

    private IPropertySymbol? GetPropertyIncludingInherited(INamedTypeSymbol type, string propertyName)
    {
        // First check direct members
        var property = type.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == propertyName);
        if (property != null)
            return property;

        // Then check base types
        var current = type.BaseType;
        while (current != null)
        {
            property = current.GetMembers().OfType<IPropertySymbol>().FirstOrDefault(p => p.Name == propertyName);
            if (property != null)
                return property;
            current = current.BaseType;
        }

        return null;
    }

    private class ModelInfo
    {
        public string PK { get; set; } = string.Empty;
        public string SK { get; set; } = string.Empty;
        public string PKName { get; set; } = "PK";
        public string SKName { get; set; } = "SK";
        public List<GSIInfo> GSIs { get; set; } = new();
    }

    private class GSIInfo
    {
        public string Name { get; set; } = string.Empty;
        public string PK { get; set; } = string.Empty;
        public string SK { get; set; } = string.Empty;
        public string PKName { get; set; } = string.Empty;
        public string SKName { get; set; } = string.Empty;
        public int Index { get; set; }
    }

    private static class AttributeValueConverters
    {
        public static string ForString(string propertyName) => $"new AttributeValue {{ S = model.{propertyName} ?? string.Empty }}";
        public static string ForStringSet(string propertyName) => $"new AttributeValue {{ SS = model.{propertyName}?.ToList() ?? new List<string>() }}";
        public static string ForNumber(string propertyName) => $"new AttributeValue {{ N = model.{propertyName}.ToString() }}";
        public static string ForNumberSet(string propertyName) => $"new AttributeValue {{ NS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}";
        public static string ForBool(string propertyName) => $"new AttributeValue {{ BOOL = model.{propertyName} }}";
        public static string ForBoolSet(string propertyName) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}";
        public static string ForDateTime(string propertyName) => $"new AttributeValue {{ S = model.{propertyName}.ToString(\"o\") }}";
        public static string ForNullableDateTime(string propertyName) => $"model.{propertyName}.HasValue ? new AttributeValue {{ S = model.{propertyName}.Value.ToString(\"o\") }} : new AttributeValue {{ NULL = true }}";
        public static string ForDateTimeSet(string propertyName) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString(\"o\")).ToList() ?? new List<string>() }}";
        public static string ForEnum(string propertyName) => $"new AttributeValue {{ S = model.{propertyName}.ToString() }}";
        public static string ForEnumSet(string propertyName) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}";
        public static string ForChar(string propertyName) => $"new AttributeValue {{ S = model.{propertyName}.ToString() }}";
        public static string ForTimeSpan(string propertyName) => $"new AttributeValue {{ S = model.{propertyName}.ToString() }}";
        public static string ForGuid(string propertyName) => $"new AttributeValue {{ S = model.{propertyName}.ToString() }}";
        public static string ForGuidSet(string propertyName) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}";
        public static string ForTimeSpanSet(string propertyName) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString()).ToList() ?? new List<string>() }}";
        public static string ForDateTimeOffsetSet(string propertyName) => $"new AttributeValue {{ SS = model.{propertyName}?.Select(x => x.ToString(\"o\")).ToList() ?? new List<string>() }}";
        public static string ForUnixTimestampSeconds(string propertyName) => $"new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}).ToUnixTimeSeconds().ToString() }}";
        public static string ForUnixTimestampMilliseconds(string propertyName) => $"new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}).ToUnixTimeMilliseconds().ToString() }}";
        public static string ForNullableUnixTimestampSeconds(string propertyName) => $"model.{propertyName}.HasValue ? new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}.Value).ToUnixTimeSeconds().ToString() }} : new AttributeValue {{ NULL = true }}";
        public static string ForNullableUnixTimestampMilliseconds(string propertyName) => $"model.{propertyName}.HasValue ? new AttributeValue {{ N = ((DateTimeOffset)model.{propertyName}.Value).ToUnixTimeMilliseconds().ToString() }} : new AttributeValue {{ NULL = true }}";
        public static string ForStringDictionary(string propertyName) => $"new AttributeValue {{ M = model.{propertyName}?.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue {{ S = kvp.Value }}) ?? new Dictionary<string, AttributeValue>() }}";
        public static string ForStringIntDictionary(string propertyName) => $"new AttributeValue {{ M = model.{propertyName}?.ToDictionary(kvp => kvp.Key, kvp => new AttributeValue {{ N = kvp.Value.ToString() }}) ?? new Dictionary<string, AttributeValue>() }}";
        public static string ForComplexType(string propertyName, string typeName) => $"model.{propertyName} != null ? new AttributeValue {{ M = DynamoMapper.{typeName}.ToDynamoRecord(model.{propertyName}) }} : new AttributeValue {{ NULL = true }}";
    }

    private void GenerateKeyFactory(StringBuilder keyFactoryBuilder, INamedTypeSymbol model, ModelInfo modelInfo, string namespaceName)
    {
        var normalizedModelName = NormalizeCSharpIdentifier(model.Name);

        keyFactoryBuilder.AppendLine($"    public static class {normalizedModelName}");
        keyFactoryBuilder.AppendLine("    {");

        // Generate PK method
        var pkParams = GetPropertiesFromPattern(modelInfo.PK, model);
        var pkParamString = string.Join(", ", pkParams.Select(p => GetParameterTypeAndName(p, model)));
        var pkFormatString = FormatKeyPatternWithTypes(modelInfo.PK, model);
        keyFactoryBuilder.AppendLine($"        public static string PK({pkParamString}) => $\"{pkFormatString}\";");

        // Generate SK method
        var skParams = GetPropertiesFromPattern(modelInfo.SK, model);
        var skParamString = string.Join(", ", skParams.Select(p => GetParameterTypeAndName(p, model)));
        var skFormatString = FormatKeyPatternWithTypes(modelInfo.SK, model);
        keyFactoryBuilder.AppendLine($"        public static string SK({skParamString}) => $\"{skFormatString}\";");

        // Generate GSI methods
        foreach (var gsi in modelInfo.GSIs)
        {
            var normalizedGsiName = NormalizeCSharpIdentifier(gsi.Name);

            // GSI PK method
            var gsiPkParams = GetPropertiesFromPattern(gsi.PK, model);
            var gsiPkParamString = string.Join(", ", gsiPkParams.Select(p => GetParameterTypeAndName(p, model)));
            var gsiPkFormatString = FormatKeyPatternWithTypes(gsi.PK, model);
            keyFactoryBuilder.AppendLine($"        public static string GSI_{normalizedGsiName}_PK({gsiPkParamString}) => $\"{gsiPkFormatString}\";");

            // GSI SK method
            var gsiSkParams = GetPropertiesFromPattern(gsi.SK, model);
            var gsiSkParamString = string.Join(", ", gsiSkParams.Select(p => GetParameterTypeAndName(p, model)));
            var gsiSkFormatString = FormatKeyPatternWithTypes(gsi.SK, model);
            keyFactoryBuilder.AppendLine($"        public static string GSI_{normalizedGsiName}_SK({gsiSkParamString}) => $\"{gsiSkFormatString}\";");

            // GSI IndexName method
            keyFactoryBuilder.AppendLine($"        public static string GSI_{normalizedGsiName}_IndexName() => \"{gsi.Name}\";");
        }

        keyFactoryBuilder.AppendLine("    }");
        keyFactoryBuilder.AppendLine();
    }

    private string FormatKeyPattern(string pattern, IEnumerable<string> properties)
    {
        var result = pattern;
        foreach (var prop in properties)
        {
            result = result.Replace($"<{prop}>", $"{{{prop.ToLowerInvariant()}}}");
        }
        return result;
    }

    private string FormatKeyPatternWithTypes(string pattern, INamedTypeSymbol model)
    {
        var result = pattern;
        var matches = PlaceholderRegex.Matches(pattern);

        foreach (Match match in matches)
        {
            var propertyName = match.Groups[1].Value;
            var propertySymbol = GetPropertyIncludingInherited(model, propertyName);
            if (propertySymbol != null)
            {
                string placeholder;
                if (propertySymbol.Type.Name == nameof(DateTime))
                {
                    // Check for UnixTimestampAttribute
                    var unixTimestampAttr = propertySymbol.GetAttributes().FirstOrDefault(attr => 
                        attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.UnixTimestampAttribute");
                    
                    if (unixTimestampAttr != null)
                    {
                        // For unix timestamps in composite keys, use direct timestamp value
                        var formatArg = unixTimestampAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Format");
                        var isMilliseconds = formatArg.Value.Value?.ToString() == "1"; // Milliseconds = 1
                        
                        placeholder = isMilliseconds 
                            ? $"{{((DateTimeOffset){propertyName.ToLowerInvariant()}).ToUnixTimeMilliseconds()}}"
                            : $"{{((DateTimeOffset){propertyName.ToLowerInvariant()}).ToUnixTimeSeconds()}}";
                    }
                    else
                    {
                        placeholder = $"{{{propertyName.ToLowerInvariant()}:yyyy-MM-dd}}";
                    }
                }
                else if (propertySymbol.Type.Name == nameof(DateTimeOffset))
                {
                    // Check for UnixTimestampAttribute
                    var unixTimestampAttr = propertySymbol.GetAttributes().FirstOrDefault(attr => 
                        attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.UnixTimestampAttribute");
                    
                    if (unixTimestampAttr != null)
                    {
                        // For unix timestamps in composite keys, use direct timestamp value
                        var formatArg = unixTimestampAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Format");
                        var isMilliseconds = formatArg.Value.Value?.ToString() == "1"; // Milliseconds = 1
                        
                        placeholder = isMilliseconds 
                            ? $"{{{propertyName.ToLowerInvariant()}.ToUnixTimeMilliseconds()}}"
                            : $"{{{propertyName.ToLowerInvariant()}.ToUnixTimeSeconds()}}";
                    }
                    else
                    {
                        placeholder = $"{{{propertyName.ToLowerInvariant()}:yyyy-MM-dd}}";
                    }
                }
                else
                {
                    placeholder = $"{{{propertyName.ToLowerInvariant()}}}";
                }
                result = result.Replace($"<{propertyName}>", placeholder);
            }
        }
        return result;
    }

    private string GetParameterTypeAndName(string propertyName, INamedTypeSymbol model)
    {
        var propertySymbol = GetPropertyIncludingInherited(model, propertyName);
        if (propertySymbol != null)
        {
            if (propertySymbol.Type.Name == nameof(DateTime))
            {
                return $"DateTime {propertyName.ToLowerInvariant()}";
            }
            else if (propertySymbol.Type.Name == nameof(DateTimeOffset))
            {
                return $"DateTimeOffset {propertyName.ToLowerInvariant()}";
            }
        }
        return $"string {propertyName.ToLowerInvariant()}";
    }

    private void GenerateMapper(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo, string namespaceName, Dictionary<string, List<INamedTypeSymbol>> typeRegistry, GeneratorExecutionContext context)
    {
        var normalizedModelName = NormalizeCSharpIdentifier(model.Name);

        mapperBuilder.AppendLine($"    public static class {normalizedModelName}");
        mapperBuilder.AppendLine("    {");

        // Generate ToDynamoRecord method
        if (model.IsAbstract)
        {
            // Handle abstract types - use switch statement based on actual type
            GenerateAbstractToDynamoRecord(mapperBuilder, model, modelInfo, typeRegistry);
        }
        else
        {
            // Handle concrete types
            GenerateConcreteToDynamoRecord(mapperBuilder, model, modelInfo, context);
        }
        mapperBuilder.AppendLine();

        // Generate FromDynamoRecord methods
        GenerateFromDynamoRecord(mapperBuilder, model, modelInfo, typeRegistry);
        GenerateFromDynamoRecordWithContext(mapperBuilder, model, modelInfo, typeRegistry);

        mapperBuilder.AppendLine("    }");
        mapperBuilder.AppendLine();
    }

    private void GenerateAbstractToDynamoRecord(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo, Dictionary<string, List<INamedTypeSymbol>> typeRegistry)
    {
        mapperBuilder.AppendLine($"        public static DynamoRecord ToDynamoRecord({model.ToDisplayString()} model)");
        mapperBuilder.AppendLine("        {");
        mapperBuilder.AppendLine("            return model switch");
        mapperBuilder.AppendLine("            {");

        var fullTypeName = model.ToDisplayString();
        if (typeRegistry.ContainsKey(fullTypeName))
        {
            foreach (var concreteType in typeRegistry[fullTypeName])
            {
                var normalizedTypeName = NormalizeCSharpIdentifier(concreteType.Name);
                mapperBuilder.AppendLine($"                {concreteType.ToDisplayString()} concrete => DynamoMapper.{normalizedTypeName}.ToDynamoRecord(concrete),");
            }
        }

        mapperBuilder.AppendLine($"                _ => throw new InvalidOperationException($\"Unknown type '{{model.GetType().FullName}}' for abstract type {model.ToDisplayString()}\")");
        mapperBuilder.AppendLine("            };");
        mapperBuilder.AppendLine("        }");
    }

    private void GenerateConcreteToDynamoRecord(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo, GeneratorExecutionContext context)
    {
        mapperBuilder.AppendLine($"        public static DynamoRecord ToDynamoRecord({model.ToDisplayString()} model)");
        mapperBuilder.AppendLine("        {");
        mapperBuilder.AppendLine("            var record = new DynamoRecord();");

        // Add PK/SK
        mapperBuilder.AppendLine($"            record[\"{modelInfo.PKName}\"] = new AttributeValue {{ S = {FormatAttributeKey(modelInfo.PK, model, context)} }};");
        mapperBuilder.AppendLine($"            record[\"{modelInfo.SKName}\"] = new AttributeValue {{ S = {FormatAttributeKey(modelInfo.SK, model, context)} }};");

        // Add GSI keys
        foreach (var gsi in modelInfo.GSIs)
        {
            mapperBuilder.AppendLine($"            record[\"{gsi.PKName}\"] = new AttributeValue {{ S = {FormatAttributeKey(gsi.PK, model, context)} }};");
            mapperBuilder.AppendLine($"            record[\"{gsi.SKName}\"] = new AttributeValue {{ S = {FormatAttributeKey(gsi.SK, model, context)} }};");
        }

        // Add Type attribute
        mapperBuilder.AppendLine($"            record[\"Type\"] = new AttributeValue {{ S = \"{model.ToDisplayString()}\" }};");

        // Add model properties
        var properties = model.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(x => x.Name != "EqualityContract")
            .ToList();

        foreach (var property in properties)
        {
            string attributeName = property.Name;
            string attributeValueLine = GetAttributeValueConversion(property);

            if (!string.IsNullOrEmpty(attributeValueLine))
            {
                mapperBuilder.AppendLine($"            record[\"{attributeName}\"] = {attributeValueLine};");
            }
        }

        mapperBuilder.AppendLine("            return record;");
        mapperBuilder.AppendLine("        }");
    }

    private string GetAttributeValueConversion(IPropertySymbol property)
    {
        string attributeName = property.Name;

        if (property.Type.SpecialType == SpecialType.System_String)
        {
            return AttributeValueConverters.ForString(attributeName);
        }
        else if (IsCollectionType(property.Type, out var elementType))
        {
            // Skip nested collections and dictionaries in collections
            if (IsCollectionType(elementType, out _) || IsDictionaryType(elementType, out _, out _))
            {
                return string.Empty;
            }

            return elementType.SpecialType switch
            {
                SpecialType.System_String => AttributeValueConverters.ForStringSet(attributeName),
                SpecialType.System_Byte or SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
                SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64 or
                SpecialType.System_Decimal or SpecialType.System_Single or SpecialType.System_Double => AttributeValueConverters.ForNumberSet(attributeName),
                SpecialType.System_Boolean => AttributeValueConverters.ForBoolSet(attributeName),
                SpecialType.System_DateTime => AttributeValueConverters.ForDateTimeSet(attributeName),
                _ when elementType.Name == nameof(Guid) => AttributeValueConverters.ForGuidSet(attributeName),
                _ when elementType.Name == nameof(TimeSpan) => AttributeValueConverters.ForTimeSpanSet(attributeName),
                _ when elementType.Name == nameof(DateTimeOffset) => AttributeValueConverters.ForDateTimeOffsetSet(attributeName),
                _ => AttributeValueConverters.ForEnumSet(attributeName)
            };
        }
        else if (property.Type.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte or
                 SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32 or
                 SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Decimal or
                 SpecialType.System_Single or SpecialType.System_Double)
        {
            return AttributeValueConverters.ForNumber(attributeName);
        }
        else if (property.Type.SpecialType == SpecialType.System_Char)
        {
            return AttributeValueConverters.ForChar(attributeName); // Treat char as string
        }
        else if (property.Type.SpecialType == SpecialType.System_Boolean)
        {
            return AttributeValueConverters.ForBool(attributeName);
        }
        else
        {
            // Handle nullable types and regular types
            var underlyingType = property.Type;
            if (IsNullableType(property.Type) && property.Type is INamedTypeSymbol nullableType)
            {
                underlyingType = nullableType.TypeArguments[0];
            }
            
            if (underlyingType.Name == nameof(DateTime))
            {
                // Check for UnixTimestampAttribute first
                var unixTimestampAttr = property.GetAttributes().FirstOrDefault(attr => 
                    attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.UnixTimestampAttribute");
                
                if (unixTimestampAttr != null)
                {
                    // Get the format from the attribute, defaulting to Seconds
                    var formatArg = unixTimestampAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Format");
                    var isMilliseconds = formatArg.Value.Value?.ToString() == "1"; // Milliseconds = 1
                    var isNullableDateTime = IsNullableType(property.Type);
                    
                    if (isNullableDateTime)
                    {
                        return isMilliseconds 
                            ? AttributeValueConverters.ForNullableUnixTimestampMilliseconds(attributeName)
                            : AttributeValueConverters.ForNullableUnixTimestampSeconds(attributeName);
                    }
                    else
                    {
                        return isMilliseconds 
                            ? AttributeValueConverters.ForUnixTimestampMilliseconds(attributeName)
                            : AttributeValueConverters.ForUnixTimestampSeconds(attributeName);
                    }
                }
                
                var isNullableDateTimeFallback = IsNullableType(property.Type);
                return isNullableDateTimeFallback 
                    ? AttributeValueConverters.ForNullableDateTime(attributeName)
                    : AttributeValueConverters.ForDateTime(attributeName);
            }
            else if (underlyingType.Name == nameof(DateTimeOffset))
            {
                // Check for UnixTimestampAttribute first
                var unixTimestampAttr = property.GetAttributes().FirstOrDefault(attr => 
                    attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.UnixTimestampAttribute");
                
                if (unixTimestampAttr != null)
                {
                    // Get the format from the attribute, defaulting to Seconds
                    var formatArg = unixTimestampAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Format");
                    var isMilliseconds = formatArg.Value.Value?.ToString() == "1"; // Milliseconds = 1
                    var isNullableDateTimeOffset = IsNullableType(property.Type);
                    
                    if (isNullableDateTimeOffset)
                    {
                        return isMilliseconds 
                            ? AttributeValueConverters.ForNullableUnixTimestampMilliseconds(attributeName)
                            : AttributeValueConverters.ForNullableUnixTimestampSeconds(attributeName);
                    }
                    else
                    {
                        return isMilliseconds 
                            ? AttributeValueConverters.ForUnixTimestampMilliseconds(attributeName)
                            : AttributeValueConverters.ForUnixTimestampSeconds(attributeName);
                    }
                }
                
                var isNullableDateTimeOffsetFallback = IsNullableType(property.Type);
                return isNullableDateTimeOffsetFallback 
                    ? AttributeValueConverters.ForNullableDateTime(attributeName)
                    : AttributeValueConverters.ForDateTime(attributeName); // Use same converter
            }
            else if (underlyingType.Name == nameof(TimeSpan))
            {
                return AttributeValueConverters.ForTimeSpan(attributeName); // Store as string
            }
            else if (underlyingType.Name == nameof(Guid))
            {
                return AttributeValueConverters.ForGuid(attributeName); // Store as string
            }
            else if (underlyingType.TypeKind == TypeKind.Enum)
            {
                return AttributeValueConverters.ForEnum(attributeName);
            }
            else if (IsDictionaryType(property.Type, out var keyType, out var valueType))
            {
                // Handle dictionary types - only support string keys for now
                if (keyType.SpecialType == SpecialType.System_String && valueType.SpecialType == SpecialType.System_String)
                {
                    return AttributeValueConverters.ForStringDictionary(attributeName);
                }
                else if (keyType.SpecialType == SpecialType.System_String && valueType.SpecialType == SpecialType.System_Int32)
                {
                    return AttributeValueConverters.ForStringIntDictionary(attributeName);
                }

                // Skip unsupported dictionary types
                return string.Empty;
            }
            else if (IsComplexType(property.Type))
            {
                // Handle complex types (records, classes)
                return AttributeValueConverters.ForComplexType(attributeName, GetNormalizedTypeName(property.Type));
            }
        }

        return string.Empty;
    }

    private IEnumerable<string> GetPropertiesFromPattern(string pattern, INamedTypeSymbol model)
    {
        var result = new List<string>();
        var matches = PlaceholderRegex.Matches(pattern);

        // Iterate over all matches (placeholders) and replace them with model property values
        foreach (Match match in matches)
        {
            var propertyName = match.Groups[1].Value;
            var propertySymbol = GetPropertyIncludingInherited(model, propertyName);
            if (propertySymbol != null)
            {
                result.Add(propertyName);
            }
        }

        return result;
    }

    private void GenerateFromDynamoRecord(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo, Dictionary<string, List<INamedTypeSymbol>> typeRegistry)
    {
        mapperBuilder.AppendLine($"        public static {model.ToDisplayString()} FromDynamoRecord(DynamoRecord record)");
        mapperBuilder.AppendLine("        {");

        // Handle abstract types
        if (model.IsAbstract)
        {
            GenerateAbstractTypeFromDynamoRecord(mapperBuilder, model, typeRegistry);
            mapperBuilder.AppendLine("        }");
            return;
        }

        // Extract PK/SK values first for better error reporting
        mapperBuilder.AppendLine($"            var pkValue = record.TryGetNullableString(\"{modelInfo.PKName}\", out var pk) ? pk : string.Empty;");
        mapperBuilder.AppendLine($"            var skValue = record.TryGetNullableString(\"{modelInfo.SKName}\", out var sk) ? sk : string.Empty;");
        mapperBuilder.AppendLine();

        // Handle concrete types
        if (model.IsRecord)
        {
            GenerateRecordFromDynamoRecord(mapperBuilder, model, modelInfo);
        }
        else
        {
            GenerateClassFromDynamoRecord(mapperBuilder, model, modelInfo);
        }
    }

    private void GenerateFromDynamoRecordWithContext(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo, Dictionary<string, List<INamedTypeSymbol>> typeRegistry)
    {
        mapperBuilder.AppendLine($"        public static {model.ToDisplayString()} FromDynamoRecord(DynamoRecord record, string? parentPkValue, string? parentSkValue)");
        mapperBuilder.AppendLine("        {");

        // Handle abstract types
        if (model.IsAbstract)
        {
            GenerateAbstractTypeFromDynamoRecordWithContext(mapperBuilder, model, typeRegistry);
            mapperBuilder.AppendLine("        }");
            return;
        }

        // Extract PK/SK values first for better error reporting, falling back to parent context
        mapperBuilder.AppendLine($"            var pkValue = record.TryGetNullableString(\"{modelInfo.PKName}\", out var pk) ? pk : parentPkValue ?? string.Empty;");
        mapperBuilder.AppendLine($"            var skValue = record.TryGetNullableString(\"{modelInfo.SKName}\", out var sk) ? sk : parentSkValue ?? string.Empty;");
        mapperBuilder.AppendLine();

        // Handle concrete types - inline the logic to avoid duplicate method generation
        if (model.IsRecord)
        {
            GenerateRecordFromDynamoRecordInline(mapperBuilder, model, modelInfo);
        }
        else
        {
            GenerateClassFromDynamoRecordInline(mapperBuilder, model, modelInfo);
        }

        mapperBuilder.AppendLine("        }");
    }

    private void GenerateAbstractTypeFromDynamoRecord(StringBuilder mapperBuilder, INamedTypeSymbol model, Dictionary<string, List<INamedTypeSymbol>> typeRegistry)
    {
        mapperBuilder.AppendLine("            if (!record.TryGetNullableString(\"Type\", out var typeValue) || typeValue == null)");
        mapperBuilder.AppendLine($"                throw new InvalidOperationException(\"Type attribute is required for abstract type {model.ToDisplayString()}\");");
        mapperBuilder.AppendLine();
        mapperBuilder.AppendLine("            return typeValue switch");
        mapperBuilder.AppendLine("            {");

        var fullTypeName = model.ToDisplayString();
        if (typeRegistry.ContainsKey(fullTypeName))
        {
            foreach (var concreteType in typeRegistry[fullTypeName])
            {
                var normalizedTypeName = NormalizeCSharpIdentifier(concreteType.Name);
                mapperBuilder.AppendLine($"                \"{concreteType.ToDisplayString()}\" => DynamoMapper.{normalizedTypeName}.FromDynamoRecord(record),");
            }
        }

        mapperBuilder.AppendLine($"                _ => throw new InvalidOperationException($\"Unknown type '{{typeValue}}' for abstract type {model.ToDisplayString()}\")");
        mapperBuilder.AppendLine("            };");
    }

    private void GenerateAbstractTypeFromDynamoRecordWithContext(StringBuilder mapperBuilder, INamedTypeSymbol model, Dictionary<string, List<INamedTypeSymbol>> typeRegistry)
    {
        mapperBuilder.AppendLine("            if (!record.TryGetNullableString(\"Type\", out var typeValue) || typeValue == null)");
        mapperBuilder.AppendLine($"                throw new InvalidOperationException(\"Type attribute is required for abstract type {model.ToDisplayString()}\");");
        mapperBuilder.AppendLine();
        mapperBuilder.AppendLine("            return typeValue switch");
        mapperBuilder.AppendLine("            {");

        var fullTypeName = model.ToDisplayString();
        if (typeRegistry.ContainsKey(fullTypeName))
        {
            foreach (var concreteType in typeRegistry[fullTypeName])
            {
                var normalizedTypeName = NormalizeCSharpIdentifier(concreteType.Name);
                mapperBuilder.AppendLine($"                \"{concreteType.ToDisplayString()}\" => DynamoMapper.{normalizedTypeName}.FromDynamoRecord(record, parentPkValue, parentSkValue),");
            }
        }

        mapperBuilder.AppendLine($"                _ => throw new InvalidOperationException($\"Unknown type '{{typeValue}}' for abstract type {model.ToDisplayString()}\")");
        mapperBuilder.AppendLine("            };");
    }

    private void GenerateRecordFromDynamoRecord(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        var constructor = model.Constructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Any());
        if (constructor == null)
        {
            // Handle records without constructors - use object initialization
            GenerateRecordWithoutConstructor(mapperBuilder, model, modelInfo);
            mapperBuilder.AppendLine("        }");
            return;
        }

        // Check if we have additional properties that need to be set after construction
        var constructorParams = new HashSet<string>(constructor.Parameters.Select(p => p.Name));
        var additionalProperties = model.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !constructorParams.Contains(p.Name))
            .Where(p => p.SetMethod != null)
            .Where(p => p.Name != "EqualityContract")
            .ToList();

        if (additionalProperties.Any())
        {
            // Mixed record - constructor + additional properties
            mapperBuilder.AppendLine($"            return new {model.ToDisplayString()}(");

            foreach (var param in constructor.Parameters)
            {
                string conversionExpression = GetFromDynamoRecordConversionWithExtensions(param, "pkValue", "skValue", model, modelInfo);

                mapperBuilder.Append("                ");
                mapperBuilder.Append(conversionExpression);

                if (!SymbolEqualityComparer.Default.Equals(param, constructor.Parameters.Last()))
                {
                    mapperBuilder.AppendLine(",");
                }
                else
                {
                    mapperBuilder.AppendLine();
                }
            }

            mapperBuilder.AppendLine("            )");
            mapperBuilder.AppendLine("            {");

            foreach (var property in additionalProperties)
            {
                string conversionExpression = GetFromDynamoRecordConversionWithExtensions(property.Name, property.Type, 
                    IsNullableType(property.Type) || property.NullableAnnotation == NullableAnnotation.Annotated, 
                    "pkValue", "skValue", model, modelInfo);
                
                mapperBuilder.AppendLine($"                {property.Name} = {conversionExpression},");
            }

            mapperBuilder.AppendLine("            };");
        }
        else
        {
            // Regular record with only constructor parameters
            mapperBuilder.AppendLine($"            return new {model.ToDisplayString()}(");

            foreach (var param in constructor.Parameters)
            {
                string conversionExpression = GetFromDynamoRecordConversionWithExtensions(param, "pkValue", "skValue", model, modelInfo);

                mapperBuilder.Append("                ");
                mapperBuilder.Append(conversionExpression);

                if (!SymbolEqualityComparer.Default.Equals(param, constructor.Parameters.Last()))
                {
                    mapperBuilder.AppendLine(",");
                }
                else
                {
                    mapperBuilder.AppendLine();
                }
            }

            mapperBuilder.AppendLine("            );");
        }
        mapperBuilder.AppendLine("        }");
    }

    private void GenerateClassFromDynamoRecord(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        var constructor = model.Constructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && !c.Parameters.Any());

        if (constructor != null)
        {
            // Use parameterless constructor and set properties
            mapperBuilder.AppendLine($"            var instance = new {model.ToDisplayString()}();");
            mapperBuilder.AppendLine();

            var properties = model.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
                .Where(p => p.Name != "EqualityContract")
                .ToList();

            foreach (var property in properties)
            {
                string conversionExpression = GetPropertySetterConversionWithExtensions(property, "pkValue", "skValue", model, modelInfo);
                if (!string.IsNullOrEmpty(conversionExpression))
                {
                    mapperBuilder.AppendLine($"            {conversionExpression}");
                }
            }

            mapperBuilder.AppendLine("            return instance;");
        }
        else
        {
            // Try constructor with parameters
            var paramConstructor = model.Constructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Any());
            if (paramConstructor != null)
            {
                mapperBuilder.AppendLine($"            return new {model.ToDisplayString()}(");

                foreach (var param in paramConstructor.Parameters)
                {
                    string conversionExpression = GetFromDynamoRecordConversionWithExtensions(param, "pkValue", "skValue", model, modelInfo);

                    mapperBuilder.Append("                ");
                    mapperBuilder.Append(conversionExpression);

                    if (!SymbolEqualityComparer.Default.Equals(param, paramConstructor.Parameters.Last()))
                    {
                        mapperBuilder.AppendLine(",");
                    }
                    else
                    {
                        mapperBuilder.AppendLine();
                    }
                }

                mapperBuilder.AppendLine("            );");
            }
            else
            {
                mapperBuilder.AppendLine("            // No suitable constructor found");
                mapperBuilder.AppendLine("            return default;");
            }
        }

        mapperBuilder.AppendLine("        }");
    }

    private void GenerateRecordFromDynamoRecordInline(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        var constructor = model.Constructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Any());
        if (constructor == null)
        {
            // Handle records without constructors - use object initialization
            GenerateRecordWithoutConstructorInline(mapperBuilder, model, modelInfo);
            return;
        }

        // Check if we have additional properties that need to be set after construction
        var constructorParams = new HashSet<string>(constructor.Parameters.Select(p => p.Name));
        var additionalProperties = model.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !constructorParams.Contains(p.Name))
            .Where(p => p.SetMethod != null)
            .Where(p => p.Name != "EqualityContract")
            .ToList();

        if (additionalProperties.Any())
        {
            // Mixed record - constructor + additional properties
            mapperBuilder.AppendLine($"            return new {model.ToDisplayString()}(");

            foreach (var param in constructor.Parameters)
            {
                string conversionExpression = GetFromDynamoRecordConversionWithExtensions(param, "pkValue", "skValue", model, modelInfo);

                mapperBuilder.Append("                ");
                mapperBuilder.Append(conversionExpression);

                if (!SymbolEqualityComparer.Default.Equals(param, constructor.Parameters.Last()))
                {
                    mapperBuilder.AppendLine(",");
                }
                else
                {
                    mapperBuilder.AppendLine();
                }
            }

            mapperBuilder.AppendLine("            )");
            mapperBuilder.AppendLine("            {");

            foreach (var property in additionalProperties)
            {
                string conversionExpression = GetFromDynamoRecordConversionWithExtensions(property.Name, property.Type, 
                    IsNullableType(property.Type) || property.NullableAnnotation == NullableAnnotation.Annotated, 
                    "pkValue", "skValue", model, modelInfo);
                
                mapperBuilder.AppendLine($"                {property.Name} = {conversionExpression},");
            }

            mapperBuilder.AppendLine("            };");
        }
        else
        {
            // Regular record with only constructor parameters
            mapperBuilder.AppendLine($"            return new {model.ToDisplayString()}(");

            foreach (var param in constructor.Parameters)
            {
                string conversionExpression = GetFromDynamoRecordConversionWithExtensions(param, "pkValue", "skValue", model, modelInfo);

                mapperBuilder.Append("                ");
                mapperBuilder.Append(conversionExpression);

                if (!SymbolEqualityComparer.Default.Equals(param, constructor.Parameters.Last()))
                {
                    mapperBuilder.AppendLine(",");
                }
                else
                {
                    mapperBuilder.AppendLine();
                }
            }

            mapperBuilder.AppendLine("            );");
        }
    }

    private void GenerateClassFromDynamoRecordInline(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        var constructor = model.Constructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && !c.Parameters.Any());

        if (constructor != null)
        {
            // Use parameterless constructor and set properties
            mapperBuilder.AppendLine($"            var instance = new {model.ToDisplayString()}();");
            mapperBuilder.AppendLine();

            var properties = model.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.SetMethod != null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
                .Where(p => p.Name != "EqualityContract")
                .ToList();

            foreach (var property in properties)
            {
                string conversionExpression = GetPropertySetterConversionWithExtensions(property, "pkValue", "skValue", model, modelInfo);
                if (!string.IsNullOrEmpty(conversionExpression))
                {
                    mapperBuilder.AppendLine($"            {conversionExpression}");
                }
            }

            mapperBuilder.AppendLine("            return instance;");
        }
        else
        {
            // Try constructor with parameters
            var paramConstructor = model.Constructors.FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Any());
            if (paramConstructor != null)
            {
                mapperBuilder.AppendLine($"            return new {model.ToDisplayString()}(");

                foreach (var param in paramConstructor.Parameters)
                {
                    string conversionExpression = GetFromDynamoRecordConversionWithExtensions(param, "pkValue", "skValue", model, modelInfo);

                    mapperBuilder.Append("                ");
                    mapperBuilder.Append(conversionExpression);

                    if (!SymbolEqualityComparer.Default.Equals(param, paramConstructor.Parameters.Last()))
                    {
                        mapperBuilder.AppendLine(",");
                    }
                    else
                    {
                        mapperBuilder.AppendLine();
                    }
                }

                mapperBuilder.AppendLine("            );");
            }
            else
            {
                mapperBuilder.AppendLine("            // No suitable constructor found");
                mapperBuilder.AppendLine("            return default;");
            }
        }
    }

    private void GenerateRecordWithoutConstructor(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        mapperBuilder.AppendLine($"            return new {model.ToDisplayString()}()");
        mapperBuilder.AppendLine("            {");

        var properties = model.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null)
            .Where(p => p.Name != "EqualityContract")
            .ToList();

        foreach (var property in properties)
        {
            string conversionExpression = GetFromDynamoRecordConversionWithExtensions(property.Name, property.Type, 
                IsNullableType(property.Type) || property.NullableAnnotation == NullableAnnotation.Annotated, 
                "pkValue", "skValue", model, modelInfo);
            
            mapperBuilder.AppendLine($"                {property.Name} = {conversionExpression},");
        }

        mapperBuilder.AppendLine("            };");
    }

    private void GenerateRecordWithoutConstructorInline(StringBuilder mapperBuilder, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        mapperBuilder.AppendLine($"            return new {model.ToDisplayString()}()");
        mapperBuilder.AppendLine("            {");

        var properties = model.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.SetMethod != null)
            .Where(p => p.Name != "EqualityContract")
            .ToList();

        foreach (var property in properties)
        {
            string conversionExpression = GetFromDynamoRecordConversionWithExtensions(property.Name, property.Type, 
                IsNullableType(property.Type) || property.NullableAnnotation == NullableAnnotation.Annotated, 
                "pkValue", "skValue", model, modelInfo);
            
            mapperBuilder.AppendLine($"                {property.Name} = {conversionExpression},");
        }

        mapperBuilder.AppendLine("            };");
    }

    private string GetFromDynamoRecordConversionWithExtensions(IParameterSymbol param, string pkValue, string skValue, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        var isNullable = IsNullableType(param.Type) || param.NullableAnnotation == NullableAnnotation.Annotated;
        return GetFromDynamoRecordConversionWithExtensions(param.Name, param.Type, isNullable, pkValue, skValue, model, modelInfo);
    }

    private string GetPropertySetterConversionWithExtensions(IPropertySymbol property, string pkValue, string skValue, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        var isNullable = IsNullableType(property.Type) || property.NullableAnnotation == NullableAnnotation.Annotated;
        var conversionExpression = GetFromDynamoRecordConversionWithExtensions(property.Name, property.Type, isNullable, pkValue, skValue, model, modelInfo);
        if (string.IsNullOrEmpty(conversionExpression))
            return string.Empty;

        // Handle collection types differently
        if (IsCollectionType(property.Type, out var elementType))
        {
            var rawVarName = $"{property.Name.ToLowerInvariant()}Raw";
            var baseExtraction = GetCollectionExtraction(property.Name, elementType, rawVarName);
            var targetConversion = ConvertToTargetCollectionType(property.Type, elementType, rawVarName);
            return $"if ({baseExtraction}) instance.{property.Name} = {targetConversion};";
        }
        
        // For simple types, use the direct assignment
        return $"instance.{property.Name} = {conversionExpression};";
    }

    private string GetFromDynamoRecordConversionWithExtensions(string memberName, ITypeSymbol type, bool isNullable, string pkValue, string skValue, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        // Check if this property corresponds to the PK or SK attribute to avoid variable conflicts
        // and reuse already-extracted values for efficiency
        if (memberName == modelInfo.PKName)
        {
            return isNullable 
                ? $"string.IsNullOrEmpty({pkValue}) ? null : {pkValue}"
                : $"string.IsNullOrEmpty({pkValue}) ? MissingAttributeException.Throw<string>(\"{memberName}\", {pkValue}, {skValue}) : {pkValue}";
        }
        if (memberName == modelInfo.SKName)
        {
            return isNullable 
                ? $"string.IsNullOrEmpty({skValue}) ? null : {skValue}"
                : $"string.IsNullOrEmpty({skValue}) ? MissingAttributeException.Throw<string>(\"{memberName}\", {pkValue}, {skValue}) : {skValue}";
        }
        
        if (IsCollectionType(type, out var collectionElementType))
        {
            // Skip nested collections for now (e.g., IEnumerable<IEnumerable<string>>)
            if (IsCollectionType(collectionElementType, out _))
            {
                return isNullable ? "null" : GetEmptyCollectionForType(type, collectionElementType);
            }

            var rawVarName = $"{memberName.ToLowerInvariant()}Raw";
            var baseExtraction = GetCollectionExtraction(memberName, collectionElementType, rawVarName);
            var targetConversion = ConvertToTargetCollectionType(type, collectionElementType, rawVarName);

            return $"{baseExtraction} ? {targetConversion} : {(isNullable ? "null" : GetEmptyCollectionForType(type, collectionElementType))}";
        }
        else if (IsDictionaryType(type, out var keyType, out var valueType))
        {
            // Handle dictionary types with proper extension methods
            return GetDictionaryExtraction(memberName, keyType, valueType, isNullable);
        }
        else if (IsComplexType(type))
        {
            // Handle complex types (records, classes)
            return GetComplexTypeExtraction(memberName, type, isNullable, model, modelInfo);
        }
        else
        {
            // For nullable value types, get the underlying type
            var underlyingType = type;
            if (IsNullableType(type) && type is INamedTypeSymbol nullableType)
            {
                underlyingType = nullableType.TypeArguments[0];
                isNullable = true;
            }

            return underlyingType.SpecialType switch
            {
                SpecialType.System_String when isNullable => $"record.TryGetNullableString(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_String => $"record.TryGetString(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : MissingAttributeException.Throw<string>(\"{memberName}\", {pkValue}, {skValue})",
                SpecialType.System_Byte when isNullable => $"record.TryGetNullableByte(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_Byte => $"record.TryGetByte(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(byte)",
                SpecialType.System_SByte when isNullable => $"record.TryGetNullableSByte(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_SByte => $"record.TryGetSByte(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(sbyte)",
                SpecialType.System_Char when isNullable => $"record.TryGetNullableString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && !string.IsNullOrEmpty({memberName.ToLowerInvariant()}Str) ? {memberName.ToLowerInvariant()}Str[0] : null",
                SpecialType.System_Char => $"record.TryGetString(\"{memberName}\", out var {memberName.ToLowerInvariant()}Str) && !string.IsNullOrEmpty({memberName.ToLowerInvariant()}Str) ? {memberName.ToLowerInvariant()}Str[0] : default(char)",
                SpecialType.System_Int16 when isNullable => $"record.TryGetNullableShort(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_Int16 => $"record.TryGetShort(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(short)",
                SpecialType.System_UInt16 when isNullable => $"record.TryGetNullableUShort(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_UInt16 => $"record.TryGetUShort(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(ushort)",
                SpecialType.System_Int32 when isNullable => $"record.TryGetNullableInt(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_Int32 => $"record.TryGetInt(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(int)",
                SpecialType.System_UInt32 when isNullable => $"record.TryGetNullableUInt(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_UInt32 => $"record.TryGetUInt(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(uint)",
                SpecialType.System_Int64 when isNullable => $"record.TryGetNullableLong(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_Int64 => $"record.TryGetLong(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(long)",
                SpecialType.System_UInt64 when isNullable => $"record.TryGetNullableULong(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_UInt64 => $"record.TryGetULong(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(ulong)",
                SpecialType.System_Double when isNullable => $"record.TryGetNullableDouble(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_Double => $"record.TryGetDouble(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(double)",
                SpecialType.System_Single when isNullable => $"record.TryGetNullableFloat(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_Single => $"record.TryGetFloat(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(float)",
                SpecialType.System_Decimal when isNullable => $"record.TryGetNullableDecimal(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_Decimal => $"record.TryGetDecimal(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(decimal)",
                SpecialType.System_Boolean when isNullable => $"record.TryGetNullableBool(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                SpecialType.System_Boolean => $"record.TryGetBool(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(bool)",
                _ when underlyingType.Name == nameof(DateTime) && isNullable => GetDateTimeFromDynamoRecordConversion(memberName, underlyingType, isNullable, model),
                _ when underlyingType.Name == nameof(DateTime) => GetDateTimeFromDynamoRecordConversion(memberName, underlyingType, isNullable, model),
                _ when underlyingType.Name == nameof(DateTimeOffset) && isNullable => GetDateTimeFromDynamoRecordConversion(memberName, underlyingType, isNullable, model),
                _ when underlyingType.Name == nameof(DateTimeOffset) => GetDateTimeFromDynamoRecordConversion(memberName, underlyingType, isNullable, model),
                _ when underlyingType.Name == nameof(TimeSpan) && isNullable => $"record.TryGetNullableTimeSpan(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                _ when underlyingType.Name == nameof(TimeSpan) => $"record.TryGetTimeSpan(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(TimeSpan)",
                _ when underlyingType.Name == nameof(Guid) && isNullable => $"record.TryGetNullableGuid(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                _ when underlyingType.Name == nameof(Guid) => $"record.TryGetGuid(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(Guid)",
                _ when underlyingType.TypeKind == TypeKind.Enum && isNullable => $"record.TryGetNullableEnum<{underlyingType.ToDisplayString()}>(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null",
                _ when underlyingType.TypeKind == TypeKind.Enum => $"record.TryGetEnum<{underlyingType.ToDisplayString()}>(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default({underlyingType.ToDisplayString()})",
                _ => $"default({type.ToDisplayString()})"
            };
        }
    }

    private string GetDefaultValueForType(ITypeSymbol type, bool isNullable)
    {
        if (isNullable)
        {
            return "null";
        }

        // For non-nullable reference types, provide a proper default value
        if (type.IsReferenceType)
        {
            if (type.SpecialType == SpecialType.System_String)
            {
                return "string.Empty";
            }

            // For other reference types like collections, dictionaries, etc.
            return $"default({type.ToDisplayString()})!";
        }

        // For value types, default() is fine
        return $"default({type.ToDisplayString()})";
    }

    private string GetFromDynamoRecordConversion(IParameterSymbol param)
    {
        string paramName = param.Name;

        if (param.Type is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType &&
            namedTypeSymbol.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
        {
            var elementType = namedTypeSymbol.TypeArguments[0];
            return elementType.SpecialType switch
            {
                SpecialType.System_String => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.SS : Enumerable.Empty<string>()",
                SpecialType.System_Int16 => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.NS.Select(x => short.TryParse(x, out var val) ? val : (short?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<short>()",
                SpecialType.System_Int32 => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.NS.Select(x => int.TryParse(x, out var val) ? val : (int?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<int>()",
                SpecialType.System_Int64 => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.NS.Select(x => long.TryParse(x, out var val) ? val : (long?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<long>()",
                SpecialType.System_Double => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.NS.Select(x => double.TryParse(x, out var val) ? val : (double?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<double>()",
                SpecialType.System_Decimal => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.NS.Select(x => decimal.TryParse(x, out var val) ? val : (decimal?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<decimal>()",
                SpecialType.System_Single => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.NS.Select(x => float.TryParse(x, out var val) ? val : (float?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<float>()",
                SpecialType.System_Boolean => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.SS.Select(x => bool.TryParse(x, out var val) ? val : (bool?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<bool>()",
                SpecialType.System_DateTime => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.SS.Select(x => DateTime.TryParse(x, out var val) ? val : (DateTime?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<DateTime>()",
                _ when elementType.TypeKind == TypeKind.Enum => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.SS.Select(x => Enum.TryParse<{elementType.ToDisplayString()}>(x, out var val) ? val : ({elementType.ToDisplayString()}?)null).Where(x => x.HasValue).Select(x => x.Value) : Enumerable.Empty<{elementType.ToDisplayString()}>()",
                _ => $"Enumerable.Empty<{elementType.ToDisplayString()}>()"
            };
        }
        else
        {
            return param.Type.SpecialType switch
            {
                SpecialType.System_String => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.S : null",
                SpecialType.System_Int16 => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) && short.TryParse(temp{paramName}.N, out var val{paramName}) ? val{paramName} : default(short)",
                SpecialType.System_Int32 => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) && int.TryParse(temp{paramName}.N, out var val{paramName}) ? val{paramName} : default(int)",
                SpecialType.System_Int64 => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) && long.TryParse(temp{paramName}.N, out var val{paramName}) ? val{paramName} : default(long)",
                SpecialType.System_Double => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) && double.TryParse(temp{paramName}.N, out var val{paramName}) ? val{paramName} : default(double)",
                SpecialType.System_Decimal => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) && decimal.TryParse(temp{paramName}.N, out var val{paramName}) ? val{paramName} : default(decimal)",
                SpecialType.System_Single => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) && float.TryParse(temp{paramName}.N, out var val{paramName}) ? val{paramName} : default(float)",
                SpecialType.System_Boolean => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) ? temp{paramName}.BOOL : default(bool)",
                _ when param.Type.Name == nameof(DateTime) => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) && DateTime.TryParse(temp{paramName}.S, out var val{paramName}) ? val{paramName} : default(DateTime)",
                _ when param.Type.TypeKind == TypeKind.Enum => $"record.TryGetValue(\"{paramName}\", out var temp{paramName}) && Enum.TryParse<{param.Type.ToDisplayString()}>(temp{paramName}.S, out var val{paramName}) ? val{paramName} : default({param.Type.ToDisplayString()})",
                _ => $"default({param.Type.ToDisplayString()})"
            };
        }
    }

    private string BuildKeyFactory(StringBuilder keyFactoryBuilder, string namespaceName)
    {
        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Collections.Generic;");
        sourceBuilder.AppendLine("using System.Linq;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"namespace {namespaceName};");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("public static class DynamoKeyFactory");
        sourceBuilder.AppendLine("{");
        sourceBuilder.Append(keyFactoryBuilder.ToString());
        sourceBuilder.AppendLine("}");
        return sourceBuilder.ToString();
    }

    private string BuildMapper(StringBuilder mapperBuilder, string namespaceName)
    {
        var sourceBuilder = new StringBuilder();
        sourceBuilder.AppendLine("using System;");
        sourceBuilder.AppendLine("using System.Collections.Generic;");
        sourceBuilder.AppendLine("using System.Linq;");
        sourceBuilder.AppendLine("using Goa.Clients.Dynamo;");
        sourceBuilder.AppendLine("using Goa.Clients.Dynamo.Models;");
        sourceBuilder.AppendLine("using Goa.Clients.Dynamo.Extensions;");
        sourceBuilder.AppendLine("using Goa.Clients.Dynamo.Exceptions;");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine($"namespace {namespaceName};");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("#nullable enable");
        sourceBuilder.AppendLine();
        sourceBuilder.AppendLine("public static class DynamoMapper");
        sourceBuilder.AppendLine("{");
        sourceBuilder.Append(mapperBuilder.ToString());
        sourceBuilder.AppendLine("}");
        return sourceBuilder.ToString();
    }

    private string GetDateTimeFromDynamoRecordConversion(string memberName, ITypeSymbol underlyingType, bool isNullable, INamedTypeSymbol model)
    {
        // Find the corresponding property on the model to check for UnixTimestampAttribute
        var property = GetPropertyIncludingInherited(model, memberName);
        if (property != null)
        {
            var unixTimestampAttr = property.GetAttributes().FirstOrDefault(attr => 
                attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.UnixTimestampAttribute");
            
            if (unixTimestampAttr != null)
            {
                // Get the format from the attribute, defaulting to Seconds
                var formatArg = unixTimestampAttr.NamedArguments.FirstOrDefault(arg => arg.Key == "Format");
                var isMilliseconds = formatArg.Value.Value?.ToString() == "1"; // Milliseconds = 1
                
                if (underlyingType.Name == nameof(DateTime))
                {
                    if (isNullable)
                    {
                        return isMilliseconds 
                            ? $"record.TryGetNullableUnixTimestampMilliseconds(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null"
                            : $"record.TryGetNullableUnixTimestampSeconds(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null";
                    }
                    else
                    {
                        return isMilliseconds 
                            ? $"record.TryGetUnixTimestampMilliseconds(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(DateTime)"
                            : $"record.TryGetUnixTimestampSeconds(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(DateTime)";
                    }
                }
                else if (underlyingType.Name == nameof(DateTimeOffset))
                {
                    if (isNullable)
                    {
                        return isMilliseconds 
                            ? $"record.TryGetNullableUnixTimestampMillisecondsAsOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null"
                            : $"record.TryGetNullableUnixTimestampSecondsAsOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null";
                    }
                    else
                    {
                        return isMilliseconds 
                            ? $"record.TryGetUnixTimestampMillisecondsAsOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(DateTimeOffset)"
                            : $"record.TryGetUnixTimestampSecondsAsOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(DateTimeOffset)";
                    }
                }
            }
        }
        
        // Fallback to regular DateTime/DateTimeOffset handling
        if (underlyingType.Name == nameof(DateTime))
        {
            return isNullable 
                ? $"record.TryGetNullableDateTime(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null"
                : $"record.TryGetDateTime(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(DateTime)";
        }
        else if (underlyingType.Name == nameof(DateTimeOffset))
        {
            return isNullable 
                ? $"record.TryGetNullableDateTimeOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : null"
                : $"record.TryGetDateTimeOffset(\"{memberName}\", out var {memberName.ToLowerInvariant()}) ? {memberName.ToLowerInvariant()} : default(DateTimeOffset)";
        }
        
        return $"default({underlyingType.ToDisplayString()})";
    }

    private bool HasDynamoModelAttribute(INamedTypeSymbol symbol)
    {
        var current = symbol;
        while (current != null)
        {
            if (current.GetAttributes().Any(attr => attr.AttributeClass?.ToDisplayString() == DynamoAttributeName))
                return true;

            current = current.BaseType;
        }
        return false;
    }

    private AttributeData? GetDynamoModelAttribute(INamedTypeSymbol symbol)
    {
        var current = symbol;
        while (current != null)
        {
            var attr = current.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == DynamoAttributeName);
            if (attr != null)
                return attr;

            current = current.BaseType;
        }
        return null;
    }

    private bool IsCollectionType(ITypeSymbol type, out ITypeSymbol elementType)
    {
        elementType = type;

        // Handle arrays first
        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        // Handle generic collections
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length == 1)
        {
            var originalDefinition = namedType.OriginalDefinition;

            // Check for collection interfaces and implementations
            if (originalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.ICollection<") ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.IList<") ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.List<") ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.ISet<") ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.HashSet<") ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.IReadOnlyCollection<") ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.IReadOnlyList<") ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.IReadOnlySet<") ||
                originalDefinition.ToDisplayString().StartsWith("System.Collections.ObjectModel.Collection<"))
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    private bool IsDictionaryType(ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType)
    {
        keyType = type;
        valueType = type;

        if (type is not INamedTypeSymbol namedType || !namedType.IsGenericType || namedType.TypeArguments.Length != 2)
            return false;

        var originalDefinition = namedType.OriginalDefinition;

        if (originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.IDictionary<") ||
            originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.Dictionary<") ||
            originalDefinition.ToDisplayString().StartsWith("System.Collections.Generic.IReadOnlyDictionary<"))
        {
            keyType = namedType.TypeArguments[0];
            valueType = namedType.TypeArguments[1];
            return true;
        }

        return false;
    }

    private string GetCollectionExtraction(string memberName, ITypeSymbol elementType, string variableName)
    {
        return elementType.SpecialType switch
        {
            SpecialType.System_String => $"record.TryGetStringSet(\"{memberName}\", out var {variableName})",
            SpecialType.System_Int32 => $"record.TryGetIntSet(\"{memberName}\", out var {variableName})",
            SpecialType.System_Int64 => $"record.TryGetLongSet(\"{memberName}\", out var {variableName})",
            SpecialType.System_Double => $"record.TryGetDoubleSet(\"{memberName}\", out var {variableName})",
            SpecialType.System_DateTime => $"record.TryGetDateTimeSet(\"{memberName}\", out var {variableName})",
            _ when elementType.Name == nameof(Guid) => $"record.TryGetStringSet(\"{memberName}\", out var {variableName}Strings)",
            _ when elementType.Name == nameof(TimeSpan) => $"record.TryGetStringSet(\"{memberName}\", out var {variableName}Strings)",
            _ when elementType.Name == nameof(DateTimeOffset) => $"record.TryGetStringSet(\"{memberName}\", out var {variableName}Strings)",
            _ when elementType.TypeKind == TypeKind.Enum => $"record.TryGetEnumSet<{elementType.ToDisplayString()}>(\"{memberName}\", out var {variableName})",
            _ => $"false"
        };
    }

    private string ConvertToTargetCollectionType(ITypeSymbol targetType, ITypeSymbol elementType, string variableName)
    {
        var elementTypeName = elementType.ToDisplayString();
        var sourceCollection = GetSourceCollectionExpression(elementType, variableName);

        if (targetType is IArrayTypeSymbol)
        {
            return $"({sourceCollection}?.ToArray() ?? Array.Empty<{elementTypeName}>())";
        }

        if (targetType is INamedTypeSymbol namedType)
        {
            var originalDefinition = namedType.OriginalDefinition.ToDisplayString();

            return originalDefinition switch
            {
                _ when originalDefinition.StartsWith("System.Collections.Generic.List<") =>
                    $"({sourceCollection}?.ToList() ?? new List<{elementTypeName}>())",
                _ when originalDefinition.StartsWith("System.Collections.Generic.IList<") =>
                    $"({sourceCollection}?.ToList() ?? new List<{elementTypeName}>())",
                _ when originalDefinition.StartsWith("System.Collections.Generic.ICollection<") =>
                    $"({sourceCollection}?.ToList() ?? new List<{elementTypeName}>())",
                _ when originalDefinition.StartsWith("System.Collections.ObjectModel.Collection<") =>
                    $"new System.Collections.ObjectModel.Collection<{elementTypeName}>({sourceCollection}?.ToList() ?? new List<{elementTypeName}>())",
                _ when originalDefinition.StartsWith("System.Collections.Generic.ISet<") =>
                    $"new HashSet<{elementTypeName}>({sourceCollection} ?? Enumerable.Empty<{elementTypeName}>())",
                _ when originalDefinition.StartsWith("System.Collections.Generic.HashSet<") =>
                    $"new HashSet<{elementTypeName}>({sourceCollection} ?? Enumerable.Empty<{elementTypeName}>())",
                _ when originalDefinition.StartsWith("System.Collections.Generic.IReadOnlyCollection<") =>
                    $"({sourceCollection}?.ToList() ?? new List<{elementTypeName}>())",
                _ when originalDefinition.StartsWith("System.Collections.Generic.IReadOnlyList<") =>
                    $"({sourceCollection}?.ToList() ?? new List<{elementTypeName}>())",
                _ when originalDefinition.StartsWith("System.Collections.Generic.IReadOnlySet<") =>
                    $"new HashSet<{elementTypeName}>({sourceCollection} ?? Enumerable.Empty<{elementTypeName}>())",
                _ => $"({sourceCollection} ?? Enumerable.Empty<{elementTypeName}>())" // Default: return IEnumerable<T>
            };
        }

        return $"({sourceCollection} ?? Enumerable.Empty<{elementTypeName}>())";
    }

    private string GetSourceCollectionExpression(ITypeSymbol elementType, string variableName)
    {
        return elementType.Name switch
        {
            nameof(Guid) => $"{variableName}Strings?.Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty)",
            nameof(TimeSpan) => $"{variableName}Strings?.Select(x => TimeSpan.TryParse(x, out var t) ? t : TimeSpan.Zero)",
            nameof(DateTimeOffset) => $"{variableName}Strings?.Select(x => DateTimeOffset.TryParse(x, out var d) ? d : DateTimeOffset.MinValue)",
            _ => variableName
        };
    }

    private string GetEmptyCollectionForType(ITypeSymbol targetType, ITypeSymbol elementType)
    {
        if (targetType is IArrayTypeSymbol)
        {
            return $"Array.Empty<{elementType.ToDisplayString()}>()";
        }

        if (targetType is INamedTypeSymbol namedType)
        {
            var originalDefinition = namedType.OriginalDefinition.ToDisplayString();
            var elementTypeName = elementType.ToDisplayString();

            return originalDefinition switch
            {
                _ when originalDefinition.StartsWith("System.Collections.Generic.List<") =>
                    $"new List<{elementTypeName}>()",
                _ when originalDefinition.StartsWith("System.Collections.Generic.IList<") =>
                    $"new List<{elementTypeName}>()",
                _ when originalDefinition.StartsWith("System.Collections.Generic.ICollection<") =>
                    $"new List<{elementTypeName}>()",
                _ when originalDefinition.StartsWith("System.Collections.ObjectModel.Collection<") =>
                    $"new System.Collections.ObjectModel.Collection<{elementTypeName}>()",
                _ when originalDefinition.StartsWith("System.Collections.Generic.ISet<") =>
                    $"new HashSet<{elementTypeName}>()",
                _ when originalDefinition.StartsWith("System.Collections.Generic.HashSet<") =>
                    $"new HashSet<{elementTypeName}>()",
                _ when originalDefinition.StartsWith("System.Collections.Generic.IReadOnlyCollection<") =>
                    $"new List<{elementTypeName}>()",
                _ when originalDefinition.StartsWith("System.Collections.Generic.IReadOnlyList<") =>
                    $"new List<{elementTypeName}>()",
                _ when originalDefinition.StartsWith("System.Collections.Generic.IReadOnlySet<") =>
                    $"new HashSet<{elementTypeName}>()",
                _ => $"Enumerable.Empty<{elementTypeName}>()" // Default: return empty enumerable
            };
        }

        return $"Enumerable.Empty<{elementType.ToDisplayString()}>()";
    }

    private bool IsNullableType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol namedType &&
               namedType.IsGenericType &&
               namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    private string GetDictionaryExtraction(string memberName, ITypeSymbol keyType, ITypeSymbol valueType, bool isNullable)
    {
        // Currently only support string keys and basic value types
        if (keyType.SpecialType != SpecialType.System_String)
        {
            // For unsupported key types, return appropriate default
            return isNullable ? "null" : $"new Dictionary<{keyType.ToDisplayString()}, {valueType.ToDisplayString()}>()";
        }

        var varName = $"{memberName.ToLowerInvariant()}";

        // Handle different value types
        var extractionMethod = valueType.SpecialType switch
        {
            SpecialType.System_String when isNullable => $"record.TryGetNullableStringDictionary(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_String => $"record.TryGetStringDictionary(\"{memberName}\", out var {varName}) ? {varName} : new Dictionary<string, string>()",
            SpecialType.System_Int32 when isNullable => $"record.TryGetNullableStringIntDictionary(\"{memberName}\", out var {varName}) ? {varName} : null",
            SpecialType.System_Int32 => $"record.TryGetStringIntDictionary(\"{memberName}\", out var {varName}) ? {varName} : new Dictionary<string, int>()",
            _ => isNullable ? "null" : $"new Dictionary<{keyType.ToDisplayString()}, {valueType.ToDisplayString()}>()" // Fallback for unsupported types
        };

        return extractionMethod;
    }

    private bool IsComplexType(ITypeSymbol type)
    {
        // Exclude primitive types, collections, dictionaries, and known system types
        if (type.SpecialType != SpecialType.None)
            return false;

        if (IsCollectionType(type, out _))
            return false;

        if (IsDictionaryType(type, out _, out _))
            return false;

        // Exclude nullable value types (e.g., int?, bool?)
        if (IsNullableType(type))
            return false;

        // Exclude System and Microsoft types (they're not user-defined complex types)
        var typeName = type.ToDisplayString();
        if (typeName.StartsWith("System.") || typeName.StartsWith("Microsoft."))
            return false;

        // Check if it's a class or record type that we can serialize
        return type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Struct ||
               (type is INamedTypeSymbol namedType && namedType.IsRecord);
    }

    private string GetComplexTypeExtraction(string memberName, ITypeSymbol type, bool isNullable, INamedTypeSymbol model, ModelInfo modelInfo)
    {
        var varName = $"{memberName.ToLowerInvariant()}";
        var typeName = type.ToDisplayString();

        if (isNullable)
        {
            return $"record.TryGetValue(\"{memberName}\", out var {varName}Attr) && {varName}Attr?.M != null ? DynamoMapper.{GetNormalizedTypeName(type)}.FromDynamoRecord(new DynamoRecord({varName}Attr.M), pkValue, skValue) : null";
        }
        else
        {
            return $"record.TryGetValue(\"{memberName}\", out var {varName}Attr) && {varName}Attr?.M != null ? DynamoMapper.{GetNormalizedTypeName(type)}.FromDynamoRecord(new DynamoRecord({varName}Attr.M), pkValue, skValue) : MissingAttributeException.Throw<{typeName}>(\"{memberName}\", pkValue, skValue)";
        }
    }

    private string GenerateKeyFactoryCall(INamedTypeSymbol model, string pattern, string keyType)
    {
        var properties = GetPropertiesFromPattern(pattern, model);
        
        if (properties.Any())
        {
            var paramValues = properties.Select(prop => GetRecordExtraction(prop, model)).ToList();
            var paramString = string.Join(", ", paramValues);
            return $"DynamoKeyFactory.{GetNormalizedTypeName(model)}.{keyType}({paramString})";
        }
        
        // Fallback to pattern if no properties found
        return $"\"{pattern}\"";
    }

    private string GetRecordExtraction(string propertyName, INamedTypeSymbol model)
    {
        var property = GetPropertyIncludingInherited(model, propertyName);
        if (property == null)
            return $"\"<{propertyName}>\""; // Fallback
            
        var lowerPropName = propertyName.ToLowerInvariant();
        
        return property.Type.SpecialType switch
        {
            SpecialType.System_String => $"record.TryGetString(\"{propertyName}\", out var {lowerPropName}Temp) ? {lowerPropName}Temp : \"<{propertyName}>\"",
            SpecialType.System_Int32 => $"record.TryGetInt(\"{propertyName}\", out var {lowerPropName}Temp) ? {lowerPropName}Temp.ToString() : \"<{propertyName}>\"",
            SpecialType.System_Int64 => $"record.TryGetLong(\"{propertyName}\", out var {lowerPropName}Temp) ? {lowerPropName}Temp.ToString() : \"<{propertyName}>\"",
            _ when property.Type.Name == nameof(DateTime) => $"record.TryGetDateTime(\"{propertyName}\", out var {lowerPropName}Temp) ? {lowerPropName}Temp : DateTime.MinValue",
            _ => $"\"<{propertyName}>\""
        };
    }

    private string GetNormalizedTypeName(ITypeSymbol type)
    {
        return NormalizeCSharpIdentifier(type.Name);
    }

    private void CollectReferencedComplexTypes(INamedTypeSymbol model, HashSet<INamedTypeSymbol> typesToGenerate, Compilation compilation)
    {
        // Get all properties and parameters from constructors
        var members = new List<ISymbol>();

        // Add properties
        members.AddRange(model.GetMembers().OfType<IPropertySymbol>());

        // Add constructor parameters for records
        var constructors = model.GetMembers().OfType<IMethodSymbol>().Where(m => m.MethodKind == MethodKind.Constructor);
        foreach (var constructor in constructors)
        {
            members.AddRange(constructor.Parameters);
        }

        foreach (var member in members)
        {
            var memberType = member switch
            {
                IPropertySymbol prop => prop.Type,
                IParameterSymbol param => param.Type,
                _ => null
            };

            if (memberType != null)
            {
                CollectReferencedComplexTypesRecursive(memberType, typesToGenerate, compilation, new HashSet<string>());
            }
        }
    }

    private void CollectReferencedComplexTypesRecursive(ITypeSymbol type, HashSet<INamedTypeSymbol> typesToGenerate, Compilation compilation, HashSet<string> visited)
    {
        var typeKey = type.ToDisplayString();
        if (visited.Contains(typeKey))
            return;
        visited.Add(typeKey);

        // Handle collections - get element type
        if (IsCollectionType(type, out var elementType))
        {
            CollectReferencedComplexTypesRecursive(elementType, typesToGenerate, compilation, visited);
            return;
        }

        // Handle dictionaries - get value type (key is usually string)
        if (IsDictionaryType(type, out var keyType, out var valueType))
        {
            CollectReferencedComplexTypesRecursive(keyType, typesToGenerate, compilation, visited);
            CollectReferencedComplexTypesRecursive(valueType, typesToGenerate, compilation, visited);
            return;
        }

        // Handle complex types
        if (IsComplexType(type) && type is INamedTypeSymbol namedType)
        {
            // Only add types from the same compilation (not external libraries)
            if (namedType.ContainingAssembly.Equals(compilation.Assembly, SymbolEqualityComparer.Default))
            {
                if (typesToGenerate.Add(namedType))
                {
                    // Recursively collect types referenced by this type
                    CollectReferencedComplexTypes(namedType, typesToGenerate, compilation);
                }
            }
        }
    }
}
