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
    /// Butil services <see cref="ConsumerComponent"/> injects - trimming must keep these, and must remove
    /// every other <see cref="ButilServiceAttribute"/> class, since this list is the whole of what the
    /// project references.
    /// </summary>
    private static readonly string[] MustSurvive = ["Clipboard", "Cookie", "Geolocation", "LocalStorage", "Window"];

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
            // passing - goes unreported. ConsumerComponent references exactly MustSurvive, so anything else
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
    private static void VerifyInteropContract(Assembly assembly, bool trimmed, InteropManifest? manifest, string? manifestError, string[] serviceNames, List<string> failures)
    {
        var contracts = InteropContract.Capture(assembly, ConsumerComponent.ExercisedPayloadTypes);

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
    /// to all of them: the report still says "57 of 57 registered, PASS" while consumers hit "Cannot provide
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
