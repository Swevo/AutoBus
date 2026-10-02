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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExcessiveRetryCountCodeFixProvider)), Shared]
public sealed class ExcessiveRetryCountCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["ABUS003"];

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
        if (node is not ArgumentSyntax argument)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Use bounded retry count (10)",
                ct => ApplyFixAsync(context.Document, root, argument, ct),
                equivalenceKey: "BoundRetryCount"),
            diagnostic);
    }

    private static Task<Document> ApplyFixAsync(
        Document document,
        SyntaxNode root,
        ArgumentSyntax argument,
        CancellationToken cancellationToken)
    {
        var literalTen = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(10))
            .WithTriviaFrom(argument.Expression);
        var newArgument = argument.WithExpression(literalTen);
        var newRoot = root.ReplaceNode(argument, newArgument);
        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
