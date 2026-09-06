using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bit.Butil;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Console = System.Console;

namespace ButilTests.Manual;

/// <summary>
/// Reports - and checks - what survives in a trimmed consumer of Bit.Butil.
/// </summary>
/// <remarks>
/// Run it two ways and compare:
/// <list type="bullet">
/// <item><c>dotnet run</c> - untrimmed, so every <see cref="ButilServiceAttribute"/> class is present and registered.</item>
/// <item><c>dotnet publish</c> then run the produced executable - trimmed with <c>TrimMode=full</c> (what Blazor
/// WebAssembly uses), so only the Butil classes <see cref="ConsumerComponent"/> references should remain.</item>
/// </list>
/// The exit code is non-zero when the outcome does not match, which makes this usable as a regression
/// check on the reflection-based registration and on <see cref="ButilServiceAttribute"/>'s annotated
/// type argument.
/// </remarks>
internal static class Program
{
    /// <summary>
    /// The Butil services this project references - trimming must keep these, and must remove every other
    /// <see cref="ButilServiceAttribute"/> class.
    /// </summary>
    /// <remarks>
    /// Two sources, and both are references a trimmer has to honour: <see cref="ConsumerComponent"/> injects
    /// nine of them the way a razor <c>@inject</c> would, and <see cref="CancellationContract"/> constructs
    /// <c>DigitalCredentials</c>, <c>Fetch</c> and <c>WebOtp</c> directly to check the handles they put on the
    /// wire. So the trimmed run measures a slightly larger consumer than the injected nine alone - the
    /// alternative, checking that contract from a project that does not reference the library, is not a thing
    /// that exists.
    /// </remarks>
    private static readonly string[] MustSurvive =
    [
        "Canvas", "Clipboard", "Cookie", "DigitalCredentials", "Dom", "Fetch", "Geolocation", "LocalStorage",
        "Streams", "WebOtp", "WebRtc", "Window"
    ];

    /// <summary>
    /// Where the untrimmed run records the service roster and the interop contract for the trimmed run to
    /// check against. Relative to the working directory, so both runs see the same file when launched from
    /// the project folder.
    /// </summary>
    private const string ManifestFileName = "interop-manifest.txt";

    /// <summary>
    /// Publish-only file the csproj drops next to a trimmed executable, and the whole of how this program
    /// knows which kind of build it is running as.
    /// </summary>
    /// <remarks>
    /// Deliberately not inferred from the assembly's contents: "some expected service names are missing, so
    /// this must be trimmed" cannot tell a trimmed build from a stale name left behind by a rename, and
    /// guessing wrong there quietly turns the checks below into no-ops.
    /// </remarks>
    private const string TrimmedMarkerFileName = "trimmed-publish.marker";

