using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
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
    /// doubling - nested aliases, nested anonymous types, or generic inference over its own result - and printing
    /// it, in the generator or in a compiler error that names it, takes gigabytes and cannot be cancelled.
    /// </summary>
    public const int MaxTypeLength = 10_000;

    public const int MaxGeneratedLength = 1_000_000;

    /// <summary>An agent fixes the first errors first; past this many, the rest are counted instead of listed.</summary>
    public const int MaxReportedDiagnostics = 100;

    /// <summary>
    /// The longest message a diagnostic is reported with. A real one is a line or two; past this it names a type or a
    /// member that runs on for pages, and its start already says what is wrong.
    /// </summary>
    public const int MaxMessageLength = 2_000;

    /// <summary>How long an inspection may run once a worker is free for it - and how long it may wait for one.</summary>
    public static readonly TimeSpan InspectionTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Starts this executable as an inspection worker rather than as the web app (see <see cref="RunWorkerAsync"/>).</summary>
    public const string WorkerArgument = "--blex-inspect-worker";

    /// <summary>The GC heap a worker may use, 1 GB: an inspection needs a few hundred megabytes at most.</summary>
    private const string WorkerHeapLimit = "0x40000000";

    /// <summary>
    /// The most of a worker's answer the server reads. The largest real one - the generated code at its cap, escaped
    /// as JSON, and a hundred diagnostics - is a few megabytes; past this the server stops reading rather than
    /// buffering whatever the worker sends.
    /// </summary>
    private const int MaxAnswerBytes = 16 * 1024 * 1024;

    /// <summary>How much of a worker's error output the log keeps: an exception, or the runtime's reason it did not start.</summary>
    private const int MaxLoggedErrorLength = 16_000;

    /// <summary>
    /// A worker stops what can be stopped this long before the server's deadline, so it answers in time, and ends
    /// itself this long after it, should the server no longer be there to kill it.
    /// </summary>
    private static readonly TimeSpan WorkerMargin = TimeSpan.FromSeconds(1);

    // Two workers at a time: the rest wait for a slot until their deadline.
    private static readonly SemaphoreSlim _gate = new(2, 2);

    // A worker started before the inspection that takes it (see TakeWorker).
    private static Process? _spare;

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

    // What a worker inspects while it waits for its request, so the runtime has compiled the compiler by the time
    // the request needs it.
    private const string WarmUpSource = """
        [BlexStore]
        public partial class WarmUpStore
        {
            [BlexState] private int _count;
            [BlexComputed] private int ComputeDouble() => Count * 2;
            [BlexAction] private void OnIncrement() => Count += Missing;
            [BlexEffect] private async Task OnLoad(int id, CancellationToken ct) => await Task.Delay(id, ct);
        }
        """;

    private static readonly Lazy<ImmutableArray<MetadataReference>> _references = new(BuildReferences);

    private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Latest);

    /// <summary>
    /// How this executable is started again as a worker. A framework-dependent app is run through the dotnet host of
    /// the installation it runs on now - the host IIS starts it with, or the one its runtime was loaded from - rather
    /// than through an apphost: a publish on Linux leaves no <c>.exe</c> for a Windows server to run, and a
    /// <c>dotnet</c> found on PATH may be another installation, or none. Only an app that brings its own runtime is
    /// started through its apphost.
    /// </summary>
    private static readonly Lazy<string[]> _workerCommand = new(() =>
    {
        var assembly = typeof(BlexStoreInspector).Assembly.Location;
        var dotnet = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";

        if (string.Equals(Path.GetFileName(Environment.ProcessPath), dotnet, StringComparison.OrdinalIgnoreCase)) return [Environment.ProcessPath!, "exec", assembly];

        // <root>/shared/Microsoft.NETCore.App/<version>/ - under IIS in-process hosting, or a test runner's own executable.
        var runtime = new DirectoryInfo(Path.GetDirectoryName(typeof(object).Assembly.Location)!);

        if (runtime.Parent?.Parent is { Name: "shared", Parent: { } root } && File.Exists(Path.Combine(root.FullName, dotnet)))
        {
            return [Path.Combine(root.FullName, dotnet), "exec", assembly];
        }

        var appHost = Path.ChangeExtension(assembly, OperatingSystem.IsWindows() ? ".exe" : null);

        return File.Exists(appHost) ? [appHost] : [Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet", "exec", assembly];
    });

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

    public static async Task<BlexStoreInspectionDto> InspectAsync(string? source, bool includeGeneratedCode, ILogger logger, CancellationToken cancellationToken)
    {
        source ??= string.Empty;

        // Only the caller's own cancellation is an exception; running out of time is an answer, so the agent
        // reads why instead of a bare protocol error.
        using (var waiting = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            waiting.CancelAfter(InspectionTimeout);

            try
            {
                await _gate.WaitAsync(waiting.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
            {
                return Refused([], $"REFUSED: the server was busy inspecting other stores for {InspectionTimeout.TotalSeconds:0} seconds. Try again shortly.");
            }
        }

        try
        {
            // The deadline starts once a slot is free, so time spent queued is not taken from the inspection.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(InspectionTimeout);

            var request = ToRequest(source, includeGeneratedCode, Environment.TickCount64 + (long)InspectionTimeout.TotalMilliseconds);

            return await InspectInWorkerAsync(request, logger, timeout.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            return Refused([], TimedOut);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static string TimedOut => $"REFUSED: the inspection did not finish within {InspectionTimeout.TotalSeconds:0} seconds. Send one store at a time.";

    /// <summary>
    /// The source is cut to <see cref="MaxSourceLength"/> here rather than in the worker, so an oversized paste is not
    /// copied into it only to be dropped there; the length it had goes along for the worker's note.
    /// </summary>
    private static InspectionRequest ToRequest(string source, bool includeGeneratedCode, long deadline)
    {
        var length = Math.Min(source.Length, MaxSourceLength);

        // Never between the halves of a surrogate pair, which JSON cannot carry.
        if (length < source.Length && char.IsHighSurrogate(source[length - 1])) length--;

        return new InspectionRequest(source[..length], source.Length, includeGeneratedCode, deadline);
    }

    /// <summary>
    /// The worker's half, run by <c>Program.cs</c> when this executable is started with <see cref="WorkerArgument"/>:
    /// one request from <paramref name="input"/>, its answer to <paramref name="output"/>, and the process ends. What
    /// goes wrong in it goes to <paramref name="errors"/>, which the server logs.
    /// </summary>
    public static async Task<int> RunWorkerAsync(Stream input, Stream output, TextWriter errors)
    {
        // For a spare, long before the request arrives.
        _ = Task.Run(WarmUp);

        InspectionRequest? request;

        try
        {
            request = await JsonSerializer.DeserializeAsync<InspectionRequest>(input, _json);
        }
        catch (JsonException)
        {
            // The server went away - or stopped a spare it had no use for - before sending anything.
            return 1;
        }

        if (request is null) return 1;

        var remaining = TimeSpan.FromMilliseconds(request.Deadline - Environment.TickCount64);

        // The server kills the worker at the deadline. Should it no longer be there to - recycled, redeployed, crashed -
        // the worker ends itself just after, whatever it is in the middle of: a computation that never looks at a
        // cancellation token would otherwise run on, keeping a core busy and this app's files open.
        new Thread(() =>
        {
            Thread.Sleep(remaining + WorkerMargin > TimeSpan.Zero ? remaining + WorkerMargin : TimeSpan.Zero);
            Environment.Exit(2);
        })
        { IsBackground = true }.Start();

        // ...and just before it, stops what can be stopped, so the agent reads an answer rather than a killed worker's silence.
        using var timeout = new CancellationTokenSource(remaining > WorkerMargin ? remaining - WorkerMargin : TimeSpan.Zero);

        BlexStoreInspectionDto answer;

        try
        {
            answer = Inspect(request.Source, request.SourceLength, request.IncludeGeneratedCode, timeout.Token);
        }
        catch (OperationCanceledException)
        {
            answer = Refused([], TimedOut);
        }
        catch (OutOfMemoryException)
        {
            answer = Refused([], "REFUSED: the inspection needed more memory than an inspection may use. Send one store at a time.");
        }
        catch (Exception exception)
        {
            await errors.WriteLineAsync(exception.ToString());
            answer = Refused([], Cut($"REFUSED: the inspection failed with {exception.GetType().Name}: {exception.Message}"));
        }

        await JsonSerializer.SerializeAsync(output, answer, _json);
        await output.FlushAsync();

        return 0;
    }

    private static void WarmUp()
    {
        try
        {
            Inspect(WarmUpSource, WarmUpSource.Length, includeGeneratedCode: true, CancellationToken.None);
        }
        catch (Exception)
        {
            // A warm-up that fails leaves the compiling to the real inspection, which reports what goes wrong itself.
        }
    }

    /// <summary>
    /// Runs one inspection in a child process of this same executable. A source can make Roslyn itself work for
    /// hours in a call that never looks at a cancellation token - inferring a type that doubles on every call, for
    /// one - and no check before compiling catches every such shape. A process can be killed where a thread
    /// cannot, and its heap can be capped where a thread's cannot, so whatever the source does, it does to the worker.
    /// </summary>
    private static async Task<BlexStoreInspectionDto> InspectInWorkerAsync(InspectionRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        using var worker = TakeWorker(logger);

        if (worker is null) return Refused([], "REFUSED: the inspection worker could not be started.");

        var errors = ReadErrorsAsync(worker.StandardError);
        BlexStoreInspectionDto? answer = null;

        try
        {
            // Killed at the deadline, which also ends a read of its output that does not look at the token.
            await using (cancellationToken.Register(() => Stop(worker)))
            {
                await using (var input = worker.StandardInput.BaseStream)
                {
                    await JsonSerializer.SerializeAsync(input, request, _json, cancellationToken);
                }

                answer = await ReadAnswerAsync(worker.StandardOutput.BaseStream, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is IOException or JsonException or OperationCanceledException)
        {
            // A worker that died part way - past the heap or the stack it may use - leaves a broken pipe, and no answer
            // or half of one. One stopped at the deadline is reported as such below, once its pipes are closed.
        }
        finally
        {
            Stop(worker);
        }

        // It ends with the worker; one that could not be killed is not waited on.
        var errorOutput = await Task.WhenAny(errors, Task.Delay(WorkerMargin, CancellationToken.None)) == errors ? await errors : string.Empty;

        // Process.Dispose leaves the redirected streams it handed out to whoever read them.
        worker.StandardOutput.Dispose();
        worker.StandardError.Dispose();

        if (answer is not null)
        {
            if (errorOutput.Length > 0) logger.LogWarning("The store inspection worker reported an error: {Errors}", errorOutput);

            return answer;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var exitCode = worker.WaitForExit(WorkerMargin) ? worker.ExitCode : (int?)null;

        logger.LogWarning("The store inspection worker stopped without an answer (exit code {ExitCode}): {Errors}", exitCode, errorOutput);

        return Refused([], "REFUSED: the inspection stopped without an answer - a source that needs more memory or a deeper stack " +
                           "than an inspection may use ends it this way. Send one store at a time.");
    }

    /// <summary>
    /// The worker's answer, read to its end but no further than <see cref="MaxAnswerBytes"/>: one that runs on past
    /// it is refused, not buffered.
    /// </summary>
    private static async Task<BlexStoreInspectionDto?> ReadAnswerAsync(Stream output, CancellationToken cancellationToken)
    {
        using var answer = new MemoryStream();
        var buffer = new byte[81_920];
        int read;

        while ((read = await output.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (answer.Length + read > MaxAnswerBytes)
            {
                return Refused([], "REFUSED: the answer for this source is longer than this server returns. Send one store at a time.");
            }

            answer.Write(buffer, 0, read);
        }

        answer.Position = 0;

        return await JsonSerializer.DeserializeAsync<BlexStoreInspectionDto>(answer, _json, cancellationToken);
    }

    /// <summary>
    /// What a worker writes to its error output, kept up to <see cref="MaxLoggedErrorLength"/> and read to the end
    /// past that, so the worker never blocks on a full pipe. It ends when the worker does.
    /// </summary>
    private static async Task<string> ReadErrorsAsync(StreamReader errors)
    {
        var kept = new StringBuilder();
        var buffer = new char[4_096];

        try
        {
            int read;

            while ((read = await errors.ReadAsync(buffer)) > 0)
            {
                kept.Append(buffer, 0, Math.Min(read, MaxLoggedErrorLength - kept.Length));
            }
        }
        catch (Exception exception) when (exception is IOException or ObjectDisposedException)
        {
            // The pipe broke with the worker; what came before is what there is.
        }

        return kept.ToString();
    }

    /// <summary>
    /// The worker for the next inspection - the spare started ahead of it when there is one - and a new spare in its
    /// place. Starting the runtime and compiling the compiler is most of what a small inspection costs, and a spare
    /// pays for it while nothing waits on it. Every worker still inspects one source and ends, so nothing one source
    /// did to a worker is there for the next; an unused spare ends with the server, when its input closes.
    /// </summary>
    private static Process? TakeWorker(ILogger logger)
    {
        var worker = Interlocked.Exchange(ref _spare, null);

        if (worker is { HasExited: true })
        {
            worker.Dispose();
            worker = null;
        }

        worker ??= StartWorker(logger);

        if (StartWorker(logger) is { } spare && Interlocked.CompareExchange(ref _spare, spare, null) is not null)
        {
            Stop(spare);
            spare.Dispose();
        }

        return worker;
    }

    private static Process? StartWorker(ILogger logger)
    {
        var command = _workerCommand.Value;
        var start = new ProcessStartInfo(command[0]);

        foreach (var argument in command.Skip(1)) start.ArgumentList.Add(argument);

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
        catch (Win32Exception exception)
        {
            logger.LogError(exception, "The store inspection worker could not be started with {Command}.", string.Join(' ', start.ArgumentList.Prepend(start.FileName)));
            return null;
        }
    }

    private static void Stop(Process worker)
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

    /// <summary>
    /// One inspection, as the server hands it to a worker: the source as cut to <see cref="MaxSourceLength"/>, the
    /// length it had before, and the <see cref="Environment.TickCount64"/> at which the server kills the worker.
    /// </summary>
    private sealed record InspectionRequest(string Source, int SourceLength, bool IncludeGeneratedCode, long Deadline);

    private static BlexStoreInspectionDto Inspect(string source, int sourceLength, bool includeGeneratedCode, CancellationToken cancellationToken)
    {
        var notes = new List<string>();

        if (sourceLength > source.Length)
        {
            notes.Add($"INCOMPLETE ANALYSIS: the source is {sourceLength} characters and only the first {source.Length} were inspected. " +
                      "Send one store at a time.");
        }

        var userTree = CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Store.cs");
        var usingsTree = CSharpSyntaxTree.ParseText(ImplicitUsings, _parseOptions, path: "GlobalUsings.cs");

        // Measured on the syntax, before the compiler folds anything: folding a constant made by doubling
        // another one materializes it, and nothing cancels that.
        if (FindUnfoldableConstant(userTree.GetRoot(cancellationToken), cancellationToken) is { } constantRefusal) return Refused(notes, constantRefusal);

        var compilation = CSharpCompilation.Create(
            "BlexStoreInspection",
            [userTree, usingsTree],
            _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var lengths = new TypeLengths();

        // Before the generator runs, which prints every one of these types.
        if (CheckGenerationBudget(compilation, userTree, lengths, cancellationToken) is { } budgetRefusal) return Refused(notes, budgetRefusal);

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

        // A compiler error prints the types it is about. Measured on the compilation the errors come from - the
        // generated members in it, as the errors see them - and only when there is an error to print, so a store
        // that compiles is bound once.
        if (compilerErrors.Length > 0 && HasOversizedType(updated, userTree, lengths, cancellationToken)) return Refused(notes, OversizedType);

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
    /// Why the constants in this source are not folded, or null when they may be: one would be longer than the
    /// whole source may be, or they name one another in a cycle that has no length.
    /// <para>
    /// Measured on the syntax, without folding anything: each expression from its parts, and a name that stands for
    /// a constant declared here by that constant's own measure - so <c>const string B = A + A;</c> measures twice
    /// <c>A</c>, as folding would make it. A name stands for every constant it could mean: a local of a block around
    /// it, a field of a type around it or of one of their base types or of a <c>using static</c> type - or, qualified,
    /// a field of the type it is qualified with, or of its base types. Types are told apart by name alone, so a
    /// name can stand for more constants than it means, never for fewer: the measure can overcount, not undercount.
    /// </para>
    /// <para>
    /// A constant is measured after the constants it names, and an expression after its parts, so neither a chain
    /// of thousands of constants nor one of thousands of <c>+</c> is recursed through.
    /// </para>
    /// </summary>
    private static string? FindUnfoldableConstant(SyntaxNode root, CancellationToken cancellationToken)
    {
        const long over = MaxSourceLength + 1L;

        var oversized = $"REFUSED: a constant in this source is longer than the {MaxSourceLength} characters the source may have, so it " +
                        "was built by concatenating another constant with itself. Constants that long are not inspected.";

        var constants = root.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Select(declarator => declarator switch
            {
                { Initializer: null } => null,
                { Parent.Parent: FieldDeclarationSyntax field } when field.Modifiers.Any(SyntaxKind.ConstKeyword)
                    => new Constant(declarator, (field.Parent as TypeDeclarationSyntax)?.Identifier.ValueText, null),
                { Parent.Parent: LocalDeclarationStatementSyntax { IsConst: true } local }
                    => new Constant(declarator, null, local.Parent is GlobalStatementSyntax global ? global.Parent : local.Parent),
                _ => null
            })
            .OfType<Constant>()
            .ToArray();

        var ids = Enumerable.Range(0, constants.Length).ToArray();
        var spelled = ids.ToLookup(id => constants[id].Name, StringComparer.Ordinal);
        var fields = ids.Where(id => constants[id].Type is not null).ToLookup(id => (constants[id].Type!, constants[id].Name));
        var locals = ids.Where(id => constants[id].Scope is not null).ToLookup(id => (constants[id].Scope!, constants[id].Name));

        var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToArray();

        // A type named through `using Alias = Some.Type;` is the type the alias stands for.
        var aliases = usings
            .Where(directive => directive.Alias is not null && Rightmost(directive.NamespaceOrType) is not null)
            .ToLookup(directive => directive.Alias!.Name.Identifier.ValueText, directive => Rightmost(directive.NamespaceOrType)!, StringComparer.Ordinal);

        var staticallyImported = usings
            .Where(directive => directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            .Select(directive => Rightmost(directive.NamespaceOrType))
            .OfType<string>()
            .ToArray();

        var bases = root.DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .SelectMany(type => (type.BaseList?.Types ?? default).Select(baseType => (Type: type.Identifier.ValueText, Base: Rightmost(baseType.Type))))
            .Where(pair => pair.Base is not null)
            .ToLookup(pair => pair.Type, pair => pair.Base!, StringComparer.Ordinal);

        var resolved = new Dictionary<IdentifierNameSyntax, int[]>();
        var measured = new Dictionary<ExpressionSyntax, long>();
        var values = new long[constants.Length];

        // What each constant's initializer names, measured before it is (Tarjan's strongly connected components,
        // without recursion): a component comes out after every component it names, and constants that name one
        // another in a cycle come out together.
        var dependencies = constants
            .Select(constant => constant.Value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Where(IsValue).SelectMany(Resolve).Distinct().ToArray())
            .ToArray();

        var index = Enumerable.Repeat(-1, constants.Length).ToArray();
        var low = new int[constants.Length];
        var onStack = new bool[constants.Length];
        var stack = new Stack<int>();
        var frames = new Stack<(int Id, int Next)>();
        var visited = 0;

        foreach (var start in ids)
        {
            if (index[start] >= 0) continue;

            Visit(start);

            while (frames.TryPop(out var frame))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var (id, next) = frame;

                if (next < dependencies[id].Length)
                {
                    frames.Push((id, next + 1));

                    var dependency = dependencies[id][next];

                    if (index[dependency] < 0) Visit(dependency);
                    else if (onStack[dependency]) low[id] = Math.Min(low[id], index[dependency]);

                    continue;
                }

                if (frames.TryPeek(out var parent)) low[parent.Id] = Math.Min(low[parent.Id], low[id]);

                if (low[id] != index[id]) continue;

                var component = new List<int>();
                int member;

                do
                {
                    member = stack.Pop();
                    onStack[member] = false;
                    component.Add(member);
                }
                while (member != id);

                if (MeasureComponent(component) is { } refusal) return refusal;
            }
        }

        // Every expression, with every constant measured: attribute arguments and default values are folded too.
        foreach (var expression in root.DescendantNodes().OfType<ExpressionSyntax>().Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if ((measured[expression] = Measure(expression)) > MaxSourceLength) return oversized;
        }

        return null;

        void Visit(int id)
        {
            index[id] = low[id] = visited++;
            stack.Push(id);
            onStack[id] = true;
            frames.Push((id, 0));
        }

        // Constants in a cycle are measured round by round up to a fixed point. One that is still growing after a
        // round per constant grows without end: such a cycle does not compile (CS0110) - or it is made of distinct
        // constants that share names, which cannot be told apart here.
        string? MeasureComponent(List<int> component)
        {
            var cyclic = component.Count > 1 || dependencies[component[0]].Contains(component[0]);

            for (var round = 0; round <= component.Count; round++)
            {
                var grew = false;

                foreach (var id in component)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var length = Evaluate(constants[id].Value);

                    if (length > MaxSourceLength) return oversized;

                    if (length > values[id])
                    {
                        values[id] = length;
                        grew = true;
                    }
                }

                if (grew is false || cyclic is false) return null;
            }

            return "REFUSED: constants in this source name one another in a cycle, so what they fold to cannot be measured. A constant " +
                   "defined through itself does not compile (CS0110); distinct constants that share a name are told apart here " +
                   "by the type they are qualified with.";
        }

        // An expression's parts come before it in reversed document order, so each finds its parts already measured.
        long Evaluate(ExpressionSyntax expression)
        {
            foreach (var part in expression.DescendantNodesAndSelf().OfType<ExpressionSyntax>().Reverse())
            {
                measured[part] = Measure(part);
            }

            return measured[expression];
        }

        long Measure(ExpressionSyntax expression)
        {
            var length = expression switch
            {
                LiteralExpressionSyntax literal => literal.Token.ValueText.Length,
                IdentifierNameSyntax name => Named(name),
                MemberAccessExpressionSyntax access => measured[access.Name],
                AliasQualifiedNameSyntax alias => measured[alias.Name],
                BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.AddExpression) => measured[binary.Left] + measured[binary.Right],
                BinaryExpressionSyntax binary => Math.Max(measured[binary.Left], measured[binary.Right]),
                ParenthesizedExpressionSyntax parenthesized => measured[parenthesized.Expression],
                CastExpressionSyntax cast => measured[cast.Expression],
                CheckedExpressionSyntax @checked => measured[@checked.Expression],
                ConditionalExpressionSyntax conditional => Math.Max(measured[conditional.WhenTrue], measured[conditional.WhenFalse]),
                InterpolatedStringExpressionSyntax interpolated => interpolated.Contents.Sum(content => content switch
                {
                    InterpolatedStringTextSyntax text => text.TextToken.ValueText.Length,
                    InterpolationSyntax hole => measured[hole.Expression],
                    _ => 0L
                }),
                // nameof(x) is a constant too: the last name in x, no longer than x is written.
                InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" }, ArgumentList.Arguments: [var argument] }
                    => argument.Expression.Span.Length,
                _ => 0L
            };

            return Math.Min(length, over);
        }

        // A name that stands for no constant declared here is a variable, which is not folded, or a constant of a
        // referenced assembly, which this source cannot have made long.
        long Named(IdentifierNameSyntax name)
        {
            if (IsValue(name) is false) return 0;

            var candidates = Resolve(name);

            return candidates.Length == 0 ? name.Identifier.ValueText.Length : candidates.Max(id => values[id]);
        }

        // The type or namespace a member is read from - or a value, whose members are no constants - is not itself folded.
        static bool IsValue(IdentifierNameSyntax name) => name.Parent is not MemberAccessExpressionSyntax access || access.Expression != name;

        int[] Resolve(IdentifierNameSyntax name)
        {
            if (resolved.TryGetValue(name, out var known)) return known;

            var spelling = name.Identifier.ValueText;

            int[] candidates;

            if (spelled.Contains(spelling) is false)
            {
                candidates = [];
            }
            else if (name.Parent is MemberAccessExpressionSyntax access && access.Name == name)
            {
                candidates = Rightmost(access.Expression) is { } type ? [.. Declared(type, spelling).Distinct()] : [.. spelled[spelling]];
            }
            else
            {
                candidates = [.. name.Ancestors()
                    .SelectMany(scope => scope is TypeDeclarationSyntax type ? Declared(type.Identifier.ValueText, spelling) : locals[(scope, spelling)])
                    .Distinct()];

                if (candidates.Length == 0) candidates = [.. staticallyImported.SelectMany(type => Declared(type, spelling)).Distinct()];
            }

            return resolved[name] = candidates;
        }

        // The constants a type of this name declares or inherits - from any type so named, through any alias.
        IEnumerable<int> Declared(string type, string name)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>([type]);

            while (pending.TryPop(out var next))
            {
                if (seen.Add(next) is false) continue;

                foreach (var id in fields[(next, name)]) yield return id;

                foreach (var other in bases[next].Concat(aliases[next])) pending.Push(other);
            }
        }

        static string? Rightmost(ExpressionSyntax? expression) => expression switch
        {
            SimpleNameSyntax name => name.Identifier.ValueText,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            _ => null
        };
    }

    /// <summary>A constant declared in the source: a field of the type named <see cref="Type"/>, or a local of the block <see cref="Scope"/>.</summary>
    private sealed record Constant(VariableDeclaratorSyntax Declarator, string? Type, SyntaxNode? Scope)
    {
        public string Name => Declarator.Identifier.ValueText;

        public ExpressionSyntax Value => Declarator.Initializer!.Value;
    }

    /// <summary>
    /// Whether a type written or inferred anywhere in the source prints longer than <see cref="MaxTypeLength"/>.
    /// Every expression is asked for its type - a type written out is an expression too, and so is the alias
    /// standing for one - so a type built by doubling is caught before a compiler error prints it.
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

    /// <summary>Why the generator is not run over these stores, or null when it may be: the types and names it would print.</summary>
    private static string? CheckGenerationBudget(Compilation compilation, SyntaxTree userTree, TypeLengths lengths, CancellationToken cancellationToken)
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

            if (storeName > MaxTypeLength) return OversizedType;

            long states = 0;
            long computeds = 0;
            names += storeName + NameArgumentLength(storeAttribute);

            foreach (var member in type.GetMembers())
            {
                var blexAttributes = member.GetAttributes()
                    .Where(attribute => attribute.AttributeClass?.ToDisplayString() is "Bit.Blex.BlexStateAttribute" or "Bit.Blex.BlexComputedAttribute" or "Bit.Blex.BlexActionAttribute" or "Bit.Blex.BlexEffectAttribute")
                    .ToArray();

                var attributes = blexAttributes.Select(attribute => attribute.AttributeClass!.ToDisplayString()).ToArray();

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
                    var length = lengths.Of(printedType);

                    if (length > MaxTypeLength) return OversizedType;

                    names += storeName + length;

                    if (names > MaxTypeNameLength) return TooMuchGeneratedCode;
                }

                // Printed with the member as well: an action's label, and its parameters' default values (no
                // longer than the source, FindUnfoldableConstant has made sure, so reading them is cheap).
                names += blexAttributes.Sum(NameArgumentLength);

                if (member is IMethodSymbol withParameters)
                {
                    names += withParameters.Parameters.Sum(parameter => parameter is { HasExplicitDefaultValue: true, ExplicitDefaultValue: string text } ? text.Length : 0L);
                }
            }

            pairs += states * computeds;

            if (pairs > MaxStateComputedPairs || names > MaxTypeNameLength) return TooMuchGeneratedCode;
        }

        return null;

        // A Name the source declared as an array - on an attribute of its own under a Blex name - has no Value to read.
        static long NameArgumentLength(AttributeData attribute)
        {
            var name = attribute.NamedArguments.FirstOrDefault(argument => argument.Key == "Name").Value;

            return name.Kind != TypedConstantKind.Array && name.Value is string text ? text.Length : 0;
        }
    }

    private const string TooMuchGeneratedCode =
        "REFUSED: the stores in this source would make the generator emit more code than this server inspects. Send one store at a time.";

    private static string OversizedType => $"REFUSED: a type in this source prints longer than {MaxTypeLength} characters, so it was built by " +
                                           "doubling (nested using aliases, nested anonymous types, or generic inference over its own result). " +
                                           "Types that long are not inspected.";

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

        // Nullability included: the generator prints string? where a compiler error prints string.
        private readonly Dictionary<ISymbol, long> _lengths = new(SymbolEqualityComparer.IncludeNullability);

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
                // (int count, string name): the element names print as well.
                INamedTypeSymbol { IsTupleType: true } tuple => tuple.TupleElements
                    .Aggregate(2L, (sum, element) => Add(sum, Add(Of(element.Type), (element.IsExplicitlyNamedTupleElement ? element.Name.Length + 1 : 0) + 2))),
                // <anonymous type: int x, string y> - nameless, it prints every member it has, and those can be anonymous too.
                INamedTypeSymbol { IsAnonymousType: true } anonymous => anonymous.GetMembers()
                    .Select(member => member switch
                    {
                        IPropertySymbol property => Add(Of(property.Type), property.Name.Length + 3),
                        IMethodSymbol { MethodKind: MethodKind.DelegateInvoke } invoke => invoke.Parameters
                            .Aggregate(Add(Of(invoke.ReturnType), 2), (sum, parameter) => Add(sum, Add(Of(parameter.Type), 2))),
                        _ => 0L
                    })
                    .Aggregate((long)"<anonymous type: >".Length, Add),
                INamedTypeSymbol named => named.TypeArguments
                    .Aggregate(Add(Of((ISymbol?)named.ContainingType ?? named.ContainingNamespace), named.Name.Length + 3), (sum, argument) => Add(sum, Add(Of(argument), 2))),
                INamespaceSymbol { IsGlobalNamespace: true } => "global::".Length,
                INamespaceSymbol ns => Add(Of(ns.ContainingNamespace), ns.Name.Length + 1),
                _ => symbol.Name.Length
            };

            if (symbol is ITypeSymbol { NullableAnnotation: NullableAnnotation.Annotated, IsValueType: false }) length = Add(length, 1);

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
            Message = Cut(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture)),
            Source = source,
            Line = span.IsValid && span.Path == userTree.FilePath ? span.StartLinePosition.Line + 1 : null
        };
    }

    /// <summary>A message cut to <see cref="MaxMessageLength"/>, never between the halves of a surrogate pair.</summary>
    private static string Cut(string message)
    {
        if (message.Length <= MaxMessageLength) return message;

        var length = char.IsHighSurrogate(message[MaxMessageLength - 1]) ? MaxMessageLength - 1 : MaxMessageLength;

        return $"{message[..length]}... (cut at {MaxMessageLength} of {message.Length} characters)";
    }
}
