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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MissingCancellationTokenForwardingCodeFixProvider)), Shared]
public sealed class MissingCancellationTokenForwardingCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ["ABUS004"];

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
                "Forward cancellation token",
                ct => ApplyFixAsync(context.Document, root, invocation, ct),
                equivalenceKey: "ForwardCancellationToken"),
            diagnostic);
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        SyntaxNode root,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return document;
        }

        var enclosingMethod = invocation.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
        if (enclosingMethod is null)
        {
            return document;
        }

        var methodSymbol = semanticModel.GetDeclaredSymbol(enclosingMethod, cancellationToken);
        if (methodSymbol is null)
        {
            return document;
        }

        var tokenParameter = methodSymbol.Parameters
            .FirstOrDefault(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken");
        if (tokenParameter is null)
        {
            return document;
        }

        var updatedInvocation = invocation.WithArgumentList(
            invocation.ArgumentList.AddArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(tokenParameter.Name))));

        var newRoot = root.ReplaceNode(invocation, updatedInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}
