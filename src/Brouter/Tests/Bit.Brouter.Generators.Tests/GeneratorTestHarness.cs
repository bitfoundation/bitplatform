using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Bit.Brouter.Generators.Tests;

/// <summary>
/// Drives <see cref="BrouterRoutesGenerator"/> end-to-end: feeds it .razor files as AdditionalTexts,
/// captures the generated source, compiles it in-memory, and loads the assembly so tests can invoke
/// the generated URL builders and assert on real output instead of source-text snapshots.
/// </summary>
internal static class GeneratorTestHarness
{
    public static (string GeneratedSource, Assembly Assembly) Run(params (string Path, string Content)[] razorFiles)
    {
        var (generated, assembly, _) = RunWithDiagnostics(razorFiles);
        return (generated, assembly);
    }

    public static (string GeneratedSource, Assembly Assembly, System.Collections.Immutable.ImmutableArray<Diagnostic> Diagnostics) RunWithDiagnostics(
        params (string Path, string Content)[] razorFiles)
    {
        var generator = new BrouterRoutesGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator],
            additionalTexts: razorFiles.Select(f => (AdditionalText)new TestAdditionalText(f.Path, f.Content)),
            optionsProvider: new TestOptionsProvider("TestApp"));

        var compilation = CSharpCompilation.Create(
            "TestApp",
            syntaxTrees: [],
            references: RuntimeReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var runResult = driver.GetRunResult();
        var generated = runResult.Results.Single().GeneratedSources.SingleOrDefault().SourceText?.ToString()
            ?? string.Empty;

        using var ms = new MemoryStream();
        var emit = outputCompilation.Emit(ms);
        if (emit.Success is false)
        {
            var errors = string.Join(Environment.NewLine,
                emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            throw new InvalidOperationException($"Generated code failed to compile:{Environment.NewLine}{errors}{Environment.NewLine}--- generated source ---{Environment.NewLine}{generated}");
        }

        return (generated, Assembly.Load(ms.ToArray()), runResult.Results.Single().Diagnostics);
    }

    /// <summary>Invokes a generated BrouterRoutes method; omitted optionals via Type.Missing.</summary>
    public static string Invoke(Assembly assembly, string method, params object?[] args)
    {
        var type = assembly.GetType("TestApp.BrouterRoutes")
            ?? throw new InvalidOperationException("TestApp.BrouterRoutes was not generated.");

        return (string)type.InvokeMember(
            method,
            BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static | BindingFlags.OptionalParamBinding,
            binder: null, target: null, args: args,
            culture: System.Globalization.CultureInfo.InvariantCulture)!;
    }

    /// <summary>Reads a constant off the generated Names class.</summary>
    public static string? NameConstant(Assembly assembly, string constant)
    {
        var type = assembly.GetType("TestApp.BrouterRoutes")?.GetNestedType("Names");
        return (string?)type?.GetField(constant)?.GetValue(null);
    }

    public static MethodInfo[] Methods(Assembly assembly) =>
        assembly.GetType("TestApp.BrouterRoutes")!
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

    private static IEnumerable<MetadataReference> RuntimeReferences()
    {
        var tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        return tpa.Split(Path.PathSeparator)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p));
    }

    private sealed class TestAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path => path;
        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content);
    }

    private sealed class TestOptionsProvider(string rootNamespace) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestOptions(rootNamespace);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class TestOptions(string rootNamespace) : AnalyzerConfigOptions
        {
            public override bool TryGetValue(string key, out string value)
            {
                if (key == "build_property.RootNamespace")
                {
                    value = rootNamespace;
                    return true;
                }
                value = string.Empty;
                return false;
            }
        }
    }
}
