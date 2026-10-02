using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AutoBus.Analyzers.Tests;

internal static class CodeFixTestHelper
{
    public static async Task<string> ApplyFirstCodeFixAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        CodeFixProvider codeFixProvider,
        string diagnosticId)
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.CurrentSolution
            .AddProject("CodeFixTests", "CodeFixTests", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithParseOptions(new CSharpParseOptions(LanguageVersion.Preview))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Task).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location))
            .AddMetadataReference(MetadataReference.CreateFromFile(typeof(System.Runtime.GCSettings).Assembly.Location));

        var document = project.AddDocument("Test.cs", SourceText.From(source));
        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        if (compilation is null)
        {
            throw new InvalidOperationException("Compilation could not be created.");
        }

        var diagnostics = await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);
        var targetDiagnostic = diagnostics.FirstOrDefault(d => d.Id == diagnosticId)
            ?? throw new InvalidOperationException($"Diagnostic '{diagnosticId}' was not produced.");

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, targetDiagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        if (actions.Count == 0)
        {
            throw new InvalidOperationException("No code fixes were registered.");
        }

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        var applyChanges = operations.OfType<ApplyChangesOperation>().FirstOrDefault()
            ?? throw new InvalidOperationException("No ApplyChangesOperation found.");
        var updatedDocument = applyChanges.ChangedSolution.GetDocument(document.Id)
            ?? throw new InvalidOperationException("Updated document missing.");
        var updatedText = await updatedDocument.GetTextAsync().ConfigureAwait(false);

        return updatedText.ToString();
    }
}
