using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace Bit.Brouter;

/// <summary>
/// Discovers attribute-routed components (<c>@page</c> / <c>[Route]</c>) across one or more assemblies,
/// translating each declared route template into the form Bit.Brouter matches against. This is what lets
/// routes live colocated with their pages instead of being hand-declared as one big <see cref="Broute"/>
/// tree, mirroring the built-in <c>Router.AppAssembly</c> / <c>AdditionalAssemblies</c> model (including
/// lazily-loaded assemblies added at runtime).
/// </summary>
internal static class BrouteScanner
{
    /// <summary>A single route discovered from a <c>[Route]</c> attribute on a routable component.</summary>
    internal readonly record struct DiscoveredRoute(
        string Template,
        // [param:] as well as [property:]: a positional record copies the constructor argument into the
        // property's backing field, and the trimmer requires the value stored there to carry the same
        // annotation (IL2069 otherwise).
        [param: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        [property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type ComponentType);

    /// <summary>
    /// Scans <paramref name="appAssembly"/> and <paramref name="additionalAssemblies"/> for public or
    /// internal component types annotated with one or more <see cref="RouteAttribute"/> and returns the
    /// discovered routes. Each <c>[Route]</c> on a component yields one entry, so a component with several
    /// route attributes contributes several routes. Duplicate assemblies are scanned only once. A type that can't
    /// be loaded is skipped, and <paramref name="onTypesSkipped"/> gets why, once per assembly that had any.
    /// </summary>
    /// <remarks>
    /// This reflects over every type in the given assemblies, so it is inherently trim-unsafe: the
    /// consumer is responsible for keeping their routable components (and the parameter types those
    /// components bind) preserved when trimming, exactly as the built-in Blazor Router requires.
    /// </remarks>
    [RequiresUnreferencedCode(
        "Attribute-route discovery reflects over all types in the supplied assemblies to find components " +
        "annotated with [Route]/@page. Ensure routable components are preserved when trimming.")]
    internal static IReadOnlyList<DiscoveredRoute> Discover(
        Assembly? appAssembly,
        IReadOnlyList<Assembly>? additionalAssemblies,
        Action<Assembly, IReadOnlyList<Exception>>? onTypesSkipped = null)
    {
        // Preserve declaration intent: scan the app assembly first so, on an otherwise-identical
        // template tie, an app-level page is registered before one contributed by a referenced
        // library (registration order is the final tie-breaker in Brouter.SelectWinner).
        var seen = new HashSet<Assembly>();
        var results = new List<DiscoveredRoute>();

        ScanAssembly(appAssembly, seen, results, onTypesSkipped);
        if (additionalAssemblies is not null)
        {
            for (int i = 0; i < additionalAssemblies.Count; i++)
            {
                ScanAssembly(additionalAssemblies[i], seen, results, onTypesSkipped);
            }
        }

        return results;
    }

    [RequiresUnreferencedCode("See Discover.")]
    private static void ScanAssembly(Assembly? assembly, HashSet<Assembly> seen, List<DiscoveredRoute> results, Action<Assembly, IReadOnlyList<Exception>>? onTypesSkipped)
    {
        if (assembly is null || seen.Add(assembly) is false) return;

        // Why each type that can't be loaded was skipped, for onTypesSkipped.
        var failures = new List<Exception>();

        // Use GetTypes (not GetExportedTypes): Razor components can be generated as internal, and the
        // built-in Router discovers those too. A partially-loadable assembly throws
        // ReflectionTypeLoadException but still exposes the types that did load via ex.Types.
        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
            failures.AddRange(ex.LoaderExceptions.OfType<Exception>());
        }
        // Mono on WebAssembly throws one of these instead, when a type's base type or interface is in an assembly
        // that isn't loaded yet (a BlazorWebAssemblyLazyLoad one, for example).
        catch (Exception ex) when (IsLoadFailure(ex))
        {
            types = GetLoadableTypes(assembly.ManifestModule, failures);
        }

        foreach (var type in types)
        {
            if (type is null) continue;

            object[] routeAttributes;
            try
            {
                if (type.IsClass is false || type.IsAbstract) continue;
                if (typeof(IComponent).IsAssignableFrom(type) is false) continue;

                // inherit: false - a base component's [Route] should not silently spawn routes for every
                // derived component. This matches the built-in Router, which reads route attributes declared
                // directly on the routable type.
                routeAttributes = type.GetCustomAttributes(typeof(RouteAttribute), inherit: false);
            }
            // A type that loaded can still fail here: Mono resolves a type's interfaces only when it is first asked about
            // them, and reading the [Route]s of a type resolves every attribute it has, one from an assembly that isn't
            // loaded included.
            catch (Exception ex) when (IsLoadFailure(ex))
            {
                failures.Add(ex);
                continue;
            }

            foreach (var attribute in routeAttributes)
            {
                var template = ((RouteAttribute)attribute).Template;
                results.Add(new DiscoveredRoute(NormalizeTemplate(template), type));
            }
        }

        if (failures.Count > 0)
        {
            onTypesSkipped?.Invoke(assembly, failures);
        }
    }

    // What the runtime throws when a type, or a type it refers to, is in an assembly that can't be loaded, or isn't loaded yet.
    private static bool IsLoadFailure(Exception ex) =>
        ex is TypeLoadException or FileNotFoundException or FileLoadException or BadImageFormatException;

    // Resolves the TypeDef rows one by one, so one type that can't be loaded doesn't hide the rest. Row 1 is <Module>.
    [RequiresUnreferencedCode("See Discover.")]
    internal static Type?[] GetLoadableTypes(Module module, List<Exception>? failures = null)
    {
        var types = new List<Type?>();

        for (var row = 2; ; row++)
        {
            try
            {
                types.Add(module.ResolveType(0x02000000 | row));
            }
            catch (ArgumentOutOfRangeException)
            {
                return [.. types];
            }
            catch (Exception ex) when (IsLoadFailure(ex))
            {
                failures?.Add(ex);
            }
        }
    }

    /// <summary>
    /// Translates an ASP.NET Core route template into Brouter's template dialect. The only structural
    /// difference is catch-all syntax: ASP.NET Core accepts the single-star form <c>{*rest}</c> while
    /// Brouter's parser expects the double-star form <c>{**rest}</c>. Everything else (literals,
    /// <c>{id:int}</c> constraints, optional <c>{id?}</c> parameters) is already compatible.
    /// </summary>
    private static string NormalizeTemplate(string? template)
    {
        if (string.IsNullOrEmpty(template)) return "/";

        // Fast path: no single-star catch-all to rewrite.
        if (template.IndexOf("{*", StringComparison.Ordinal) < 0) return template;

        var segments = template.Split('/');
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (segment.StartsWith("{*", StringComparison.Ordinal) &&
                segment.StartsWith("{**", StringComparison.Ordinal) is false)
            {
                segments[i] = "{**" + segment[2..];
            }
        }

        return string.Join('/', segments);
    }
}
