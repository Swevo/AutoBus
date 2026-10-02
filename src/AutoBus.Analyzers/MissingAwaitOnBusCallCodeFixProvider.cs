using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoBus.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingAwaitOnBusCallCodeFixProvider)), Shared]
public sealed class MissingAwaitOnBusCallCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["ABUS001"];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics.First();
        var node = root.FindNode(diagnostic.Location.SourceSpan);
        if (node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Await bus call",
                ct => ApplyFixAsync(context.Document, root, invocation, ct),
                equivalenceKey: "AwaitBusCall"),
            diagnostic);
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        if (invocation.Parent is not ExpressionStatementSyntax statement)
        {
            return document;
        }

        var awaitedExpression = SyntaxFactory.AwaitExpression(invocation.WithoutTrivia())
            .WithTriviaFrom(invocation);
        var awaitedStatement = statement.WithExpression(awaitedExpression);

        var newRoot = root.ReplaceNode(statement, awaitedStatement);

        if (statement.FirstAncestorOrSelf<MethodDeclarationSyntax>() is { } method && !method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            var returnTypeText = method.ReturnType.ToString();
            if (returnTypeText.StartsWith("Task", StringComparison.Ordinal) ||
                returnTypeText.StartsWith("ValueTask", StringComparison.Ordinal))
            {
                var updatedMethod = method.WithModifiers(method.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword)));
                newRoot = newRoot.ReplaceNode(method, updatedMethod);
            }
        }

        return document.WithSyntaxRoot(newRoot);
    }
}