    private static async Task<int> Main()
    {
        var failures = new List<string>();
        var assembly = typeof(BitButil).Assembly;
        var trimmed = File.Exists(Path.Combine(AppContext.BaseDirectory, TrimmedMarkerFileName));
        var mode = trimmed ? "TRIMMED" : "UNTRIMMED";
        var manifest = InteropContract.Read(ManifestFileName, out var manifestError);

        var services = new ServiceCollection();
        services.AddSingleton<IJSRuntime>(new StubJSRuntime());
        services.AddBitButilServices();

        var registered = services
            .Where(descriptor => descriptor.ServiceType.Assembly == assembly)
            .Select(descriptor => descriptor.ServiceType)
            .ToArray();

        var discovered = DiscoverButilServices(assembly);
        var discoveredNames = discovered.Select(entry => entry.Type.Name).ToHashSet(StringComparer.Ordinal);
        var registeredNames = registered.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        var unattributed = UnattributedServiceCandidates(assembly, discovered);

        Console.WriteLine("=== Bit.Butil trimming report ===");
        Console.WriteLine($"assembly       : {assembly.Location}");
        Console.WriteLine($"assembly size  : {AssemblySizeText(assembly)}");
        Console.WriteLine($"mode           : {mode}");
        Console.WriteLine($"types in asm   : {SafeTypeCount(assembly)}");
        Console.WriteLine($"[ButilService] : {discovered.Length} discovered, {registered.Length} registered");
        Console.WriteLine();

        Console.WriteLine("--- surviving services ---");
        foreach (var (type, serviceType) in discovered.OrderBy(entry => entry.Type.Name, StringComparer.Ordinal))
        {
            var constructors = PublicConstructorCount(type);
            var flags = registeredNames.Contains(type.Name) ? "registered" : "NOT REGISTERED";
            Console.WriteLine($"  {type.Name,-22} ctors={constructors} {flags}");

            // The whole reason ButilServiceAttribute takes an annotated type argument: without it a class
            // survives with zero public constructors and DI activation blows up at runtime instead of at
            // build time.
            if (constructors == 0) failures.Add($"{type.Name} survived with no public constructor - did ButilServiceAttribute lose its PublicConstructors annotation?");
            if (registeredNames.Contains(type.Name) is false) failures.Add($"{type.Name} is marked [ButilService] but was not registered.");

            // A copy-pasted [ButilService(typeof(SomethingElse))] would silently register the wrong service.
            if (serviceType != type) failures.Add($"{type.Name} is marked [ButilService(typeof({serviceType.Name}))] - the argument must be the decorated type itself.");
        }
        Console.WriteLine();

        Console.WriteLine("--- service classes without [ButilService] ---");
        Console.WriteLine($"  {(unattributed.Length == 0 ? "none" : string.Join(", ", unattributed.Select(type => type.Name)))}");
        foreach (var type in unattributed)
        {
            failures.Add($"{type.Name} looks like a Butil service (public class taking an IJSRuntime) but carries no [ButilService], so nothing registers it - injecting it fails at runtime with \"Cannot provide a value for property\".");
        }
        Console.WriteLine();

        // The list above is names, and a renamed service leaves behind a name that matches nothing.
        // Checking them against the roster of services that genuinely exist turns that into a plain "unknown
        // name" failure rather than a check that silently stops asserting anything.
        var roster = trimmed ? manifest?.ServiceNames.ToHashSet(StringComparer.Ordinal) : discoveredNames;
        if (roster is not null)
        {
            foreach (var name in MustSurvive.Where(name => roster.Contains(name) is false))
            {
                failures.Add(trimmed
                    ? $"{name} is an expected Butil service name but the untrimmed capture in {ManifestFileName} has no such service - the name here is stale (renamed or removed), or the manifest is."
                    : $"{name} is an expected Butil service name but this untrimmed build has no [ButilService] class called that - the name here is stale (renamed or removed), or this is really a trimmed run whose {TrimmedMarkerFileName} went missing.");
            }
        }

        if (trimmed)
        {
            foreach (var name in MustSurvive.Where(name => discoveredNames.Contains(name) is false))
            {
                failures.Add($"{name} is used by ConsumerComponent but did not survive trimming.");
            }

            // Every survivor, not a hand-picked sample of the ones expected to go: a sampled list only ever
            // fails for the names someone thought to put in it, so a service that starts surviving for a
            // reason nobody anticipated - a new unconditional reference, a DynamicDependency added in
            // passing - goes unreported. This project references exactly MustSurvive, so anything else
            // still here is either a trimming regression or a reference added without updating that list.
            var unexpected = discoveredNames.Except(MustSurvive, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal).ToArray();
            if (unexpected.Length > 0)
            {
                failures.Add($"services nothing in this project references survived trimming: {string.Join(", ", unexpected)}");
            }
        }

        Console.WriteLine("--- interop contract ---");
        VerifyInteropContract(assembly, trimmed, manifest, manifestError, [.. discoveredNames], failures);
        Console.WriteLine();

        // The JavaScript side of the same story: which script modules this assembly still calls, whether
        // that is the expected set, and what a consumer publishing it would ship. Same code as the
        // publish-time bundler (Bit.Butil.Build), same trimmed assembly.
        Console.WriteLine("--- javascript modules ---");
        var scripts = string.IsNullOrEmpty(assembly.Location) ? null : ScriptTrimming.Run(assembly.Location, trimmed, failures);
        if (scripts is null)
        {
            Console.WriteLine("  NOT CHECKED - see failures");
        }
        else
        {
            Console.WriteLine($"  modules called  : {scripts.Referenced.Length} of {scripts.TotalModules} ({string.Join(", ", scripts.Referenced)})");
            Console.WriteLine($"  bundle would be : {scripts.Included.Length} modules ({string.Join(", ", scripts.Included)})");
            Console.WriteLine($"  full bundle     : {scripts.FullBundleBytes:N0} bytes");
            Console.WriteLine($"  trimmed bundle  : {scripts.TrimmedBundleBytes:N0} bytes ({scripts.TrimmedBundleGzipBytes:N0} gzip, {scripts.TrimmedBundleBrotliBytes:N0} brotli) - {100.0 * scripts.TrimmedBundleBytes / scripts.FullBundleBytes:F1}% of the full bundle");
            Console.WriteLine($"  lazy modules    : {scripts.LazyModulesBytes:N0} bytes downloaded across {scripts.Referenced.Length} self-contained files - the sum of the files, not the distinct JavaScript in them: each one inlines its dependencies, so shared code is counted once per file that carries it");
        }
        Console.WriteLine();

        // The publish-time bundler under test in its own right - the parsing, resolution and writing a real
        // consumer's build can reach but this repository's one assembly never exercises - and the artifacts
        // it works from, down to running an assembled bundle.
        Console.WriteLine("--- script bundling ---");
        var (bundlingPassed, bundlingFailed) = ScriptBundling.Run(string.IsNullOrEmpty(assembly.Location) ? null : assembly.Location, failures);
        Console.WriteLine($"  {bundlingPassed} checks passed, {bundlingFailed} failed");
        Console.WriteLine();

        // The other two signals a publish can trim on, which stand in for ILLink when it never runs: the
        // class-to-module map, and the scan of a consumer's own assemblies that uses it. Checked against the
        // very set trimming produces for the very same code, so the three cannot drift apart.
        Console.WriteLine("--- script scanning ---");
        var (scanningPassed, scanningFailed) = ScriptScanning.Run(string.IsNullOrEmpty(assembly.Location) ? null : assembly.Location, trimmed, failures);
        Console.WriteLine(trimmed
            ? "  not checked in a trimmed run - the map is a question about the untrimmed library"
            : $"  {scanningPassed} checks passed, {scanningFailed} failed");
        Console.WriteLine();

        // And the same feature as MSBuild actually runs it: a real consumer app published with each
        // combination of the switches, and the JavaScript that came out read back off disk. The only checks
        // here that go through a publish rather than through a method call.
        Console.WriteLine("--- script publishing ---");
        var publishingStarted = DateTime.UtcNow;
        var (publishingPassed, publishingFailed) = ScriptPublishing.Run(trimmed, failures);
        Console.WriteLine(trimmed
            ? "  not checked in a trimmed run - this process publishes the fixture itself, to the same answers either way"
            : $"  {publishingPassed} checks passed, {publishingFailed} failed ({(DateTime.UtcNow - publishingStarted).TotalSeconds:N0}s)");
        Console.WriteLine();

        Console.WriteLine("--- lazy scripts ---");
        var (lazyPassed, lazyFailed) = await LazyScripts.Run(failures);
        Console.WriteLine($"  {lazyPassed} checks passed, {lazyFailed} failed");
        Console.WriteLine();

        // The other half of the interop contract: not which members survive, but which arguments the
        // cancellable APIs put on the wire. Checkable without a browser, and only here - the APIs it covers
        // all prompt, so no headless test can reach them.
        Console.WriteLine("--- cancellation contract ---");
        var (cancellationPassed, cancellationFailed) = await CancellationContract.Run(failures);
        Console.WriteLine($"  {cancellationPassed} checks passed, {cancellationFailed} failed");
        Console.WriteLine();

        Console.WriteLine("--- activation ---");
        await using var provider = services.BuildServiceProvider();
        // CreateAsyncScope, not CreateScope: several Butil services implement only IAsyncDisposable, and
        // a synchronous scope dispose throws on those.
        await using var scope = provider.CreateAsyncScope();
        var component = new ConsumerComponent();

        try
        {
            component.Inject(scope.ServiceProvider);
            Console.WriteLine($"  injected: {string.Join(", ", ConsumerComponent.InjectedTypes.Select(type => type.Name))}");
        }
        catch (Exception exception)
        {
            failures.Add($"injection failed: {exception.GetType().Name}: {exception.Message}");
            Console.WriteLine($"  injection FAILED: {exception.Message}");
        }

        // Throwing is not a failure: the stub answers every call with default, so a service handed a null
        // where it expects a DTO is entitled to blow up. Activation is what matters, and that is checked above.
        var (succeeded, threw) = await component.Use();
        Console.WriteLine($"  interop calls: {succeeded} completed, {threw} threw against the stub runtime");
        Console.WriteLine();

        Console.WriteLine(failures.Count == 0
            ? $"RESULT: PASS ({mode})"
            : $"RESULT: FAIL ({mode})");
        foreach (var failure in failures)
        {
            Console.WriteLine($"  - {failure}");
        }

        return failures.Count == 0 ? 0 : 1;
    }

