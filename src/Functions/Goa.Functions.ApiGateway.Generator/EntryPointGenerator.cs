using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Goa.Functions.ApiGateway.Generator;

[Generator]
public class EntryPointGenerator : IIncrementalGenerator
{
    public static readonly DiagnosticDescriptor NoEntrypointFound = new DiagnosticDescriptor(
        id: "GOA001",
        title: "No Entrypoint Found",
        messageFormat: "You need to have a function class inheriting from FunctionBase to use this generator",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor TooManyEntrypoints = new DiagnosticDescriptor(
        id: "GOA002",
        title: "Too Many Entrypoints",
        messageFormat: "You have too classes inheriting from FunctionBase. Limit to 1 per project. Discovered: {0}.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    public static readonly DiagnosticDescriptor Message = new DiagnosticDescriptor(
        id: "GOA000",
        title: "Messsage",
        messageFormat: "{0}",
        category: "Usage",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entrypoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => ctx
            )
            .WithTrackingName("EntryPointsDebug");

        context.RegisterSourceOutput(entrypoints.Collect(), static (spc, source) => Execute(spc, source));
    }

    private static void Execute(SourceProductionContext spc, ImmutableArray<GeneratorSyntaxContext> sources)
    {
        var viableSources = new List<GeneratorSyntaxContext>();
        foreach (var source in sources)
        {
            var semanticModel = source.SemanticModel.GetDeclaredSymbol(source.Node) as INamedTypeSymbol;
            if (semanticModel is null || semanticModel.BaseType is null)
                continue;

            if (!string.Equals("Goa.Functions.Core", semanticModel.BaseType.ContainingNamespace.ToDisplayString(), StringComparison.Ordinal))
                continue;

            if (!string.Equals("FunctionBase", semanticModel.BaseType.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            viableSources.Add(source);
        }

        if (viableSources.Count == 0)
        {
            // report diagnostic that no entrypoint is found
            spc.ReportDiagnostic(Diagnostic.Create(NoEntrypointFound, Location.None));
            return;
        }

        if (viableSources.Count > 1)
        {
            spc.ReportDiagnostic(Diagnostic.Create(TooManyEntrypoints, Location.None, string.Join(",", sources)));
            return;
        }

        var src = viableSources[0]!;
        var model = (INamedTypeSymbol)src.SemanticModel.GetDeclaredSymbol(src.Node)!;
        var current = model.BaseType;
        ImmutableArray<ITypeSymbol> genericArguments = ImmutableArray<ITypeSymbol>.Empty;

        // Find the function base
        do
        {
            if (current is null)
                break;

            if (!string.Equals("Goa.Functions.Core", current.ContainingNamespace.ToDisplayString(), StringComparison.Ordinal))
            {
                current = current.BaseType;
                continue;
            }

            if (!string.Equals("FunctionBase", current.Name, StringComparison.OrdinalIgnoreCase))
            {
                current = current.BaseType;
                continue;
            }

            genericArguments = current.TypeArguments;
            break;
        } while (current is not null);

        var builder = new StringBuilder();
        var requestModel = genericArguments[0];
        var responseModel = genericArguments[1];

        builder.AppendLine("using System;");
        builder.AppendLine("using Goa.Functions.Core;");
        builder.AppendLine("using Goa.Functions.Core.Bootstrapping;");
        builder.AppendLine();
        builder.AppendLine($"namespace {model.ContainingNamespace.ToDisplayString()};");
        builder.AppendLine();
        builder.AppendLine("public class __Entrypoint");
        builder.AppendLine("{");
        builder.AppendLine("    public static async Task Main()");
        builder.AppendLine("    {");
        builder.AppendLine($"         await new LambdaBootstrapper<{model.ContainingNamespace.ToDisplayString()}.{model.Name}, {requestModel.ContainingNamespace.ToDisplayString()}.{requestModel.Name}, {responseModel.ContainingNamespace.ToDisplayString()}.{responseModel.Name}>(CustomSerializationContext.Default).RunAsync();");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        spc.AddSource($"__Entrypoint.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }
}
