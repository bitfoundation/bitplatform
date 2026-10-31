using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Bit.Blex.Generators;
using Bit.Blex.Demo.Server.Dtos;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
/// generator's own, from the same assembly the Bit.Blex package ships as its analyzer.
/// </para>
/// </summary>
public static class BlexStoreInspector
{
    /// <summary>A store class is a few dozen lines; anything far past that was pasted by mistake.</summary>
    public const int MaxSourceLength = 50_000;

    public const int MaxStateComputedPairs = 10_000;

    /// <summary>
    /// What the generator prints per member in all - the store's name, the member's types, its label and the
    /// default values of its parameters - summed over every store in the source.
    /// </summary>
    public const int MaxTypeNameLength = 500_000;

    /// <summary>
    /// The longest one type may print. A real type is a few hundred characters; one past this was built by
    /// doubling - nested aliases, or generic inference over its own result - and printing it, in the generator or
    /// in a compiler error that names it, takes gigabytes and cannot be cancelled.
    /// </summary>
    public const int MaxTypeLength = 10_000;

    public const int MaxGeneratedLength = 1_000_000;

    /// <summary>An agent fixes the first errors first; past this many, the rest are counted instead of listed.</summary>
    public const int MaxReportedDiagnostics = 100;

    public static readonly TimeSpan InspectionTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Starts this executable as an inspection worker rather than as the web app (see <see cref="RunWorkerAsync"/>).</summary>
    public const string WorkerArgument = "--blex-inspect-worker";

    /// <summary>The GC heap a worker may use, 1 GB: an inspection needs a few hundred megabytes at most.</summary>
    private const string WorkerHeapLimit = "0x40000000";

    // Two workers at a time: the rest wait for a slot until their deadline.
    private static readonly SemaphoreSlim _gate = new(2, 2);

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

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

