using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Goa.Functions.ApiGateway.Generator;

[Generator]
public class EntryPointGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor NoHttpActionInTopLevelStatements = new DiagnosticDescriptor(
        id: "HTTP001",
        title: "No Http Action Found in Top-Level Statements",
        messageFormat: "No usage of 'Http.UseRestApi', 'Http.UseHttpV1', or 'Http.UseHttpV2' was found in top-level statements",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor MultipleHttpActionsInTopLevelStatements = new DiagnosticDescriptor(
        id: "HTTP002",
        title: "Multiple Http Actions Found in Top-Level Statements",
        messageFormat: "Multiple usages of 'Http.UseRestApi', 'Http.UseHttpV1', or 'Http.UseHttpV2' were found in top-level statements. Only one is allowed.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor UnrecognisedHttpAction = new DiagnosticDescriptor(
        id: "HTTP003",
        title: "HttpActionNotFound",
        messageFormat: "Expected one of: 'Http.UseRestApi', 'Http.UseHttpV1', or 'Http.UseHttpV2'. Found: '{0}'.",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    private static readonly DiagnosticDescriptor LambdaRunAsyncNotFound = new DiagnosticDescriptor(
        id: "HTTP004",
        title: "No Lambda.RunAsync Invocation Found",
        messageFormat: "No valid 'Lambda.RunAsync' invocation was found with a valid 'IHttpBuilder' instance",
        category: "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var httpActions = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is GlobalStatementSyntax,
                transform: static (ctx, _) => ProcessHttpStatements(ctx, ctx.SemanticModel))
            .Where(result => result.action != null)
            .WithTrackingName("HttpActions")
            .Collect();
        context.RegisterSourceOutput(httpActions, GenerateCode);

        var lambdaActions = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is InvocationExpressionSyntax,
                transform: static (ctx, _) => ProcessLambdaStatements(ctx, ctx.SemanticModel))
            .Where(result => result.HasValue)
            .WithTrackingName("LambdaStatements")
            .Collect();
        context.RegisterSourceOutput(lambdaActions, GenerateLambdaDiagnostics);
    }

    private static (string? action, Location location) ProcessHttpStatements(GeneratorSyntaxContext context, SemanticModel semanticModel)
    {
        return FindHttpActionInVariableOrExpression(context, semanticModel);
    }

    private static bool? ProcessLambdaStatements(GeneratorSyntaxContext context, SemanticModel semanticModel)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        return IsLambdaRunWithHttpBuilder(invocation, semanticModel);
    }

    private static (string? action, Location location) FindHttpActionInVariableOrExpression(GeneratorSyntaxContext context, SemanticModel semanticModel)
    {
        // Find all variable declarations and method invocation expressions
        var allInvocations = context.Node.DescendantNodes().OfType<InvocationExpressionSyntax>();

        foreach (var invocation in allInvocations)
        {
            // Traverse the method chain for each invocation to find the Http action
            var httpAction = TraverseMethodChainForHttp(invocation, semanticModel);
            if (httpAction != null)
            {
                return (httpAction, invocation.GetLocation());
            }
        }

        return (null, Location.None);
    }

    // Traverse the method chain and return the first Http action (e.g., UseRestApi, UseHttpV1, UseHttpV2)
    private static string? TraverseMethodChainForHttp(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var currentInvocation = invocation;

        while (currentInvocation != null)
        {
            if (currentInvocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression.ToString() == "Http")
            {
                // Ensure the method is called on Http (e.g., Http.UseRestApi)
                return memberAccess.Name.Identifier.Text;
            }

            // Traverse the chain to the next method invocation
            if (currentInvocation.Expression is InvocationExpressionSyntax nextInvocation)
            {
                currentInvocation = nextInvocation;
            }
            else
            {
                break;
            }
        }

        return null;
    }

    private static bool? IsLambdaRunWithHttpBuilder(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (!IsLambdaRun(invocation))
            return null;

        // Check the first argument passed to Lambda.RunAsync
        var firstArgument = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        if (firstArgument != null)
        {
            // If the first argument is a variable or an invocation expression, analyze it
            var typeInfo = semanticModel.GetTypeInfo(firstArgument);
            if (typeInfo.Type != null && ImplementsIHttpBuilder(typeInfo.Type))
            {
                var info = typeInfo.Type ?? throw new Exception("No type found");

                return true;
            }
        }

        return false;
    }

    private static bool IsLambdaRun(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Name.Identifier.Text == "RunAsync" &&
               memberAccess.Expression.ToString() == "Lambda";
    }

    private static bool ImplementsIHttpBuilder(ITypeSymbol typeSymbol)
    {
        var expected = "Goa.Functions.ApiGateway.IHttpBuilder";
        return typeSymbol.ToDisplayString() == expected || typeSymbol.AllInterfaces.Any(i => i.ToDisplayString() == expected);
    }

    private static void GenerateCode(SourceProductionContext context, ImmutableArray<(string? action, Location location)> httpActions)
    {
        var httpActionList = httpActions.Where(x => x.action != null).ToList();

        // Ensure that the user has implemented in the way that we want them to implement
        if (httpActionList.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(NoHttpActionInTopLevelStatements, Location.None));
            return;
        }

        if (httpActionList.Count > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(MultipleHttpActionsInTopLevelStatements, Location.None));
            return;
        }

        // Use the correct request/response/serializationContext depending on the action
        var httpAction = httpActionList.First();
        var requestModel = "Goa.Functions.ApiGateway.Payloads.V2.ProxyPayloadV2Request";
        var responseModel = "Goa.Functions.ApiGateway.Payloads.V2.ProxyPayloadV2Response";
        var serializationContext = "Goa.Functions.ApiGateway.Payloads.V2.ProxyPayloadV2SerializationContext";

        switch (httpAction.action)
        {
            case "UseRestApi":
            case "UseHttpV1":
                requestModel = "Goa.Functions.ApiGateway.Payloads.V1.ProxyPayloadV1Request";
                responseModel = "Goa.Functions.ApiGateway.Payloads.V1.ProxyPayloadV1Response";
                serializationContext = "Goa.Functions.ApiGateway.Payloads.V1.ProxyPayloadV1SerializationContext";
                break;
            case "UseHttpV2":
                break; // This should be the default, so okay to no-op this
            default:
                context.ReportDiagnostic(Diagnostic.Create(UnrecognisedHttpAction, httpAction.location, httpAction.action ?? ""));
                return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine("using Goa.Functions.Core;");
        builder.AppendLine("using Goa.Functions.Core.Bootstrapping;");
        builder.AppendLine();
        builder.AppendLine($"namespace Goa.Generated;");
        builder.AppendLine();
        builder.AppendLine("// Function handler");
        builder.AppendLine($"internal sealed class Function : ILambdaFunction<{requestModel},{responseModel}>");
        builder.AppendLine("{");
        builder.AppendLine($"    private readonly List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> _middleware;");
        builder.AppendLine($"    internal Function(List<Func<InvocationContext, Func<Task>, CancellationToken, Task>> middleware)");
        builder.AppendLine("    {");
        builder.AppendLine("        _middleware = middleware;");
        builder.AppendLine("    }");
        builder.AppendLine($"    public async Task<{responseModel}> InvokeAsync({requestModel} request, CancellationToken cancellationToken)");
        builder.AppendLine("    {");
        builder.AppendLine("        await Task.Delay(1);");
        builder.AppendLine("        return default;");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        builder.AppendLine();
        builder.AppendLine("// Lambda entrypoint");
        builder.AppendLine("public static class Lambda");
        builder.AppendLine("{");
        builder.AppendLine($"    public static Task RunAsync(IHttpBuilder httpBuilder, CancellationToken cancellationToken = default) => new Goa.Functions.Core.Bootstrapping.LambdaBootstrapper<Goa.Generated.Function, {requestModel}, {responseModel}>({serializationContext}.Default, () => new Goa.Generated.Function(httpBuilder.CreatePipeline().ToList())).RunAsync(cancellationToken);");
        builder.AppendLine("}");

        context.AddSource("Function.g.cs", builder.ToString());
    }

    private static void GenerateLambdaDiagnostics(SourceProductionContext context, ImmutableArray<bool?> lambdaResults)
    {
        // Emit diagnostics if Lambda.RunAsync wasn't found with IHttpBuilder
        if (!lambdaResults.Any(r => r == true))
        {
            context.ReportDiagnostic(Diagnostic.Create(LambdaRunAsyncNotFound, Location.None));
        }
    }
}