    /// <summary>
    /// Checks the members Bit.Butil reaches by name at runtime - [JSInvokable] callbacks and JSON payload
    /// types - which no missing-service error would ever surface.
    /// </summary>
    /// <remarks>
    /// The untrimmed run records the contract; the trimmed run checks the trimmed assembly against it.
    /// Types the trimmer removed entirely are skipped, because that is the point of the exercise - only a
    /// type that survived while losing members it is reflected over is a defect.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Looking a type up by name and finding it gone is exactly the outcome this harness measures.")]
    [UnconditionalSuppressMessage("Trimming", "IL2057",
        Justification = "Looking a type up by name and finding it gone is exactly the outcome this harness measures.")]
    private static void VerifyInteropContract(Assembly assembly, bool trimmed, InteropManifest? manifest, string? manifestError, string[] serviceNames, List<string> failures)
    {
        // The internal payload roots are looked up by name because nothing outside Bit.Butil can name them,
        // and one that is simply gone from a trimmed assembly is dropped rather than reported: a type the
        // trimmer removed outright is the feature working, the same rule Verify applies below.
        var internalRoots = ConsumerComponent.ExercisedInternalPayloadTypeNames
            .Select(assembly.GetType)
            .OfType<Type>();

        var contracts = InteropContract.Capture(assembly, [.. ConsumerComponent.ExercisedPayloadTypes, .. internalRoots]);

        if (trimmed is false)
        {
            InteropContract.Write(ManifestFileName, new InteropManifest(serviceNames, contracts));
            Console.WriteLine($"  captured {contracts.Length} types ({contracts.Count(contract => contract.IsCallbackTarget)} with [JSInvokable] callbacks, {contracts.Count(contract => contract.IsPayload)} JSON payloads) and {serviceNames.Length} service names");
            Console.WriteLine($"  written to {Path.GetFullPath(ManifestFileName)}");
            return;
        }

        // A manifest that is missing - or present but unreadable - is a failure rather than a skip: without
        // a whole one this half of the harness verifies nothing, and a run that verifies nothing must not
        // be able to report PASS.
        if (manifest is null)
        {
            var reason = manifestError ?? $"no {ManifestFileName} in {Directory.GetCurrentDirectory()}";
            Console.WriteLine($"  NOT VERIFIED - {reason}");
            Console.WriteLine("  run `dotnet run -c Release` from the project folder first, then re-run this executable from there");
            failures.Add($"the interop contract was not checked at all: {reason} - nothing to compare the trimmed assembly against.");
            return;
        }

        var (callbackTargets, payloads, removedTypes, contractFailures) = InteropContract.Verify(assembly, manifest.Types);
        Console.WriteLine($"  {callbackTargets.Length + payloads.Length} surviving types checked, {removedTypes} trimmed away entirely, {contractFailures.Length} problems");
        Console.WriteLine($"  [JSInvokable] targets intact (identifiers): {Format(callbackTargets)}");
        Console.WriteLine($"  JSON payloads intact (properties)        : {Format(payloads)}");
        failures.AddRange(contractFailures);

        static string Format(string[] names) => names.Length == 0 ? "none" : string.Join(", ", names);
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Enumerating the trimmed assembly's surviving types is exactly what this harness measures.")]
    private static (Type Type, Type ServiceType)[] DiscoverButilServices(Assembly assembly)
        => [.. assembly.GetTypes()
                .Select(type => (Type: type, Attribute: type.GetCustomAttribute<ButilServiceAttribute>(inherit: false)))
                .Where(entry => entry.Attribute is not null)
                .Select(entry => (entry.Type, entry.Attribute!.ServiceType))];

    /// <summary>
    /// Butil service classes found by <b>shape</b> - public, constructible, taking an <see cref="IJSRuntime"/> -
    /// that carry no <see cref="ButilServiceAttribute"/>.
    /// </summary>
    /// <remarks>
    /// Every other check here starts from the attribute, so a class that simply never got one is invisible
    /// to all of them: the report still says "64 of 64 registered, PASS" while consumers hit "Cannot provide
    /// a value for property" at runtime. That is the failure mode reflection-based registration introduces -
    /// there is no central <c>AddScoped&lt;T&gt;()</c> list whose absence a reviewer would notice - so it is
    /// the one thing this harness has to find without being told the answer.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Enumerating the surviving types is what this harness measures; a type the trimmer removed cannot be an unregistered service in the consumer's app either.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Reading the constructors the trimmer left behind is the measurement; a class stripped to zero constructors simply does not match the shape.")]
    private static Type[] UnattributedServiceCandidates(Assembly assembly, (Type Type, Type ServiceType)[] discovered)
    {
        var attributed = discovered.Select(entry => entry.Type).ToHashSet();

        return [.. assembly.GetTypes()
            .Where(type => type.IsClass && type.IsPublic && type.IsAbstract is false && type.IsGenericTypeDefinition is false)
            .Where(type => attributed.Contains(type) is false)
            .Where(type => type.GetConstructors().Any(constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(IJSRuntime))))
            // ButilStorage takes an IJSRuntime and is public, but it is the shared base of LocalStorage and
            // SessionStorage rather than a service of its own - it reaches DI through them.
            .Where(type => attributed.Any(service => service.IsSubclassOf(type)) is false)
            .OrderBy(type => type.Name, StringComparer.Ordinal)];
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Reporting the constructors the trimmer actually left behind; a zero count is a result, not an error to avoid.")]
    private static int PublicConstructorCount(Type type) => type.GetConstructors().Length;

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Type count is a size proxy for the report; missing types are the measurement.")]
    private static int SafeTypeCount(Assembly assembly) => assembly.GetTypes().Length;

    private static string AssemblySizeText(Assembly assembly)
    {
        // Empty under single-file publish, where there is no standalone file to measure.
        if (string.IsNullOrEmpty(assembly.Location)) return "n/a (single file)";

        var file = new FileInfo(assembly.Location);
        return file.Exists ? $"{file.Length:N0} bytes" : "n/a";
    }
}
