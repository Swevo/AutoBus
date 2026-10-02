using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoBus.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MissingCancellationTokenForwardingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.MissingCancellationTokenForwarding];

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

        var method = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol as IMethodSymbol;
        if (method is null)
        {
            return;
        }

        if (method.Name is not ("PublishAsync" or "SendAsync" or "SchedulePublishAsync"))
        {
            return;
        }

        var containingType = method.ContainingType?.Name;
        if (containingType is not ("IMessageBus" or "IMessageScheduler"))
        {
            return;
        }

        var cancellationTokenParameter = method.Parameters
            .FirstOrDefault(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken");
        if (cancellationTokenParameter is null)
        {
            return;
        }

        if (InvocationProvidesCancellationToken(invocation, method, cancellationTokenParameter))
        {
            return;
        }

        var enclosingMethod = invocation.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
        if (enclosingMethod is null)
        {
            return;
        }

        var enclosingMethodSymbol = context.SemanticModel.GetDeclaredSymbol(enclosingMethod, context.CancellationToken);
        if (enclosingMethodSymbol is null)
        {
            return;
        }

        var availableToken = enclosingMethodSymbol.Parameters.Any(p => p.Type.ToDisplayString() == "System.Threading.CancellationToken");
        if (!availableToken)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.MissingCancellationTokenForwarding,
            invocation.GetLocation(),
            method.Name));
    }

    private static bool InvocationProvidesCancellationToken(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        IParameterSymbol cancellationTokenParameter)
    {
        var args = invocation.ArgumentList.Arguments;
        foreach (var argument in args)
        {
            if (argument.NameColon?.Name.Identifier.ValueText == cancellationTokenParameter.Name)
            {
                return true;
            }
        }

        if (args.Count <= cancellationTokenParameter.Ordinal)
        {
            return false;
        }

        return true;
    }
}
