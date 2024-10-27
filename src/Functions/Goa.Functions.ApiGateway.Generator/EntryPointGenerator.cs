using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

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
        Action<StringBuilder> responseSerializer = sb => { };

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
        builder.AppendLine("using Goa.Functions.Core;");
        builder.AppendLine("using Goa.Functions.Core.Bootstrapping;");
        builder.AppendLine("using Microsoft.Extensions.Logging;");
        builder.AppendLine("using System;");
        builder.AppendLine("using System.Linq;");
        builder.AppendLine("using System.Text.Json;");
        builder.AppendLine("using System.Text.Json.Serialization;");
        builder.AppendLine("using System.Threading.Tasks;");
        builder.AppendLine();
        builder.AppendLine($"namespace Goa.Generated;");
        builder.AppendLine();
        builder.AppendLine("// Function handler");
        builder.AppendLine($"public sealed class Function : ILambdaFunction<{requestModel},{responseModel}>");
        builder.AppendLine("{");
        builder.AppendLine("    private readonly static Goa.Functions.Core.Logging.JsonLogger _logger = new(\"Function\", Microsoft.Extensions.Logging.LogLevel.Information);");
        builder.AppendLine($"    private readonly List<Func<Goa.Functions.ApiGateway.InvocationContext, Func<Task>, CancellationToken, Task>> _middleware;");
        builder.AppendLine("    private readonly JsonSerializerContext _jsonContext;");
        builder.AppendLine();
        builder.AppendLine($"    public Function(List<Func<Goa.Functions.ApiGateway.InvocationContext, Func<Task>, CancellationToken, Task>> middleware, JsonSerializerContext jsonContext)");
        builder.AppendLine("    {");
        builder.AppendLine("        _middleware = middleware;");
        builder.AppendLine("        _jsonContext = jsonContext;");
        builder.AppendLine("    }");
        builder.AppendLine($"    public async Task<{responseModel}> InvokeAsync({requestModel} request, CancellationToken cancellationToken)");
        builder.AppendLine("    {");
        builder.AppendLine("        // Local function to execute the pipeline recursively");
        builder.AppendLine("        static async Task InvokeNextAsync(List<Func<Goa.Functions.ApiGateway.InvocationContext, Func<Task>, CancellationToken, Task>> pipeline, int index, Goa.Functions.ApiGateway.InvocationContext ctx, CancellationToken token)");
        builder.AppendLine("        {");
        builder.AppendLine("            if (index >= pipeline.Count)");
        builder.AppendLine("            {");
        builder.AppendLine("                return; // End of the pipeline, return completed task");
        builder.AppendLine("            }");
        builder.AppendLine();
        builder.AppendLine("            try");
        builder.AppendLine("            {");
        builder.AppendLine("                // Get the current pipeline step and pass in the next step as the Func<Task>");
        builder.AppendLine("                Func<Task> next = () => InvokeNextAsync(pipeline, index + 1, ctx, token);");
        builder.AppendLine("                await pipeline[index].Invoke(ctx, next, token);");
        builder.AppendLine("            }");
        builder.AppendLine("            catch (Exception ex)");
        builder.AppendLine("            {");
        builder.AppendLine("                _logger.LogFunctionError(ex);");
        builder.AppendLine("                ctx.Response.Result = HttpResult.InternalServerError();");
        builder.AppendLine("                ctx.Response.Exception = ex;");
        builder.AppendLine("                ctx.Response.ExceptionHandled = true;");
        builder.AppendLine("                return;");
        builder.AppendLine("            }");
        builder.AppendLine("        }");
        builder.AppendLine();
        builder.AppendLine("        var sw = System.Diagnostics.Stopwatch.StartNew();");
        builder.AppendLine("        try");
        builder.AppendLine("        {");
        builder.AppendLine("            var context = new Goa.Functions.ApiGateway.InvocationContext { Request = Goa.Functions.ApiGateway.Request.MapFrom(request), Response = new() };");
        builder.AppendLine("            await InvokeNextAsync(_middleware, 0, context, cancellationToken);");
        builder.AppendLine("            if (context.Response is null)");
        builder.AppendLine("            {");
        builder.AppendLine("                context.Response.Result = HttpResult.NotFound();");
        builder.AppendLine("            }");
        // TODO :: Support content negotiation
        builder.AppendLine("            context.Response.TryAddHeader(\"Content-Type\", \"application/json\");");
        builder.AppendLine($"            var response = new {responseModel}();");
        builder.AppendLine("            response.StatusCode = (int)context.Response.Result.StatusCode;");
        builder.AppendLine("            if (context.Response.Result.ResponseBody is not null)");
        builder.AppendLine("            {");
        builder.AppendLine("                var typeInfo = _jsonContext.GetTypeInfo(context.Response.Result.ResponseBody.GetType());");
        builder.AppendLine("                if (typeInfo is null)");
        builder.AppendLine("                {");
        builder.AppendLine("                    _logger.LogFunctionError($\"The response object type {context.Response.Result.ResponseBody.GetType().Name} cannot be found on the serializer context\");");
        builder.AppendLine("                    response.StatusCode = 500;");
        builder.AppendLine("                    response.Body = string.Empty;");
        builder.AppendLine("                }");
        builder.AppendLine("                else");
        builder.AppendLine("                {");
        builder.AppendLine("                    response.Body = JsonSerializer.Serialize(context.Response.Result.ResponseBody, typeInfo);");
        builder.AppendLine("                }");
        builder.AppendLine("            }");
        builder.AppendLine("            else { response.Body = string.Empty; }");
        switch (httpAction.action)
        {
            case "UseRestApi":
            case "UseHttpV1":
                builder.AppendLine("            response.MultiValueHeaders = context.Response.Headers.ToDictionary(x => x.Key, x => x.Value.ToList());");
                break;
            default:
                builder.AppendLine("            response.Headers = context.Response.Headers.ToDictionary(x => x.Key, x => string.Join(',', x.Value));");
                break;
        }
        builder.AppendLine("            return response;");
        builder.AppendLine("        }");
        builder.AppendLine("        finally");
        builder.AppendLine("        {");
        builder.AppendLine("            _logger.LogFunctionCompletion(sw.ElapsedMilliseconds);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("}");

        builder.AppendLine();
        builder.AppendLine("// Lambda entrypoint");
        builder.AppendLine("public static class Lambda");
        builder.AppendLine("{");
        builder.AppendLine($"    public static Task RunAsync(IHttpBuilder httpBuilder, JsonSerializerContext jsonContext, CancellationToken cancellationToken = default) => new Goa.Functions.Core.Bootstrapping.LambdaBootstrapper<Goa.Generated.Function, {requestModel}, {responseModel}>({serializationContext}.Default, () => new Goa.Generated.Function(httpBuilder.CreatePipeline().ToList(), jsonContext)).RunAsync(cancellationToken);");
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

