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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CooldownWithoutTracingCodeFixProvider)), Shared]
public sealed class CooldownWithoutTracingCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["ABUS005"];

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
                "Enable delivery tracing",
                ct => ApplyFixAsync(context.Document, root, invocation, ct),
                equivalenceKey: "EnableDeliveryTracing"),
            diagnostic);
    }

    private static Task<Document> ApplyFixAsync(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        if (invocation.Parent is not ExpressionStatementSyntax statement)
        {
            return Task.FromResult(document);
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax access)
        {
            return Task.FromResult(document);
        }

        if (statement.Parent is not BlockSyntax block)
        {
            return Task.FromResult(document);
        }

        var receiver = access.Expression.WithoutTrivia();
        var tracingInvocation = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver,
                    SyntaxFactory.IdentifierName("EnableDeliveryTracing"))));

        var index = block.Statements.IndexOf(statement);
        if (index < 0)
        {
            return Task.FromResult(document);
        }

        var updatedBlock = block.WithStatements(block.Statements.Insert(index + 1, tracingInvocation));
        var newRoot = root.ReplaceNode(block, updatedBlock);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
