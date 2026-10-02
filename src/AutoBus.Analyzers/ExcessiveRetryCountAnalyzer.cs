using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoBus.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExcessiveRetryCountAnalyzer : DiagnosticAnalyzer
{
    private const int MaxRecommendedRetryCount = 10;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.ExcessiveRetryCount];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol is null || symbol.Name != "UseRetry")
        {
            return;
        }

        if (symbol.Parameters.Length == 0)
        {
            return;
        }

        var containingTypeName = symbol.ContainingType?.Name;
        if (containingTypeName != "AutoBusConfigurator")
        {
            return;
        }

        var firstArgument = invocation.ArgumentList.Arguments.FirstOrDefault();
        if (firstArgument is null)
        {
            return;
        }

        var constant = context.SemanticModel.GetConstantValue(firstArgument.Expression, context.CancellationToken);
        if (!constant.HasValue || constant.Value is not int retryCount)
        {
            return;
        }

        if (retryCount > MaxRecommendedRetryCount)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ExcessiveRetryCount,
                firstArgument.GetLocation(),
                retryCount));
        }
    }
}
