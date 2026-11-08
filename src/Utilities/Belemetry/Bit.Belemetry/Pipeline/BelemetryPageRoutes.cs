using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Bit.Belemetry;

/// <summary>
/// Finds the app's page routes - the templates of the <see cref="RouteAttribute"/>s (<c>@page</c>) on its components -
/// which the browser script matches page paths against for <c>url.template</c>, as the Blazor router does.
/// </summary>
internal static class BelemetryPageRoutes
{
    private static readonly Lazy<string[]> _discovered = new(() => Assembly.GetEntryAssembly() is { } entry ? Discover(entry) : []);

    // Belemetry's own assembly uses components but routes none.
    private static readonly string? _ownName = typeof(BelemetryPageRoutes).Assembly.GetName().Name;

    /// <summary>The routes found from the entry assembly, once per process.</summary>
    public static IReadOnlyList<string> Discovered => _discovered.Value;

    /// <summary>
    /// The routes of the components in <paramref name="root"/> and in the assemblies it references that use Blazor's
    /// components, and so on through those: the app's own pages, a server's client project's (Auto), a shared page
    /// library's (Hybrid) - the assemblies an app hands its router. Past the root, an assembly that does not use
    /// components is not followed: an app's pages are in its UI libraries, and loading every library it references would
    /// cost a server start-up what it may never use. Framework assemblies are skipped, and so is one that cannot be loaded
    /// (not deployed, or lazy-loaded in WebAssembly and not loaded yet) or read.
    /// </summary>
    public static string[] Discover(Assembly root)
    {
        var componentsAssembly = typeof(IComponent).Assembly.GetName().Name;
        var routes = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root.GetName().Name ?? string.Empty };
        var pending = new Queue<Assembly>();
        pending.Enqueue(root);

        while (pending.TryDequeue(out var assembly))
        {
            AssemblyName[] references;
            try
            {
                references = assembly.GetReferencedAssemblies();
            }
            catch (Exception)
            {
                continue;
            }

            var usesComponents = references.Any(r => string.Equals(r.Name, componentsAssembly, StringComparison.Ordinal));
            if (usesComponents) AddRoutes(assembly, routes, seen);
            if (usesComponents is false && assembly != root) continue;

            foreach (var reference in references)
            {
                if (reference.Name is not { } name || IsSkipped(name) || visited.Add(name) is false) continue;
                try
                {
                    pending.Enqueue(Assembly.Load(reference));
                }
                catch (Exception)
                {
                    // No routes from an assembly that is not there.
                }
            }
        }

        return [.. routes];
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
