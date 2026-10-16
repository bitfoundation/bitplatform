using System.Collections.Immutable;
using Bit.Blex.Generators;
using Bit.Blex.Demo.Server.Dtos;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// Runs the real Blex source generator over a piece of store code and reports what it made of it -
/// the diagnostics a build would report, and the public members the generated partial adds.
/// <para>
/// This is the one question about Blex that documentation cannot answer: "what does the generator do
/// with THIS class?". A store is plain C# with attributes on it, and the rules that decide whether it
/// generates - partial, naming prefixes, return types, member shapes - are easy to get slightly wrong
/// in ways that only show up as a build error, or worse, as a generated member under a name nobody
/// expected. So instead of describing the rules, the server applies them: the verdict is the
/// generator's own, from the same assembly the Bit.Blex.Generators package ships.
/// </para>
/// </summary>
public static class BlexStoreInspector
{
    /// <summary>A store class is a few dozen lines; anything far past that was pasted by mistake.</summary>
    public const int MaxSourceLength = 50_000;

    // The usings a store file starts with in practice, so a pasted class without its header still
    // compiles - every sample on the site and in the README is written without them.
    private const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using Bit.Blex;
        """;

    private static readonly Lazy<ImmutableArray<MetadataReference>> _references = new(BuildReferences);

    private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Latest);

    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        var references = ImmutableArray.CreateBuilder<MetadataReference>();

        // The framework this server runs on - BCL, System.Text.Json and the rest a store can use.
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

        foreach (var path in tpa.Split(Path.PathSeparator))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) references.Add(MetadataReference.CreateFromFile(path));
        }

        // The Blex runtime: the attributes the generator reads and the BlexStoreBase it derives from.
        references.Add(MetadataReference.CreateFromFile(typeof(BlexStoreBase).Assembly.Location));

        return references.ToImmutable();
    }

    public static BlexStoreInspectionDto Inspect(string? source, bool includeGeneratedCode)
    {
        source ??= string.Empty;

        var notes = new List<string>();

        if (source.Length > MaxSourceLength)
        {
            notes.Add($"INCOMPLETE ANALYSIS: the source is {source.Length} characters and only the first {MaxSourceLength} were inspected. " +
                      "Send one store at a time.");
            source = source[..MaxSourceLength];
        }

        var userTree = CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Store.cs");
        var usingsTree = CSharpSyntaxTree.ParseText(ImplicitUsings, _parseOptions, path: "GlobalUsings.cs");

        var compilation = CSharpCompilation.Create(
            "BlexStoreInspection",
            [userTree, usingsTree],
            _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BlexGenerator()).WithUpdatedParseOptions(_parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);

        var run = driver.GetRunResult();
        var generatedTrees = run.GeneratedTrees.ToHashSet();

        var generatorDiagnostics = run.Results.SelectMany(result => result.Diagnostics).ToArray();

        // Only what the compiler says is broken: a pasted fragment trips over nullable and unused-variable
        // warnings that say nothing about Blex. Generator diagnostics are kept whatever their severity.
        var compilerErrors = updated.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        var diagnostics = generatorDiagnostics.Select(diagnostic => ToDto(diagnostic, "generator", userTree))
            .Concat(compilerErrors.Select(diagnostic => ToDto(diagnostic, "compiler", userTree)))
            .ToArray();

        var stores = FindStores(updated, userTree, generatedTrees);

        if (stores.Length == 0)
        {
            notes.Add("No class marked [BlexStore] (or [BlexStoreAttribute]) was found, so the generator had nothing to do. " +
                      "A store is a `partial class` marked [BlexStore] whose state lives in [BlexState] fields.");
        }

        foreach (var store in stores.Where(store => store.Generated is false))
        {
            notes.Add($"'{store.Name}' was rejected: the generator emits nothing for a store with a BLEX error, so every " +
                      "generated member it would have had is missing too. Fix the BLEX diagnostic first; the compiler errors " +
                      "that mention its members go away with it. GetBlexDiagnostics(code) has the cause and the fix for each id.");
        }

        if (generatorDiagnostics.Length == 0 && compilerErrors.Length > 0)
        {
            notes.Add("The generator accepted the store, but the C# around it does not compile - see the compiler diagnostics.");
        }

        string? generatedCode = null;

        if (includeGeneratedCode)
        {
            generatedCode = string.Join("\n\n", run.Results.SelectMany(result => result.GeneratedSources)
                .Select(generated => $"// {generated.HintName}\n{generated.SourceText}"));
        }

        return new BlexStoreInspectionDto
        {
            IsValid = diagnostics.Any(diagnostic => diagnostic.Severity == nameof(DiagnosticSeverity.Error)) is false,
            Stores = stores,
            Diagnostics = diagnostics,
            Notes = [.. notes],
            GeneratedCode = string.IsNullOrEmpty(generatedCode) ? null : generatedCode
        };
    }

    private static BlexInspectedStoreDto[] FindStores(Compilation compilation, SyntaxTree userTree, HashSet<SyntaxTree> generatedTrees)
    {
        var model = compilation.GetSemanticModel(userTree);
        var stores = new List<BlexInspectedStoreDto>();

        foreach (var declaration in userTree.GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type) continue;

            var isStore = type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == "Bit.Blex.BlexStoreAttribute");
            if (isStore is false || stores.Any(store => store.Name == type.Name)) continue;

            var members = type.GetMembers()
                .Where(member => member.DeclaredAccessibility == Accessibility.Public
                                 && member.IsImplicitlyDeclared is false
                                 && member.IsOverride is false
                                 && member.Locations.Any(location => location.SourceTree is not null && generatedTrees.Contains(location.SourceTree)))
                .Select(Describe)
                .OfType<BlexGeneratedMemberDto>()
                .ToArray();

            var generated = type.Locations.Any(location => location.SourceTree is not null && generatedTrees.Contains(location.SourceTree));

            stores.Add(new BlexInspectedStoreDto { Name = type.Name, Generated = generated, Members = members });
        }

        return [.. stores];
    }

    private static BlexGeneratedMemberDto? Describe(ISymbol member)
    {
        var format = SymbolDisplayFormat.MinimallyQualifiedFormat;

        return member switch
        {
            IPropertySymbol property => new BlexGeneratedMemberDto
            {
                Kind = "Property",
                Name = property.Name,
                Signature = $"{property.Type.ToDisplayString(format)} {property.Name} {{ get;{(property.SetMethod?.DeclaredAccessibility == Accessibility.Public ? " set;" : string.Empty)} }}"
            },
            IMethodSymbol { MethodKind: MethodKind.Ordinary } method => new BlexGeneratedMemberDto
            {
                Kind = "Method",
                Name = method.Name,
                Signature = $"{method.ReturnType.ToDisplayString(format)} {method.Name}({string.Join(", ", method.Parameters.Select(p => p.ToDisplayString(format)))})"
            },
            _ => null
        };
    }

    private static BlexInspectionDiagnosticDto ToDto(Diagnostic diagnostic, string source, SyntaxTree userTree)
    {
        // The generator reports through locations it rebuilt from a file path and a span (an incremental
        // generator may not hold on to syntax trees), so they carry no SourceTree - the path is what
        // says whether a diagnostic points into the submitted source.
        var span = diagnostic.Location.GetLineSpan();

        return new BlexInspectionDiagnosticDto
        {
            Id = diagnostic.Id,
            Severity = diagnostic.Severity.ToString(),
            Message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture),
            Source = source,
            Line = span.IsValid && span.Path == userTree.FilePath ? span.StartLinePosition.Line + 1 : null
        };
    }
}
