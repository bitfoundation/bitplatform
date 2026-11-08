using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Bit.Belemetry;

/// <summary>
/// Finds the app's page routes - the templates of the <see cref="RouteAttribute"/>s (<c>@page</c>) on its components -
/// which page paths are matched against for <c>url.template</c>, as the Blazor router does.
/// </summary>
internal static class BelemetryPageRoutes
{
    private static readonly Lazy<string[]> _discovered = new(() => Discover(Roots()));

    private static readonly string? _componentsName = typeof(IComponent).Assembly.GetName().Name;

    // Belemetry's own assembly uses components but routes none.
    private static readonly string? _ownName = typeof(BelemetryPageRoutes).Assembly.GetName().Name;

    /// <summary>The routes found from the entry assembly (<see cref="Roots"/>), once per process.</summary>
    public static IReadOnlyList<string> Discovered => _discovered.Value;

    /// <summary>
    /// The entry assembly; where there is none (.NET for Android runs no managed <c>Main</c>), the loaded assemblies that
    /// use components: the app's pages are loaded once its router has rendered, which is before anything asks for them.
    /// </summary>
    private static Assembly[] Roots()
    {
        if (Assembly.GetEntryAssembly() is { } entry) return [entry];
        try
        {
            return [.. AppDomain.CurrentDomain.GetAssemblies().Where(a => a.IsDynamic is false && a.GetName().Name is { } name && IsSkipped(name) is false && UsesComponents(a) is not false)];
        }
        catch (Exception)
        {
            return [];
        }
    }

    /// <summary>
    /// The routes of the components in <paramref name="roots"/> and in the assemblies they reference that use Blazor's
    /// components, and so on through those: the app's own pages, a server's client project's (Auto), a shared page
    /// library's (Hybrid) - the assemblies an app usually hands its router. Past the roots, an assembly that does not use
    /// components is not followed: an app's pages are in its UI libraries, and loading every library it references would
    /// cost a server start-up what it may never use. Framework assemblies are skipped, and so is one that cannot be loaded
    /// (not deployed, or lazy-loaded in WebAssembly and not loaded yet) or read. An assembly whose references cannot be
    /// read (NativeAOT keeps no metadata of them) still gives its own routes.
    /// </summary>
    public static string[] Discover(params Assembly[] roots)
    {
        var routes = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Queue<(Assembly Assembly, bool Root)>();
        foreach (var root in roots)
        {
            if (visited.Add(root.GetName().Name ?? string.Empty)) pending.Enqueue((root, true));
        }

        while (pending.TryDequeue(out var item))
        {
            var (assembly, root) = item;
            AssemblyName[] references;
            try
            {
                references = assembly.GetReferencedAssemblies();
            }
            catch (Exception)
            {
                AddRoutes(assembly, routes, seen);
                continue;
            }

            var usesComponents = references.Any(r => string.Equals(r.Name, _componentsName, StringComparison.Ordinal));
            if (usesComponents) AddRoutes(assembly, routes, seen);
            if (usesComponents is false && root is false) continue;

            foreach (var reference in references)
            {
                if (reference.Name is not { } name || IsSkipped(name) || visited.Add(name) is false) continue;
                try
                {
                    pending.Enqueue((Assembly.Load(reference), false));
                }
                catch (Exception)
                {
                    // No routes from an assembly that is not there.
                }
            }
        }

        return [.. routes];
    }

    /// <summary>
    /// The routes of exactly <paramref name="assemblies"/>, references not followed: what a router given them as its
    /// <c>AppAssembly</c> and <c>AdditionalAssemblies</c> routes.
    /// </summary>
    public static string[] Read(IEnumerable<Assembly> assemblies)
    {
        var routes = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var assembly in assemblies.Distinct()) AddRoutes(assembly, routes, seen);
        return [.. routes];
    }

    private static bool? UsesComponents(Assembly assembly)
    {
        try
        {
            return assembly.GetReferencedAssemblies().Any(r => string.Equals(r.Name, _componentsName, StringComparison.Ordinal));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static bool IsSkipped(string name) =>
        name is "System" or "mscorlib" or "netstandard" or "WindowsBase"
        || name.StartsWith("System.", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.", StringComparison.Ordinal)
        || name == _ownName;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "As for the Blazor router, which finds its routes the same way: an app's own assemblies are not trimmed, and a component the trimmer removed is a page nobody can visit.")]
    private static void AddRoutes(Assembly assembly, List<string> routes, HashSet<string> seen)
    {
        Type[] types;
        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (Exception)
        {
            return;
        }

        foreach (var type in types)
        {
            if (typeof(IComponent).IsAssignableFrom(type) is false) continue;
            try
            {
                foreach (var route in type.GetCustomAttributes<RouteAttribute>(inherit: false))
                {
                    if (string.IsNullOrWhiteSpace(route.Template) is false && seen.Add(route.Template)) routes.Add(route.Template);
                }
            }
            catch (Exception)
            {
                // An attribute that cannot be read is no route.
            }
        }
    }
}
