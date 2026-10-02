using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoBus.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CooldownWithoutTracingAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        => [DiagnosticDescriptors.CooldownWithoutTracing];

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

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
            memberAccess.Name.Identifier.ValueText != "UseConsumerFailureCooldown")
        {
            return;
        }

        var lambda = invocation.Ancestors().OfType<LambdaExpressionSyntax>().FirstOrDefault();
        if (lambda is null)
        {
            return;
        }

        var hasTracing = lambda.Body.DescendantNodesAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .Any(i =>
                i.Expression is MemberAccessExpressionSyntax access &&
                access.Name.Identifier.ValueText == "EnableDeliveryTracing");
        if (hasTracing)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.CooldownWithoutTracing,
            invocation.GetLocation()));
    }
}
