using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoBus.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BroadExceptionCatchInConsumerAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.BroadExceptionCatchInConsumer];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeCatchClause, Microsoft.CodeAnalysis.CSharp.SyntaxKind.CatchClause);
    }

    private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not CatchClauseSyntax catchClause)
        {
            return;
        }

        var catchTypeSyntax = catchClause.Declaration?.Type;
        if (catchTypeSyntax is null)
        {
            return;
        }

        var catchType = context.SemanticModel.GetTypeInfo(catchTypeSyntax, context.CancellationToken).Type;
        if (catchType?.ToDisplayString() != "System.Exception")
        {
            return;
        }

        var containingTypeSyntax = catchClause.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        if (containingTypeSyntax is null)
        {
            return;
        }

        var containingType = context.SemanticModel.GetDeclaredSymbol(containingTypeSyntax, context.CancellationToken) as INamedTypeSymbol;
        if (containingType is null || !ImplementsAutoBusConsumerOrHandler(containingType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.BroadExceptionCatchInConsumer,
            catchTypeSyntax.GetLocation(),
            containingType.Name));
    }

    private static bool ImplementsAutoBusConsumerOrHandler(INamedTypeSymbol typeSymbol)
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (!iface.IsGenericType)
            {
                continue;
            }

            if (iface.Name is "IConsumer" or "IRequestHandler")
            {
                return true;
            }
        }

        return false;
    }
}
