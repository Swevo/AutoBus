using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoBus.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingAwaitOnBusCallAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.MissingAwaitOnBusCall];

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

        if (invocation.Parent is not ExpressionStatementSyntax)
        {
            return;
        }

        var symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (symbol is null)
        {
            return;
        }

        if (!ReturnsTaskLike(symbol))
        {
            return;
        }

        var containingType = symbol.ContainingType?.Name;
        if (containingType is not ("IMessageBus" or "IMessageScheduler"))
        {
            return;
        }

        if (symbol.Name is not ("PublishAsync" or "SendAsync" or "SchedulePublishAsync"))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MissingAwaitOnBusCall,
            invocation.GetLocation(),
            symbol.Name));
    }

    private static bool ReturnsTaskLike(IMethodSymbol symbol)
    {
        var returnType = symbol.ReturnType;
        if (returnType.Name == nameof(Task))
        {
            return true;
        }

        if (returnType.Name == nameof(ValueTask))
        {
            return true;
        }

        return returnType is INamedTypeSymbol named && named.IsGenericType &&
               (named.Name == "Task" || named.Name == "ValueTask");
    }
}
