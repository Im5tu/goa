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

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entrypoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => ctx
            )
            .WithTrackingName("EntryPoints");

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

            var sym = semanticModel;
            do
            {
                if (!(string.Equals("Goa.Functions.Core", sym.ContainingNamespace?.ToDisplayString(), StringComparison.Ordinal) && string.Equals("FunctionBase", sym.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    viableSources.Add(source);
                    break;
                }
                sym = sym.BaseType;
            } while (sym is not null);
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

            if (string.Equals("Goa.Functions.Core", current.ContainingNamespace.ToDisplayString(), StringComparison.Ordinal) && string.Equals("FunctionBase", current.Name, StringComparison.OrdinalIgnoreCase))
            {
                genericArguments = current.TypeArguments;
                break;
            }

            current = current.BaseType;
        } while (current is not null);

        var builder = new StringBuilder();
        var requestModel = genericArguments[0];
        var responseModel = genericArguments[1];
        var serializationType = "Goa.Functions.ApiGateway.ProxyPayloadV2SerializationContext";
        if (requestModel.Name.StartsWith("ProxyPayloadV1"))
        {
            serializationType = "Goa.Functions.ApiGateway.ProxyPayloadV1SerializationContext";
        }

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
        builder.AppendLine($"         await new LambdaBootstrapper<{model.ContainingNamespace.ToDisplayString()}.{model.Name}, {requestModel.ContainingNamespace.ToDisplayString()}.{requestModel.Name}, {responseModel.ContainingNamespace.ToDisplayString()}.{responseModel.Name}>({serializationType}.Default).RunAsync();");
        builder.AppendLine("    }");
        builder.AppendLine("}");
        builder.AppendLine();

        spc.AddSource($"__Entrypoint.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }
}
