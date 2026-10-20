using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Bit.Blex.Generators;

namespace Bit.Blex.Tests.Generators;

/// <summary>Drives <see cref="BlexGenerator"/> over an in-memory compilation.</summary>
public static class GeneratorTestHelper
{
    private static readonly ImmutableArray<MetadataReference> References = BuildReferences();

    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        var references = ImmutableArray.CreateBuilder<MetadataReference>();

        // Everything the runtime loaded for this test process (BCL, System.Text.Json, ...).
        var tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        foreach (var path in tpa.Split(Path.PathSeparator))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                references.Add(MetadataReference.CreateFromFile(path));
        }

        // The Blex runtime (attributes, BlexStoreBase, ...).
        references.Add(MetadataReference.CreateFromFile(typeof(BlexStoreAttribute).Assembly.Location));
        return references.ToImmutable();
    }

    public sealed record RunResult(
        ImmutableArray<Diagnostic> GeneratorDiagnostics,
        ImmutableArray<Diagnostic> CompilationDiagnostics,
        IReadOnlyDictionary<string, string> GeneratedSources)
    {
        public bool HasGeneratorError => GeneratorDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

        public string SingleGenerated => Assert1(GeneratedSources);

        private static string Assert1(IReadOnlyDictionary<string, string> sources)
        {
            if (sources.Count != 1)
                throw new InvalidOperationException($"Expected exactly one generated source, got {sources.Count}: {string.Join(", ", sources.Keys)}");
            return sources.Values.First();
        }
    }

    /// <summary>Runs the generator over <paramref name="source"/> and compiles the result.</summary>
    public static RunResult Run(string source)
    {
        var compilation = CSharpCompilation.Create(
            "BlexGeneratorTests",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest))],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BlexGenerator());
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);

        var runResult = driver.GetRunResult();
        var generated = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .ToDictionary(s => s.HintName, s => s.SourceText.ToString());

        return new RunResult(
            runResult.Results.SelectMany(r => r.Diagnostics).ToImmutableArray(),
            updated.GetDiagnostics(),
            generated);
    }

    /// <summary>Asserts the run produced a specific diagnostic id.</summary>
    public static bool HasDiagnostic(this RunResult result, string id)
        => result.GeneratorDiagnostics.Any(d => d.Id == id);

    /// <summary>Asserts the generated code compiles without errors.</summary>
    public static ImmutableArray<Diagnostic> CompileErrors(this RunResult result)
        => [.. result.CompilationDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)];
}
