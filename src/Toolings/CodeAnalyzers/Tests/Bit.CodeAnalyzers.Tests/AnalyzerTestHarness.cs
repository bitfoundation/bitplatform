using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Bit.CodeAnalyzers.Tests;

internal static class AnalyzerTestHarness
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Preview, DocumentationMode.Diagnose);

    private static readonly Lazy<ImmutableArray<MetadataReference>> References = new(() => LoadReferences(includeEntityFrameworkCore: true));

    private static readonly Lazy<ImmutableArray<MetadataReference>> ReferencesWithoutEntityFrameworkCore = new(() => LoadReferences(includeEntityFrameworkCore: false));

    public static async Task<string[]> GetDiagnostics(DiagnosticAnalyzer analyzer, string source,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool referenceEntityFrameworkCore = true)
    {
        return await GetDiagnostics(analyzer, [("Test.cs", source)], outputKind, referenceEntityFrameworkCore);
    }

    public static async Task<string[]> GetDiagnostics(DiagnosticAnalyzer analyzer, (string Path, string Source)[] sources,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool referenceEntityFrameworkCore = true)
    {
        var compilation = CSharpCompilation.Create("Test",
            sources.Select(source => CSharpSyntaxTree.ParseText(source.Source, ParseOptions, source.Path)),
            referenceEntityFrameworkCore ? References.Value : ReferencesWithoutEntityFrameworkCore.Value,
            CreateCompilationOptions(outputKind));

        var diagnostics = await RunAnalyzer(compilation, analyzer);

        return [.. diagnostics.Select(Describe)];
    }

    public static void AssertDiagnostics(string[] actual, params string[] expected)
    {
        Assert.AreEqual(string.Join(Environment.NewLine, expected), string.Join(Environment.NewLine, actual));
    }

    public static async Task<string> ApplyCodeFix(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string source)
    {
        var document = CreateDocument(source);
        var diagnostics = await RunAnalyzer(document, analyzer);

        Assert.HasCount(1, diagnostics, "The source must have exactly one diagnostic to fix.");

        var action = await GetCodeAction(codeFixProvider, document, diagnostics[0]);

        return await ApplyAndVerify(document, action, analyzer);
    }

    public static async Task<string> ApplyFixAll(DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string source)
    {
        var document = CreateDocument(source);
        var diagnostics = await RunAnalyzer(document, analyzer);

        Assert.IsNotEmpty(diagnostics, "The source has nothing to fix.");

        var action = await GetCodeAction(codeFixProvider, document, diagnostics[0]);

        var fixAllContext = new FixAllContext(document, codeFixProvider, FixAllScope.Document, action.EquivalenceKey,
            codeFixProvider.FixableDiagnosticIds, new FixedDiagnosticProvider(diagnostics), CancellationToken.None);

        var fixAllAction = await codeFixProvider.GetFixAllProvider()!.GetFixAsync(fixAllContext);

        Assert.IsNotNull(fixAllAction);

        return await ApplyAndVerify(document, fixAllAction, analyzer);
    }

    private static async Task<CodeAction> GetCodeAction(CodeFixProvider codeFixProvider, Document document, Diagnostic diagnostic)
    {
        var actions = new List<CodeAction>();

        await codeFixProvider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));

        Assert.HasCount(1, actions);

        return actions[0];
    }

    private static async Task<string> ApplyAndVerify(Document document, CodeAction action, DiagnosticAnalyzer analyzer)
    {
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var fixedDocument = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution.GetDocument(document.Id)!;

        var warningsBefore = await GetCompilerWarnings(document);
        var warningsAfter = await GetCompilerWarnings(fixedDocument);
        var fixedText = (await fixedDocument.GetTextAsync()).ToString();

        Assert.AreEqual(string.Join(", ", warningsBefore), string.Join(", ", warningsAfter), $"The fix added compiler warnings:{Environment.NewLine}{fixedText}");
        Assert.IsEmpty(await RunAnalyzer(fixedDocument, analyzer), $"The fix left diagnostics behind:{Environment.NewLine}{fixedText}");

        return fixedText;
    }

    private static async Task<string[]> GetCompilerWarnings(Document document)
    {
        var compilation = await GetCompilation(document);

        return [.. compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Warning).Select(diagnostic => diagnostic.Id).Order()];
    }

    private static Document CreateDocument(string source)
    {
        var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var documentId = DocumentId.CreateNewId(projectId);

        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .WithProjectCompilationOptions(projectId, CreateCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithProjectParseOptions(projectId, ParseOptions)
            .AddMetadataReferences(projectId, References.Value)
            .AddDocument(documentId, "Test.cs", SourceText.From(source));

        return solution.GetDocument(documentId)!;
    }

    private static CSharpCompilationOptions CreateCompilationOptions(OutputKind outputKind)
    {
        return new CSharpCompilationOptions(outputKind, nullableContextOptions: NullableContextOptions.Enable)
            .WithSpecificDiagnosticOptions(new Dictionary<string, ReportDiagnostic> { ["CS1591"] = ReportDiagnostic.Suppress });
    }

    private static async Task<Compilation> GetCompilation(Document document)
    {
        return (await document.Project.GetCompilationAsync())!;
    }

    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzer(Document document, DiagnosticAnalyzer analyzer)
    {
        return await RunAnalyzer(await GetCompilation(document), analyzer);
    }

    private static async Task<ImmutableArray<Diagnostic>> RunAnalyzer(Compilation compilation, DiagnosticAnalyzer analyzer)
    {
        var errors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity is DiagnosticSeverity.Error).ToArray();

        if (errors.Length > 0)
            throw new InvalidOperationException($"The test source does not compile:{Environment.NewLine}{string.Join(Environment.NewLine, errors.Select(error => error.ToString()))}");

        var exceptions = new ConcurrentQueue<Exception>();

        var options = new CompilationWithAnalyzersOptions(new AnalyzerOptions([]), (exception, _, _) => exceptions.Enqueue(exception),
            concurrentAnalysis: true, logAnalyzerExecutionTime: false);

        var diagnostics = await compilation.WithAnalyzers([analyzer], options).GetAnalyzerDiagnosticsAsync();

        if (exceptions.IsEmpty is false)
            throw new AggregateException("The analyzer threw.", exceptions);

        return [.. diagnostics.OrderBy(diagnostic => diagnostic.Location.SourceTree?.FilePath).ThenBy(diagnostic => diagnostic.Location.SourceSpan.Start)];
    }

    private static string Describe(Diagnostic diagnostic)
    {
        return $"{diagnostic.Id}: {diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan)}";
    }

    private static ImmutableArray<MetadataReference> LoadReferences(bool includeEntityFrameworkCore)
    {
        var trustedPlatformAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;

        return [.. trustedPlatformAssemblies.Split(Path.PathSeparator)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Where(path => includeEntityFrameworkCore || Path.GetFileName(path).StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) is false)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];
    }

    private sealed class FixedDiagnosticProvider(ImmutableArray<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<Diagnostic>());
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
        }
    }
}
