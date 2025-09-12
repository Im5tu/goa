using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Collections.Immutable;
using Goa.Clients.Dynamo.Generator.Models;
using Goa.Clients.Dynamo.Generator.Attributes;
using Goa.Clients.Dynamo.Generator.TypeHandlers;
using Goa.Clients.Dynamo.Generator.CodeGeneration;

namespace Goa.Clients.Dynamo.Generator;

[Generator]
public class DynamoMapperIncrementalGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create providers for types with DynamoModel attribute
        var typeProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsCandidateType(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        
        // Combine with compilation and generate code
        var compilationAndTypes = context.CompilationProvider.Combine(typeProvider.Collect());
        
        context.RegisterSourceOutput(compilationAndTypes, static (spc, source) => Execute(source.Left, source.Right, spc));
    }
    
    private static bool IsCandidateType(SyntaxNode node)
    {
        return node is TypeDeclarationSyntax typeDecl && typeDecl.AttributeLists.Count > 0;
    }
    
    private static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        var symbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration) as INamedTypeSymbol;
        
        if (symbol == null)
            return null;
        
        // Check if it has any relevant attributes directly or through inheritance
        var hasRelevantAttribute = HasRelevantAttribute(symbol) || IsReferencedByDynamoTypes(symbol);
        
        return hasRelevantAttribute ? symbol : null;
    }
    
    private static bool HasRelevantAttribute(INamedTypeSymbol symbol)
    {
        // Check current type for attributes
        if (symbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.DynamoModelAttribute" ||
            attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute"))
        {
            return true;
        }
        
        // Check base types for DynamoModel attribute
        var baseType = symbol.BaseType;
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (baseType.GetAttributes().Any(attr =>
                attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.DynamoModelAttribute"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        
        return false;
    }
    
    private static bool IsReferencedByDynamoTypes(INamedTypeSymbol symbol)
    {
        // This would need more sophisticated logic to detect if a type is referenced
        // by DynamoDB types. For now, we'll include all types to be safe.
        return true;
    }
    
    private static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol?> types, SourceProductionContext context)
    {
        if (types.IsDefaultOrEmpty)
            return;
        
        var validTypes = types.Where(t => t is not null).Cast<INamedTypeSymbol>().ToList();
        if (!validTypes.Any())
            return;
        
        try
        {
            // Initialize registries
            var attributeRegistry = CreateAttributeRegistry();
            var typeHandlerRegistry = CreateTypeHandlerRegistry();
            
            // Analyze types
            var analyzedTypes = AnalyzeTypes(validTypes, attributeRegistry, compilation);
            if (!analyzedTypes.Any())
                return;
            
            // Validate types and report unsupported types
            ValidateTypes(analyzedTypes, typeHandlerRegistry, context.ReportDiagnostic);
            
            // Build generation context
            var generationContext = BuildGenerationContext(analyzedTypes, context.ReportDiagnostic);
            
            // Generate code
            var keyFactoryGenerator = new KeyFactoryGenerator(typeHandlerRegistry);
            var mapperGenerator = new MapperGenerator(typeHandlerRegistry);
            
            var keyFactoryCode = keyFactoryGenerator.GenerateCode(analyzedTypes, generationContext);
            var mapperCode = mapperGenerator.GenerateCode(analyzedTypes, generationContext);
            
            // Add source files
            context.AddSource("DynamoKeyFactory.g.cs", SourceText.From(keyFactoryCode, Encoding.UTF8));
            context.AddSource("DynamoMapper.g.cs", SourceText.From(mapperCode, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            // Report any generation errors
            var descriptor = new DiagnosticDescriptor(
                id: "DYNAMO_GEN_001",
                title: "Code generation error",
                messageFormat: "An error occurred during code generation: {0}",
                category: "DynamoDB.Generator",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            
            var diagnostic = Diagnostic.Create(descriptor, Location.None, ex.Message);
            context.ReportDiagnostic(diagnostic);
        }
    }
    
    private static AttributeHandlerRegistry CreateAttributeRegistry()
    {
        var registry = new AttributeHandlerRegistry();
        registry.RegisterHandler(new DynamoModelAttributeHandler());
        registry.RegisterHandler(new GSIAttributeHandler());
        registry.RegisterHandler(new UnixTimestampAttributeHandler());
        registry.RegisterHandler(new IgnoreAttributeHandler());
        registry.RegisterHandler(new SerializedNameAttributeHandler());
        return registry;
    }
    
    private static TypeHandlerRegistry CreateTypeHandlerRegistry()
    {
        var registry = new TypeHandlerRegistry();
        registry.RegisterHandler(new UnixTimestampTypeHandler()); // Highest priority
        registry.RegisterHandler(new DateTimeTypeHandler());
        registry.RegisterHandler(new DateOnlyTypeHandler());
        registry.RegisterHandler(new TimeOnlyTypeHandler());
        registry.RegisterHandler(new EnumTypeHandler());
        registry.RegisterHandler(new CollectionTypeHandler());
        registry.RegisterHandler(new PrimitiveTypeHandler());
        registry.RegisterHandler(new UnsupportedDictionaryHandler()); // Handle unsupported dictionaries
        registry.RegisterHandler(new ComplexTypeHandler()); // Lowest priority
        return registry;
    }
    
    private static List<DynamoTypeInfo> AnalyzeTypes(List<INamedTypeSymbol> types, AttributeHandlerRegistry attributeRegistry, Compilation compilation)
    {
        var result = new List<DynamoTypeInfo>();
        var processedTypes = new HashSet<string>();
        
        // First pass: collect all DynamoDB model types
        var dynamoTypes = new List<INamedTypeSymbol>();
        foreach (var type in types)
        {
            // Check for attributes on the type itself
            var dynamoAttr = type.GetAttributes().FirstOrDefault(attr => 
                attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.DynamoModelAttribute");
            var gsiAttr = type.GetAttributes().FirstOrDefault(attr => 
                attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.GlobalSecondaryIndexAttribute");
            
            // Also check if type inherits DynamoModel from base type
            var hasDynamoModelFromBase = false;
            var baseType = type.BaseType;
            while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
            {
                if (baseType.GetAttributes().Any(attr =>
                    attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.DynamoModelAttribute"))
                {
                    hasDynamoModelFromBase = true;
                    break;
                }
                baseType = baseType.BaseType;
            }
            
            // Include types that have [DynamoModel] OR [GlobalSecondaryIndex] attributes OR inherit [DynamoModel]
            if (dynamoAttr != null || gsiAttr != null || hasDynamoModelFromBase)
            {
                dynamoTypes.Add(type);
            }
        }
        
        // Second pass: collect referenced complex types and base types
        var allTypesToProcess = new HashSet<INamedTypeSymbol>(dynamoTypes, SymbolEqualityComparer.Default);
        foreach (var type in dynamoTypes)
        {
            CollectReferencedTypes(type, allTypesToProcess, compilation);
            CollectBaseTypes(type, allTypesToProcess);
        }
        
        // Third pass: analyze all types
        foreach (var type in allTypesToProcess)
        {
            if (processedTypes.Contains(type.ToDisplayString()))
                continue;
                
            var analyzedType = AnalyzeType(type, attributeRegistry);
            if (analyzedType != null)
            {
                result.Add(analyzedType);
                processedTypes.Add(type.ToDisplayString());
            }
        }
        
        return result;
    }
    
    private static void CollectReferencedTypes(INamedTypeSymbol type, HashSet<INamedTypeSymbol> allTypes, Compilation compilation)
    {
        foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
        {
            var propertyType = member.Type;
            
            // Skip system types that shouldn't be analyzed
            if (IsSystemType(propertyType))
                continue;
            
            // Handle nullable types
            if (propertyType is INamedTypeSymbol nullableType && nullableType.IsGenericType)
            {
                propertyType = nullableType.TypeArguments[0];
            }
            
            // Handle collection types
            if (IsCollectionType(propertyType, out var elementType))
            {
                propertyType = elementType;
            }
            
            // Handle complex types
            if (propertyType is INamedTypeSymbol namedType && 
                namedType.TypeKind == TypeKind.Class && 
                !IsBuiltInType(namedType.SpecialType) &&
                namedType.Name != nameof(String) &&
                !IsSystemType(namedType))
            {
                if (allTypes.Add(namedType))
                {
                    CollectReferencedTypes(namedType, allTypes, compilation);
                }
            }
        }
    }
    
    private static void CollectBaseTypes(INamedTypeSymbol type, HashSet<INamedTypeSymbol> allTypes)
    {
        var baseType = type.BaseType;
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            // Only include non-system base types that aren't already processed
            if (!IsSystemType(baseType) && !allTypes.Contains(baseType))
            {
                allTypes.Add(baseType);
                
                // Recursively collect base types of the base type
                CollectBaseTypes(baseType, allTypes);
            }
            
            baseType = baseType.BaseType;
        }
    }
    
    private static bool IsSystemType(ITypeSymbol type)
    {
        var ns = type.ContainingNamespace?.ToDisplayString() ?? "";
        return ns.StartsWith("System.Reflection") || 
               ns.StartsWith("System.Runtime") ||
               ns.StartsWith("System.IO") ||
               ns.StartsWith("System.Threading") ||
               ns.StartsWith("System.Security") ||
               ns.StartsWith("System.Diagnostics");
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
    
    private static DynamoTypeInfo? AnalyzeType(INamedTypeSymbol type, AttributeHandlerRegistry attributeRegistry)
    {
        try
        {
            var properties = new List<PropertyInfo>();
            foreach (var member in type.GetMembers().OfType<IPropertySymbol>())
            {
                var propertyInfo = AnalyzeProperty(member, attributeRegistry);
                if (propertyInfo != null)
                {
                    properties.Add(propertyInfo);
                }
            }
            
            var attributes = attributeRegistry.ProcessAttributes(type);
            
            DynamoTypeInfo? baseType = null;
            if (type.BaseType != null && type.BaseType.SpecialType != SpecialType.System_Object)
            {
                baseType = AnalyzeType(type.BaseType, attributeRegistry);
            }
            
            var dynamoTypeInfo = new DynamoTypeInfo
            {
                Symbol = type,
                Name = type.Name,
                FullName = type.ToDisplayString(),
                Namespace = type.ContainingNamespace.ToDisplayString(),
                IsAbstract = type.IsAbstract,
                IsRecord = type.IsRecord,
                Properties = properties,
                Attributes = attributes,
                BaseType = baseType
            };
            
            // Post-process GSI attributes to assign proper numbering
            AssignGSINumbering(dynamoTypeInfo);
            
            return dynamoTypeInfo;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Assigns proper GSI numbering to GSI attributes on a per-model basis.
    /// Processes inherited GSIs first, then current model GSIs.
    /// Uses the format GSI_X_PK/GSI_X_SK where X is 1-based index.
    /// </summary>
    private static void AssignGSINumbering(DynamoTypeInfo typeInfo)
    {
        // Collect all GSI attributes (inherited + current)
        var allGsiAttributes = new List<GSIAttributeInfo>();
        
        // First, collect inherited GSI attributes (if any)
        if (typeInfo.BaseType != null)
        {
            CollectInheritedGsiAttributes(typeInfo.BaseType, allGsiAttributes);
        }
        
        // Then, add current model's GSI attributes
        allGsiAttributes.AddRange(typeInfo.Attributes.OfType<GSIAttributeInfo>());
        
        // Assign sequential numbering starting from 1
        int gsiNumber = 1;
        foreach (var gsi in allGsiAttributes)
        {
            // If PKName/SKName are not explicitly set, assign sequential numbers
            if (string.IsNullOrEmpty(gsi.PKName) && string.IsNullOrEmpty(gsi.SKName))
            {
                gsi.PKName = $"GSI_{gsiNumber}_PK";
                gsi.SKName = $"GSI_{gsiNumber}_SK";
                gsiNumber++;
            }
            // If only one is set, this is an error, but we'll handle it gracefully
            else if (string.IsNullOrEmpty(gsi.PKName) || string.IsNullOrEmpty(gsi.SKName))
            {
                // Fill in the missing one with the same number
                if (string.IsNullOrEmpty(gsi.PKName))
                {
                    gsi.PKName = $"GSI_{gsiNumber}_PK";
                }
                if (string.IsNullOrEmpty(gsi.SKName))
                {
                    gsi.SKName = $"GSI_{gsiNumber}_SK";
                }
                gsiNumber++;
            }
            // If both are explicitly set, don't increment the number (custom names)
        }
    }
    
    /// <summary>
    /// Recursively collects GSI attributes from base types.
    /// </summary>
    private static void CollectInheritedGsiAttributes(DynamoTypeInfo baseType, List<GSIAttributeInfo> gsiAttributes)
    {
        // First collect from parent's parent (depth-first)
        if (baseType.BaseType != null)
        {
            CollectInheritedGsiAttributes(baseType.BaseType, gsiAttributes);
        }
        
        // Then add this level's GSI attributes
        gsiAttributes.AddRange(baseType.Attributes.OfType<GSIAttributeInfo>());
    }
    
    /// <summary>
    /// Attempts to extract GSI number from attribute names like GSI_1_PK, GSI1PK, GSI_2_SK, etc.
    /// </summary>
    private static bool TryExtractGSINumber(string attributeName, out int number)
    {
        number = 0;
        
        if (string.IsNullOrEmpty(attributeName))
            return false;
        
        // Try GSI_X_PK/GSI_X_SK format
        var match = System.Text.RegularExpressions.Regex.Match(attributeName, @"^GSI_(\d+)_(?:PK|SK)$");
        if (match.Success && int.TryParse(match.Groups[1].Value, out number))
        {
            return true;
        }
        
        // Try GSIXPK/GSIXSK format  
        match = System.Text.RegularExpressions.Regex.Match(attributeName, @"^GSI(\d+)(?:PK|SK)$");
        if (match.Success && int.TryParse(match.Groups[1].Value, out number))
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Validates that all property types in the analyzed types are supported.
    /// Reports diagnostics for unsupported types.
    /// </summary>
    private static void ValidateTypes(List<DynamoTypeInfo> types, TypeHandlerRegistry typeHandlerRegistry, Action<Diagnostic> reportDiagnostic)
    {
        foreach (var type in types)
        {
            ValidateTypeProperties(type, typeHandlerRegistry, reportDiagnostic);
        }
    }
    
    private static void ValidateTypeProperties(DynamoTypeInfo type, TypeHandlerRegistry typeHandlerRegistry, Action<Diagnostic> reportDiagnostic)
    {
        // Check all properties including inherited ones
        var current = type;
        while (current != null)
        {
            foreach (var property in current.Properties)
            {
                if (!typeHandlerRegistry.CanHandle(property))
                {
                    ReportUnsupportedTypeError(property, type, reportDiagnostic);
                }
            }
            current = current.BaseType;
        }
    }
    
    private static void ReportUnsupportedTypeError(PropertyInfo property, DynamoTypeInfo containingType, Action<Diagnostic> reportDiagnostic)
    {
        var typeName = property.Type.ToDisplayString();
        
        var descriptor = new DiagnosticDescriptor(
            id: "DYNAMO010",
            title: "Unsupported property type",
            messageFormat: "Property '{0}' in type '{1}' has unsupported type '{2}'. No type handler is registered for this type.",
            category: "DynamoDB.Generator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        
        var location = property.Symbol?.Locations.FirstOrDefault() ?? Location.None;
        var diagnostic = Diagnostic.Create(descriptor, location, property.Name, containingType.FullName, typeName);
        reportDiagnostic(diagnostic);
    }
    
    private static PropertyInfo? AnalyzeProperty(IPropertySymbol property, AttributeHandlerRegistry attributeRegistry)
    {
        try
        {
            // Skip compiler-generated properties for records
            if (property.Name == "EqualityContract")
                return null;
            
            var isNullable = property.Type.CanBeReferencedByName && property.NullableAnnotation == NullableAnnotation.Annotated;
            
            // Use the actual type for detection - nullable reference types don't change the underlying type
            var isCollection = IsCollectionType(property.Type, out var elementType);
            var isDictionary = IsDictionaryType(property.Type, out var keyType, out var valueType);
            
            var attributes = attributeRegistry.ProcessAttributes(property);
            
            return new PropertyInfo
            {
                Symbol = property,
                Name = property.Name,
                Type = property.Type,
                IsNullable = isNullable,
                IsCollection = isCollection,
                IsDictionary = isDictionary,
                Attributes = attributes,
                ElementType = elementType,
                DictionaryTypes = isDictionary ? (keyType!, valueType!) : null
            };
        }
        catch
        {
            return null;
        }
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
    
    private static GenerationContext BuildGenerationContext(List<DynamoTypeInfo> types, Action<Diagnostic> reportDiagnostic)
    {
        var availableConversions = new Dictionary<string, string>();
        var typeRegistry = new Dictionary<string, List<DynamoTypeInfo>>();
        
        // Build available conversions mapping
        foreach (var type in types)
        {
            availableConversions[type.FullName] = type.Name;
        }
        
        // Build inheritance registry
        foreach (var type in types.Where(t => !t.IsAbstract))
        {
            var current = type.BaseType;
            while (current != null)
            {
                // Include abstract base types if:
                // 1. They have [DynamoModel] attribute themselves (like BaseEntity), OR
                // 2. They are abstract and are in our types collection (meaning they're base classes of [DynamoModel] types)
                var isAbstractBase = current.IsAbstract && 
                    (current.Attributes.Any(a => a is DynamoModelAttributeInfo) || 
                     types.Any(t => t.FullName == current.FullName));
                
                if (isAbstractBase)
                {
                    if (!typeRegistry.ContainsKey(current.FullName))
                        typeRegistry[current.FullName] = new List<DynamoTypeInfo>();
                    
                    typeRegistry[current.FullName].Add(type);
                    break;
                }
                current = current.BaseType;
            }
        }
        
        return new GenerationContext
        {
            AvailableConversions = availableConversions,
            TypeRegistry = typeRegistry,
            ReportDiagnostic = reportDiagnostic
        };
    }
    
    private static bool IsBuiltInType(SpecialType specialType)
    {
        return specialType switch
        {
            SpecialType.System_Object or
            SpecialType.System_Boolean or
            SpecialType.System_Char or
            SpecialType.System_SByte or
            SpecialType.System_Byte or
            SpecialType.System_Int16 or
            SpecialType.System_UInt16 or
            SpecialType.System_Int32 or
            SpecialType.System_UInt32 or
            SpecialType.System_Int64 or
            SpecialType.System_UInt64 or
            SpecialType.System_Decimal or
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_String or
            SpecialType.System_DateTime => true,
            _ => false
        };
    }
}