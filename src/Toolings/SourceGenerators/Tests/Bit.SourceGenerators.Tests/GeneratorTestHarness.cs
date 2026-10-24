using System.Collections.Immutable;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.SourceGenerators.Tests;

internal static class GeneratorTestHarness
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);

    private static readonly Lazy<ImmutableArray<MetadataReference>> References = new(LoadReferences);

    public static GeneratorRun Run(IIncrementalGenerator generator, params string[] sources)
    {
        return Run(generator, ImmutableDictionary<string, string>.Empty, sources);
    }

    public static GeneratorRun Run(IIncrementalGenerator generator, IReadOnlyDictionary<string, string> globalOptions, params string[] sources)
    {
        return GeneratorRun.Create(CreateDriver(generator, globalOptions), CreateCompilation(sources));
    }

    public static CSharpCompilation CreateCompilation(params string[] sources)
    {
        return CSharpCompilation.Create(
            "TestApp",
            sources.Select((source, index) => ParseSource(source, $"Source{index}.cs")),
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, warningLevel: 9999));
    }

    public static SyntaxTree ParseSource(string source, string path)
    {
        return CSharpSyntaxTree.ParseText(source, ParseOptions, path: path);
    }

    private static GeneratorDriver CreateDriver(IIncrementalGenerator generator, IReadOnlyDictionary<string, string> globalOptions)
    {
        return CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: ParseOptions,
            optionsProvider: new TestOptionsProvider(globalOptions),
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
    }

    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        string[] frameworkDirectories =
        [
            Path.GetDirectoryName(typeof(object).Assembly.Location)!,
            Path.GetDirectoryName(typeof(ComponentBase).Assembly.Location)!
        ];

        return [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
                           frameworkDirectories.Contains(Path.GetDirectoryName(path), StringComparer.OrdinalIgnoreCase))
            .Select(path => MetadataReference.CreateFromFile(path))];
    }

    private sealed class TestOptionsProvider(IReadOnlyDictionary<string, string> globalOptions) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions(globalOptions);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class TestOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value)
            {
                return values.TryGetValue(key, out value!);
            }
        }
    }
}

internal sealed class GeneratorRun
{
    private GeneratorRun(GeneratorDriver driver, Compilation outputCompilation)
    {
        Driver = driver;
        OutputCompilation = outputCompilation;
        Result = driver.GetRunResult().Results.Single();
    }

    public GeneratorDriver Driver { get; }

    public Compilation OutputCompilation { get; }

    public GeneratorRunResult Result { get; }

    public ImmutableArray<Diagnostic> GeneratorDiagnostics => Result.Diagnostics;

    public string AllSources => string.Join(Environment.NewLine, Result.GeneratedSources.Select(s => $"{s.HintName}:{Environment.NewLine}{s.SourceText}"));

    public ImmutableArray<Diagnostic> CompilationDiagnostics => [.. OutputCompilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning)];

    public static GeneratorRun Create(GeneratorDriver driver, Compilation compilation)
    {
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        return new GeneratorRun(driver, outputCompilation);
    }

    public GeneratorRun RunAgain(Compilation compilation)
    {
        return Create(Driver, compilation);
    }

    public string Source(string hintNameFragment)
    {
        var matches = Result.GeneratedSources.Where(s => s.HintName.Contains(hintNameFragment, StringComparison.Ordinal)).ToArray();
        Assert.HasCount(1, matches, $"Expected one generated source named like '{hintNameFragment}'.{Environment.NewLine}{AllSources}");
        return matches[0].SourceText.ToString();
    }

    public GeneratorRun AssertSucceeded()
    {
        Assert.IsNull(Result.Exception, $"The generator threw.{Environment.NewLine}{Result.Exception}");
        Assert.IsEmpty(GeneratorDiagnostics, Describe("The generator reported diagnostics", GeneratorDiagnostics));
        Assert.IsEmpty(CompilationDiagnostics, Describe("The compilation has errors or warnings", CompilationDiagnostics));
        return this;
    }

    public string TextAt(Diagnostic diagnostic)
    {
        var path = diagnostic.Location.GetLineSpan().Path;
        var tree = OutputCompilation.SyntaxTrees.Single(t => t.FilePath == path);
        return tree.GetText().ToString(diagnostic.Location.SourceSpan);
    }

    public Assembly Load()
    {
        using var stream = new MemoryStream();
        var emitResult = OutputCompilation.Emit(stream);
        Assert.IsTrue(emitResult.Success, Describe("The compilation failed to emit", emitResult.Diagnostics));
        return Assembly.Load(stream.ToArray());
    }

    public string Describe(string title, IEnumerable<Diagnostic> diagnostics)
    {
        return $"{title}:{Environment.NewLine}{string.Join(Environment.NewLine, diagnostics)}{Environment.NewLine}{AllSources}";
    }
}
