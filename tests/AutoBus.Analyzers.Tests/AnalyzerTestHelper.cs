using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoBus.Analyzers.Tests;

internal static class AnalyzerTestHelper
{
    public static async Task<IReadOnlyList<Diagnostic>> GetDiagnosticsAsync(
        string source,
        DiagnosticAnalyzer analyzer)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = GetMetadataReferences();

        var compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTests",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = await compilation
            .WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);

        return diagnostics;
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var assemblies = new[]
        {
            typeof(object).Assembly,
            typeof(Task).Assembly,
            typeof(Enumerable).Assembly,
            typeof(System.Runtime.GCSettings).Assembly
        };

        return assemblies
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .DistinctBy(reference => reference.Display);
    }
}
