using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Bit.Butil;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Console = System.Console;

namespace ButilManualTests;

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
    /// <summary>Butil services <see cref="ConsumerComponent"/> injects - trimming must keep these.</summary>
    private static readonly string[] MustSurvive = ["Clipboard", "Cookie", "LocalStorage"];

    /// <summary>
    /// Butil services nothing in this project references - trimming must remove these. They are also how
    /// the report tells a trimmed run from an untrimmed one, without hard-coding a total type count.
    /// </summary>
    private static readonly string[] MustBeTrimmed = ["Fetch", "Geolocation", "IndexedDb", "MediaRecorder", "WebAuthn", "Window"];

    private static async Task<int> Main()
    {
        var failures = new List<string>();
        var assembly = typeof(BitButil).Assembly;

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

        var absent = MustBeTrimmed.Where(name => discoveredNames.Contains(name) is false).ToArray();
        var mode = absent.Length == MustBeTrimmed.Length ? "TRIMMED"
                 : absent.Length == 0 ? "UNTRIMMED"
                 : "PARTIAL";

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

        foreach (var name in MustSurvive.Where(name => discoveredNames.Contains(name) is false))
        {
            failures.Add($"{name} is used by ConsumerComponent but did not survive trimming.");
        }

        if (mode == "PARTIAL")
        {
            var unexpected = MustBeTrimmed.Where(name => discoveredNames.Contains(name)).ToArray();
            failures.Add($"unused services survived trimming: {string.Join(", ", unexpected)}");
        }

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

        try
        {
            await component.Use();
            Console.WriteLine("  calls through the stub JS runtime completed");
        }
        catch (Exception exception)
        {
            // Not a failure: the stub returns default for everything, so a service is free to reject it.
            // Activation is what matters, and that already succeeded above.
            Console.WriteLine($"  calls threw (expected with a stub runtime): {exception.GetType().Name}");
        }
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

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Enumerating the trimmed assembly's surviving types is exactly what this harness measures.")]
    private static (Type Type, Type ServiceType)[] DiscoverButilServices(Assembly assembly)
        => [.. assembly.GetTypes()
                .Select(type => (Type: type, Attribute: type.GetCustomAttribute<ButilServiceAttribute>(inherit: false)))
                .Where(entry => entry.Attribute is not null)
                .Select(entry => (entry.Type, entry.Attribute!.ServiceType))];

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
