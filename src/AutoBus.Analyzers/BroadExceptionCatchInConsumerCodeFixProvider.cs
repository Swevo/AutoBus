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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BroadExceptionCatchInConsumerCodeFixProvider)), Shared]
public sealed class BroadExceptionCatchInConsumerCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["ABUS002"];

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
        if (node is not TypeSyntax catchType)
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                "Replace with InvalidOperationException",
                ct => ApplyFixAsync(context.Document, root, catchType, ct),
                equivalenceKey: "ReplaceBroadCatch"),
            diagnostic);
    }

    private static Task<Document> ApplyFixAsync(
        Document document,
        SyntaxNode root,
        TypeSyntax catchType,
        CancellationToken cancellationToken)
    {
        var replacement = SyntaxFactory.IdentifierName(nameof(InvalidOperationException))
            .WithTriviaFrom(catchType);
        var newRoot = root.ReplaceNode(catchType, replacement);

        if (newRoot is CompilationUnitSyntax compilationUnit &&
            !compilationUnit.Usings.Any(u => u.Name?.ToString() == "System"))
        {
            newRoot = compilationUnit.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")));
        }

        return Task.FromResult(document.WithSyntaxRoot(newRoot));
    }
}