    public static async Task<BlexStoreInspectionDto> InspectAsync(string? source, bool includeGeneratedCode, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(InspectionTimeout);

        // Only the caller's own cancellation is an exception; running out of time is an answer, so the agent
        // reads why instead of a bare protocol error.
        try
        {
            await _gate.WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            return Refused([], $"REFUSED: the server was busy inspecting other stores for {InspectionTimeout.TotalSeconds:0} seconds. Try again shortly.");
        }

        try
        {
            return await InspectInWorkerAsync(new InspectionRequest(source ?? string.Empty, includeGeneratedCode), timeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            return Refused([], $"REFUSED: the inspection did not finish within {InspectionTimeout.TotalSeconds:0} seconds. Send one store at a time.");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>
    /// The worker's half, run by <c>Program.cs</c> when this executable is started with <see cref="WorkerArgument"/>:
    /// one request from <paramref name="input"/>, its answer to <paramref name="output"/>, and the process ends.
    /// </summary>
    public static async Task<int> RunWorkerAsync(Stream input, Stream output)
    {
        if (await JsonSerializer.DeserializeAsync<InspectionRequest>(input, _json) is not { } request) return 1;

        // The server kills the worker at the same deadline; stopping the work that can be stopped first lets it
        // answer instead.
        using var timeout = new CancellationTokenSource(InspectionTimeout);

        await JsonSerializer.SerializeAsync(output, Inspect(request.Source, request.IncludeGeneratedCode, timeout.Token), _json);
        await output.FlushAsync();

        return 0;
    }

    /// <summary>
    /// Runs one inspection in a child process of this same executable. A source can make Roslyn itself work for
    /// hours in a call that never looks at a cancellation token - inferring a type that doubles on every call, for
    /// one - and no check before compiling catches every such shape. A process can be killed where a thread
    /// cannot, and its heap can be capped where a thread's cannot, so whatever the source does, it does to the worker.
    /// </summary>
    private static async Task<BlexStoreInspectionDto> InspectInWorkerAsync(InspectionRequest request, CancellationToken cancellationToken)
    {
        using var worker = StartWorker();

        if (worker is null) return Refused([], "REFUSED: the inspection worker could not be started.");

        try
        {
            _ = worker.StandardError.BaseStream.CopyToAsync(Stream.Null, CancellationToken.None);

            await using (var input = worker.StandardInput.BaseStream)
            {
                await JsonSerializer.SerializeAsync(input, request, _json, cancellationToken);
            }

            var answer = await JsonSerializer.DeserializeAsync<BlexStoreInspectionDto>(worker.StandardOutput.BaseStream, _json, cancellationToken);

            await worker.WaitForExitAsync(cancellationToken);

            if (worker.ExitCode == 0 && answer is not null) return answer;
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            // A worker that died part way - past the heap it may use - leaves a broken pipe, and no answer or half of one.
        }
        finally
        {
            try
            {
                if (worker.HasExited is false) worker.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // It exited in between.
            }
        }

        return Refused([], "REFUSED: the inspection stopped without an answer - most likely it needed more memory than an inspection " +
                           "may use. Send one store at a time.");
    }

    private static Process? StartWorker()
    {
        // The apphost next to the server's assembly - the executable IIS runs, and the one a test run copies next to
        // its own - or else the assembly through the dotnet host.
        var assembly = typeof(BlexStoreInspector).Assembly.Location;
        var appHost = Path.ChangeExtension(assembly, OperatingSystem.IsWindows() ? ".exe" : null);

        var start = File.Exists(appHost)
            ? new ProcessStartInfo(appHost)
            : new ProcessStartInfo(Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet") { ArgumentList = { "exec", assembly } };

        start.ArgumentList.Add(WorkerArgument);
        start.RedirectStandardInput = true;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.UseShellExecute = false;
        start.CreateNoWindow = true;

        // Past this the worker's allocations fail in the worker, not in the server. A workstation GC, because the
        // server's runtimeconfig asks for a server GC, which splits the limit across a heap per core.
        start.Environment["DOTNET_GCHeapHardLimit"] = WorkerHeapLimit;
        start.Environment["DOTNET_gcServer"] = "0";

        try
        {
            return Process.Start(start);
        }
        catch (Win32Exception)
        {
            return null;
        }
    }

    /// <summary>One inspection, as the server hands it to a worker.</summary>
    private sealed record InspectionRequest(string Source, bool IncludeGeneratedCode);

    private static BlexStoreInspectionDto Inspect(string? source, bool includeGeneratedCode, CancellationToken cancellationToken)
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

        // Measured on the syntax, before the compiler folds anything: folding a constant made by doubling
        // another one materializes it, and nothing cancels that.
        if (HasOversizedConstant(userTree.GetRoot(cancellationToken), cancellationToken))
        {
            return Refused(notes, $"REFUSED: a constant in this source is longer than the {MaxSourceLength} characters the source may have, so it " +
                                  "was built by concatenating another constant with itself. Constants that long are not inspected.");
        }

        var compilation = CSharpCompilation.Create(
            "BlexStoreInspection",
            [userTree, usingsTree],
            _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var lengths = new TypeLengths();

        if (HasOversizedType(compilation, userTree, lengths, cancellationToken))
        {
            return Refused(notes, $"REFUSED: a type in this source prints longer than {MaxTypeLength} characters, so it was built by doubling " +
                                  "(nested using aliases, or generic inference over its own result). Types that long are not inspected.");
        }

        if (ExceedsGenerationBudget(compilation, userTree, lengths, cancellationToken)) return Refused(notes, TooMuchGeneratedCode);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BlexGenerator()).WithUpdatedParseOptions(_parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _, cancellationToken);

        var run = driver.GetRunResult();

        if (run.Results.SelectMany(result => result.GeneratedSources).Sum(generated => (long)generated.SourceText.Length) > MaxGeneratedLength) return Refused(notes, TooMuchGeneratedCode);

        var generatedTrees = run.GeneratedTrees.ToHashSet();

        var pragmas = userTree.GetRoot(cancellationToken).DescendantTrivia()
            .Select(trivia => trivia.GetStructure())
            .OfType<PragmaWarningDirectiveTriviaSyntax>()
            .OrderBy(pragma => pragma.SpanStart)
            .ToArray();

        // The driver hands its diagnostics back as reported; a build drops the warnings a #pragma silences
        // only when it reports them, so that filter is applied here - or a store that deliberately silences a
        // warning would be told about it all the same.
        var generatorDiagnostics = run.Results.SelectMany(result => result.Diagnostics)
            .Where(diagnostic => IsSuppressedByPragma(diagnostic, userTree, pragmas) is false)
            .ToArray();

        // Only what the compiler says is broken: a pasted fragment trips over nullable and unused-variable
        // warnings that say nothing about Blex. Generator diagnostics are kept whatever their severity.
        var compilerErrors = updated.GetDiagnostics(cancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        var diagnostics = generatorDiagnostics.Select(diagnostic => (Diagnostic: diagnostic, Source: "generator"))
            .Concat(compilerErrors.Select(diagnostic => (Diagnostic: diagnostic, Source: "compiler")))
            .Take(MaxReportedDiagnostics)
            .Select(reported => ToDto(reported.Diagnostic, reported.Source, userTree))
            .ToArray();

        if (generatorDiagnostics.Length + compilerErrors.Length > diagnostics.Length)
        {
            notes.Add($"Only the first {diagnostics.Length} of {generatorDiagnostics.Length + compilerErrors.Length} diagnostics are listed. " +
                      "Fix those and inspect the store again.");
        }

        var stores = FindStores(updated, userTree, generatedTrees, cancellationToken);

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
            IsValid = compilerErrors.Length == 0 && generatorDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error) is false,
            Stores = stores,
            Diagnostics = diagnostics,
            Notes = [.. notes],
            GeneratedCode = string.IsNullOrEmpty(generatedCode) ? null : generatedCode
        };
    }

    private static BlexInspectedStoreDto[] FindStores(Compilation compilation, SyntaxTree userTree, HashSet<SyntaxTree> generatedTrees, CancellationToken cancellationToken)
    {
        var model = compilation.GetSemanticModel(userTree);
        var stores = new List<BlexInspectedStoreDto>();

        foreach (var declaration in userTree.GetRoot(cancellationToken).DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (model.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type) continue;

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

    /// <summary>
    /// Whether a constant expression here would be longer than the whole source may be. Each expression is
    /// measured from its parts, and the name of a constant declared here stands for that constant's own
    /// measure - so <c>const string B = A + A;</c> measures twice <c>A</c>, as folding would make it, without
    /// anything being folded. Names are matched by spelling alone, which can overcount but never undercount.
    /// </summary>
    private static bool HasOversizedConstant(SyntaxNode root, CancellationToken cancellationToken)
    {
        const long over = MaxSourceLength + 1L;

        var constants = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Where(declarator => declarator.Initializer is not null && IsConstant(declarator))
            .ToLookup(declarator => declarator.Identifier.ValueText, declarator => declarator.Initializer!.Value, StringComparer.Ordinal);

        var measured = new Dictionary<ExpressionSyntax, long>();
        var named = new Dictionary<string, long>(StringComparer.Ordinal);
        var resolving = new HashSet<string>(StringComparer.Ordinal);

        // Innermost first, so an expression finds its parts already measured: a chain of thousands of `+`
        // is measured a link at a time rather than recursed through.
        foreach (var expression in root.DescendantNodes().OfType<ExpressionSyntax>().Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (Measure(expression) > MaxSourceLength) return true;
        }

        return false;

        static bool IsConstant(VariableDeclaratorSyntax declarator)
            => declarator.Parent?.Parent switch
            {
                FieldDeclarationSyntax field => field.Modifiers.Any(SyntaxKind.ConstKeyword),
                LocalDeclarationStatementSyntax local => local.IsConst,
                _ => false
            };

        long Measure(ExpressionSyntax expression)
        {
            if (measured.TryGetValue(expression, out var known)) return known;

            // Constants that name each other thousands deep: refused like a long one, not a stack overflow.
            if (RuntimeHelpers.TryEnsureSufficientExecutionStack() is false) return over;

            var length = expression switch
            {
                LiteralExpressionSyntax literal => literal.Token.ValueText.Length,
                IdentifierNameSyntax name => Named(name.Identifier.ValueText),
                MemberAccessExpressionSyntax access => Named(access.Name.Identifier.ValueText),
                BinaryExpressionSyntax binary => Measure(binary.Left) + Measure(binary.Right),
                ParenthesizedExpressionSyntax parenthesized => Measure(parenthesized.Expression),
                CastExpressionSyntax cast => Measure(cast.Expression),
                CheckedExpressionSyntax @checked => Measure(@checked.Expression),
                ConditionalExpressionSyntax conditional => Math.Max(Measure(conditional.WhenTrue), Measure(conditional.WhenFalse)),
                InterpolatedStringExpressionSyntax interpolated => interpolated.Contents.Sum(content => content switch
                {
                    InterpolatedStringTextSyntax text => text.TextToken.ValueText.Length,
                    InterpolationSyntax hole => Measure(hole.Expression),
                    _ => 0L
                }),
                _ => 0L
            };

            length = Math.Min(length, over);
            measured[expression] = length;
            return length;
        }

        long Named(string name)
        {
            if (constants.Contains(name) is false) return name.Length;

            if (named.TryGetValue(name, out var known)) return known;

            // A constant defined through itself does not compile, and nothing is folded out of it.
            if (resolving.Add(name) is false) return 0;

            var length = constants[name].Max(Measure);
            resolving.Remove(name);
            named[name] = length;
            return length;
        }
    }

    /// <summary>
    /// Whether a type written or inferred anywhere in the source prints longer than <see cref="MaxTypeLength"/>.
    /// Every expression is asked for its type - a type written out is an expression too, and so is the alias
    /// standing for one - so a type built by doubling is caught before the generator or a compiler error prints it.
    /// </summary>
    private static bool HasOversizedType(Compilation compilation, SyntaxTree userTree, TypeLengths lengths, CancellationToken cancellationToken)
    {
        var model = compilation.GetSemanticModel(userTree);

        foreach (var expression in userTree.GetRoot(cancellationToken).DescendantNodes().OfType<ExpressionSyntax>())
        {
            var info = model.GetTypeInfo(expression, cancellationToken);

            if (info.Type is { } type && lengths.Of(type) > MaxTypeLength) return true;

            if (info.ConvertedType is { } converted && lengths.Of(converted) > MaxTypeLength) return true;
        }

        return false;
    }

    private static bool ExceedsGenerationBudget(Compilation compilation, SyntaxTree userTree, TypeLengths lengths, CancellationToken cancellationToken)
    {
        var model = compilation.GetSemanticModel(userTree);
        var seen = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        long pairs = 0;
        long names = 0;

        foreach (var declaration in userTree.GetRoot(cancellationToken).DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type || seen.Add(type) is false) continue;

            var storeAttribute = type.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.ToDisplayString() == "Bit.Blex.BlexStoreAttribute");

            if (storeAttribute is null) continue;

            var storeName = lengths.Of(type);
            long states = 0;
            long computeds = 0;
            names += storeName + NameArgumentLength(storeAttribute);

            foreach (var member in type.GetMembers())
            {
                var attributes = member.GetAttributes().Select(attribute => attribute.AttributeClass?.ToDisplayString()).ToArray();

                ITypeSymbol[] printed = member switch
                {
                    IFieldSymbol field when attributes.Contains("Bit.Blex.BlexStateAttribute") => [field.Type],
                    IMethodSymbol method when attributes.Any(name => name is "Bit.Blex.BlexComputedAttribute" or "Bit.Blex.BlexActionAttribute" or "Bit.Blex.BlexEffectAttribute")
                        => [method.ReturnType, .. method.Parameters.Select(parameter => parameter.Type)],
                    _ => []
                };

                if (printed.Length == 0) continue;

                if (member is IFieldSymbol) states++;
                else if (attributes.Contains("Bit.Blex.BlexComputedAttribute")) computeds++;

                foreach (var printedType in printed)
                {
                    names += storeName + lengths.Of(printedType);

                    if (names > MaxTypeNameLength) return true;
                }

                // Printed with the member as well: an action's label, and its parameters' default values (no
                // longer than the source, HasOversizedConstant has made sure, so reading them is cheap).
                names += member.GetAttributes().Sum(NameArgumentLength);

                if (member is IMethodSymbol withParameters)
                {
                    names += withParameters.Parameters.Sum(parameter => parameter is { HasExplicitDefaultValue: true, ExplicitDefaultValue: string text } ? text.Length : 0L);
                }
            }

            pairs += states * computeds;

            if (pairs > MaxStateComputedPairs || names > MaxTypeNameLength) return true;
        }

        return false;

        static long NameArgumentLength(AttributeData attribute)
            => attribute.NamedArguments.FirstOrDefault(argument => argument.Key == "Name").Value.Value is string name ? name.Length : 0;
    }

    private const string TooMuchGeneratedCode =
        "REFUSED: the stores in this source would make the generator emit more code than this server inspects. Send one store at a time.";

    /// <summary>
    /// The answer to a source that is not inspected, with the reason as its last note: an agent reads it, where an
    /// exception would reach it as a bare "an error occurred" and an HTTP caller as a 500.
    /// </summary>
    private static BlexStoreInspectionDto Refused(List<string> notes, string reason)
        => new() { IsValid = false, Stores = [], Diagnostics = [], Notes = [.. notes, reason] };

    /// <summary>
    /// How long a type prints fully qualified, worked out without printing it. Each distinct type is measured once,
    /// so Tuple&lt;A, A&gt; costs a step more than A rather than twice A - where printing it costs twice. Lengths
    /// saturate instead of overflowing; the result is close to, not exactly, what printing gives.
    /// </summary>
    private sealed class TypeLengths
    {
        private const long Saturated = long.MaxValue / 4;

        private readonly Dictionary<ISymbol, long> _lengths = new(SymbolEqualityComparer.Default);

        public long Of(ISymbol? symbol)
        {
            if (symbol is null) return 0;

            if (_lengths.TryGetValue(symbol, out var known)) return known;

            // A type nested thousands deep: refused like a long one, not a stack overflow.
            if (RuntimeHelpers.TryEnsureSufficientExecutionStack() is false) return Saturated;

            var length = symbol switch
            {
                IArrayTypeSymbol array => Add(Of(array.ElementType), array.Rank + 1),
                IPointerTypeSymbol pointer => Add(Of(pointer.PointedAtType), 1),
                IFunctionPointerTypeSymbol pointer => pointer.Signature.Parameters
                    .Aggregate(Add(Of(pointer.Signature.ReturnType), "delegate*<>".Length), (sum, parameter) => Add(sum, Add(Of(parameter.Type), 2))),
                INamedTypeSymbol named => named.TypeArguments
                    .Aggregate(Add(Of((ISymbol?)named.ContainingType ?? named.ContainingNamespace), named.Name.Length + 3), (sum, argument) => Add(sum, Add(Of(argument), 2))),
                INamespaceSymbol { IsGlobalNamespace: true } => "global::".Length,
                INamespaceSymbol ns => Add(Of(ns.ContainingNamespace), ns.Name.Length + 1),
                _ => symbol.Name.Length
            };

            _lengths[symbol] = length;
            return length;
        }

        private static long Add(long left, long right) => Math.Min(left + right, Saturated);
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

    /// <summary>
    /// Whether a <c>#pragma warning disable</c> in the submitted source covers a warning where it is reported:
    /// the last pragma before that point naming its id (or naming none, which means every warning) decides.
    /// Errors cannot be silenced this way, in a build or here.
    /// </summary>
    private static bool IsSuppressedByPragma(Diagnostic diagnostic, SyntaxTree userTree, PragmaWarningDirectiveTriviaSyntax[] pragmas)
    {
        if (diagnostic.Severity != DiagnosticSeverity.Warning) return false;

        // The generator's locations carry a path and a span but no tree (see ToDto).
        var span = diagnostic.Location.GetLineSpan();
        if (span.IsValid is false || span.Path != userTree.FilePath) return false;

        var position = userTree.GetText().Lines.GetPosition(span.StartLinePosition);
        var suppressed = false;

        foreach (var pragma in pragmas.TakeWhile(pragma => pragma.SpanStart < position))
        {
            if (pragma.ErrorCodes.Count > 0 && pragma.ErrorCodes.Any(code => code.ToString().Trim() == diagnostic.Id) is false) continue;

            suppressed = pragma.DisableOrRestoreKeyword.IsKind(SyntaxKind.DisableKeyword);
        }

        return suppressed;
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
