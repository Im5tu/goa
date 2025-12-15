using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Goa.Clients.Dynamo.Analyzers;

/// <summary>
/// Code fix provider that converts DynamoMapper.X.ToDynamoRecord(model) to model.ToDynamoRecord().
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseDynamoExtensionCodeFixProvider)), Shared]
public class UseDynamoExtensionCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use extension method";

    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseDynamoExtensionAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the invocation expression
        var invocation = root.FindToken(diagnosticSpan.Start).Parent?.AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault();

        if (invocation == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => ConvertToExtensionMethodAsync(context.Document, invocation, c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> ConvertToExtensionMethodAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Get the argument (the model being converted)
        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count != 1)
            return document;

        var argument = arguments[0].Expression;

        // Create the new extension method call: model.ToDynamoRecord()
        var newInvocation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                argument.WithoutTrivia(),
                SyntaxFactory.IdentifierName("ToDynamoRecord")),
            SyntaxFactory.ArgumentList())
            .WithLeadingTrivia(invocation.GetLeadingTrivia())
            .WithTrailingTrivia(invocation.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}
