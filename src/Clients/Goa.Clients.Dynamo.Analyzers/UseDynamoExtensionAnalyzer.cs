using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Goa.Clients.Dynamo.Analyzers;

/// <summary>
/// Analyzer that suggests using the ToDynamoRecord() extension method instead of
/// DynamoMapper.X.ToDynamoRecord(model) when the extension is available.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UseDynamoExtensionAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "GOA001";
    private const string Category = "Usage";

    private static readonly LocalizableString Title = "Use extension method instead of DynamoMapper";
    private static readonly LocalizableString MessageFormat = "Use '{0}.ToDynamoRecord()' instead of 'DynamoMapper.{1}.ToDynamoRecord({0})'";
    private static readonly LocalizableString Description = "When the ToDynamoRecord() extension method is available, prefer using it over the static DynamoMapper method for cleaner code.";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            // Check if auto-generate extensions is enabled globally
            var autoGenerateExtensions = false;
            if (compilationContext.Options.AnalyzerConfigOptionsProvider.GlobalOptions
                .TryGetValue("build_property.GoaAutoGenerateExtensions", out var value))
            {
                autoGenerateExtensions = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
            }

            compilationContext.RegisterSyntaxNodeAction(
                nodeContext => AnalyzeInvocation(nodeContext, autoGenerateExtensions),
                SyntaxKind.InvocationExpression);
        });
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, bool autoGenerateExtensions)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Check if this is a call to DynamoMapper.X.ToDynamoRecord(...)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        if (memberAccess.Name.Identifier.Text != "ToDynamoRecord")
            return;

        // Check if it's DynamoMapper.X.ToDynamoRecord
        if (memberAccess.Expression is not MemberAccessExpressionSyntax dynamoMapperAccess)
            return;

        if (dynamoMapperAccess.Expression is not IdentifierNameSyntax dynamoMapperIdentifier)
            return;

        if (dynamoMapperIdentifier.Identifier.Text != "DynamoMapper")
            return;

        // Get the argument (the model being converted)
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count != 1)
            return;

        var argument = arguments[0].Expression;

        // Get the type of the argument
        var semanticModel = context.SemanticModel;
        var argumentTypeInfo = semanticModel.GetTypeInfo(argument, context.CancellationToken);
        var argumentType = argumentTypeInfo.Type;

        if (argumentType == null)
            return;

        // Check if the extension is available for this type
        if (!IsExtensionAvailable(argumentType, autoGenerateExtensions))
            return;

        // Get the argument text for the diagnostic message
        var argumentText = argument.ToString();
        var typeMapperName = dynamoMapperAccess.Name.Identifier.Text;

        // Report the diagnostic
        var diagnostic = Diagnostic.Create(
            Rule,
            invocation.GetLocation(),
            argumentText,
            typeMapperName);

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsExtensionAvailable(ITypeSymbol type, bool autoGenerateExtensions)
    {
        // Check if the type has [Extension] attribute
        var hasExtensionAttribute = type.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.ExtensionAttribute");

        if (hasExtensionAttribute)
            return true;

        // If auto-generate extensions is enabled, check if the type has [DynamoModel] attribute
        // (directly or inherited)
        if (autoGenerateExtensions)
        {
            return HasDynamoModelAttribute(type);
        }

        return false;
    }

    private static bool HasDynamoModelAttribute(ITypeSymbol type)
    {
        // Check current type
        if (type.GetAttributes()
            .Any(attr => attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.DynamoModelAttribute"))
        {
            return true;
        }

        // Check base types
        var baseType = type.BaseType;
        while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
        {
            if (baseType.GetAttributes()
                .Any(attr => attr.AttributeClass?.ToDisplayString() == "Goa.Clients.Dynamo.DynamoModelAttribute"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }
}
