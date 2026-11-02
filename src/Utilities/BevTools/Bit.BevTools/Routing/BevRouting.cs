using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>One routable page: what the app's router can match.</summary>
/// <param name="Templates">Its <c>@page</c> templates, in declaration order.</param>
/// <param name="PageType">The page's full type name.</param>
/// <param name="PageDisplayName">The page's type name without its namespace.</param>
/// <param name="Assembly">The assembly the router found it in.</param>
/// <param name="Authorization">Its <c>[Authorize]</c> attributes as written ("Authorize(Roles = Admin)"); empty when it has none.</param>
/// <param name="AllowAnonymous">Whether it carries <c>[AllowAnonymous]</c>.</param>
/// <param name="RenderMode">Its <c>[RenderMode*]</c> attribute, or null.</param>
/// <param name="Layout">Its <c>@layout</c>, or null (the router's default layout applies).</param>
public sealed record BevRouteEntry(
    IReadOnlyList<string> Templates,
    string PageType,
    string PageDisplayName,
    string Assembly,
    IReadOnlyList<string> Authorization,
    bool AllowAnonymous,
    string? RenderMode,
    string? Layout);

/// <summary>The route the session is on: from the <c>RouteView</c> (or <c>AuthorizeRouteView</c>) the router rendered.</summary>
public sealed record BevCurrentRoute(
    string? Uri,
    string? PageType,
    string? PageDisplayName,
    long? PageInstanceId,
    string? Template,
    bool TemplateInferred,
    IReadOnlyList<KeyValuePair<string, string>> RouteValues,
    IReadOnlyList<KeyValuePair<string, string>> Query,
    string? Layout,
    string Source);

/// <summary>What the Routing panel shows: the current route, every route the router knows, and how both were read.</summary>
public sealed record BevRoutingSnapshot(BevCurrentRoute Current, IReadOnlyList<BevRouteEntry> Routes, IReadOnlyList<string> RouteAssemblies, string RoutesSource);

/// <summary>
/// Reads routing from the components the session already tracks: the <c>RouteView</c> holds the matched
/// <see cref="RouteData"/> as a public parameter and the <c>Router</c> its assemblies, so nothing here reaches into
/// framework internals. An app whose router runs during static rendering (per-page interactivity) has neither in
/// an interactive session, and the snapshot says so instead of guessing.
/// </summary>
internal static class BevRoutingReader
{
    private static readonly ConcurrentDictionary<Assembly, BevRouteEntry[]> RoutesByAssembly = new();

    public static BevRoutingSnapshot Read(BevToolsSession session)
    {
        var live = session.Components.All.Where(r => !r.IsDisposed && !r.IsBevTools).ToArray();
        var routeView = live.Where(r => r.Component is RouteView).OrderByDescending(r => r.LastRenderAt ?? r.CreatedAt).FirstOrDefault();
        var router = live.Where(r => r.Component is Router).OrderByDescending(r => r.CreatedAt).FirstOrDefault();

        var (assemblies, routesSource) = RouteAssemblies(router?.Component as Router, live);
        var routes = assemblies.SelectMany(RoutesOf).OrderBy(r => r.Templates.FirstOrDefault() ?? "", StringComparer.OrdinalIgnoreCase).ToArray();
        return new BevRoutingSnapshot(
            CurrentRoute(session, routeView, live, routes),
            routes,
            assemblies.Select(a => a.GetName().Name ?? a.FullName ?? "?").ToArray(),
            routesSource);
    }

    private static (Assembly[] Assemblies, string Source) RouteAssemblies(Router? router, BevComponentRecord[] live)
    {
        if (router is not null)
        {
            var assemblies = new List<Assembly>();
            if (router.AppAssembly is { } app)
            {
                assemblies.Add(app);
            }

            if (router.AdditionalAssemblies is { } additional)
            {
                assemblies.AddRange(additional);
            }

            return (assemblies.Distinct().ToArray(), "From the Router's AppAssembly and AdditionalAssemblies.");
        }

        // No router in this session: scan the assemblies of the pages it did render, and say that is what this is.
        var pages = live.Select(r => r.Type).Where(t => t.IsDefined(typeof(RouteAttribute), false)).Select(t => t.Assembly).Distinct().ToArray();
        return (pages, pages.Length == 0
            ? "No Router in this session and no routable page rendered: the router runs during static rendering here (per-page interactivity), outside this session."
            : "No Router in this session (it runs during static rendering here), so these are the routes of the assemblies whose pages this session rendered; others can exist.");
    }

    private static BevRouteEntry[] RoutesOf(Assembly assembly) => RoutesByAssembly.GetOrAdd(assembly, static a =>
    {
        Type[] types;
        try
        {
            // Exported types, as the router's own route table reads them: an internal page is not routable.
            types = a.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.Where(t => t is not null).ToArray()!;
        }
        catch
        {
            return [];
        }

        var routes = new List<BevRouteEntry>();
        foreach (var type in types)
        {
            if (!typeof(IComponent).IsAssignableFrom(type) || type.IsAbstract)
            {
                continue;
            }

            var templates = type.GetCustomAttributes<RouteAttribute>(inherit: false).Select(r => r.Template).ToArray();
            if (templates.Length == 0)
            {
                continue;
            }

            var attributes = type.GetCustomAttributes(inherit: true);
            routes.Add(new BevRouteEntry(
                templates,
                type.FullName ?? type.Name,
                TypeNames.Short(type),
                a.GetName().Name ?? "?",
                attributes.Where(x => x.GetType().Name == "AuthorizeAttribute").Select(DescribeAuthorize).ToArray(),
                attributes.Any(x => x.GetType().Name == "AllowAnonymousAttribute"),
                attributes.OfType<RenderModeAttribute>().Select(m => BevRenderModes.Describe(m.Mode)).FirstOrDefault(),
                attributes.OfType<LayoutAttribute>().Select(l => TypeNames.Short(l.LayoutType)).FirstOrDefault()));
        }

        return routes.ToArray();
    });

    /// <summary>
    /// "Authorize", "Authorize(Roles = Admin)", "Authorize(Policy = CanEdit)". Read by name: Microsoft.AspNetCore.Authorization
    /// is the app's dependency, not BevTools'.
    /// </summary>
    private static string DescribeAuthorize(object attribute)
    {
        var parts = new List<string>();
        foreach (var name in (string[])["Policy", "Roles", "AuthenticationSchemes"])
        {
            if (attribute.GetType().GetProperty(name)?.GetValue(attribute) is string { Length: > 0 } value)
            {
                parts.Add(name + " = " + value);
            }
        }

        return parts.Count == 0 ? "Authorize" : "Authorize(" + string.Join(", ", parts) + ")";
    }

    private static BevCurrentRoute CurrentRoute(BevToolsSession session, BevComponentRecord? routeViewRecord, BevComponentRecord[] live, BevRouteEntry[] routes)
    {
        var uri = CurrentUri(session, out var query);
        if (routeViewRecord?.Component is not RouteView { RouteData: { } routeData } routeView)
        {
            return new BevCurrentRoute(uri, null, null, null, null, false, [], query, null,
                routeViewRecord is null ? "No RouteView in this session; only NavigationManager's URI is known." : "The RouteView has no RouteData yet.");
        }

        var pageType = routeData.PageType;
        var page = live.Where(r => r.Type == pageType).OrderByDescending(r => r.LastRenderAt ?? r.CreatedAt).FirstOrDefault();
        string? template = null;
        var inferred = false;
        template = routeData.Template;
        if (template is null && routes.FirstOrDefault(r => r.PageType == pageType.FullName) is { Templates.Count: 1 } single)
        {
            // When RouteData does not carry the template that matched, a page with one @page has only one.
            template = single.Templates[0];
            inferred = true;
        }

        var values = routeData.RouteValues
            .Select(v => new KeyValuePair<string, string>(v.Key, session.Inspector.Describe(v.Key, v.Value).Display))
            .ToArray();
        var layout = pageType.GetCustomAttribute<LayoutAttribute>()?.LayoutType ?? routeView.DefaultLayout;
        return new BevCurrentRoute(uri, pageType.FullName, TypeNames.Short(pageType), page?.InstanceId, template, inferred, values, query,
            layout is null ? null : TypeNames.Short(layout), "Read from the " + TypeNames.Short(routeView.GetType()) + "'s RouteData.");
    }

    /// <summary>The base-relative URI, redacted like every navigation the timeline records, and its query values.</summary>
    private static string? CurrentUri(BevToolsSession session, out IReadOnlyList<KeyValuePair<string, string>> query)
    {
        query = [];
        string relative;
        try
        {
            if (session.Services.GetService<NavigationManager>() is not { } navigation)
            {
                return null;
            }

            relative = "/" + navigation.ToBaseRelativePath(navigation.Uri);
        }
        catch
        {
            // NavigationManager throws until the host initialized it (a prerender that has not started, a disposed scope).
            return null;
        }

        var redacted = session.Redactor.RedactUrl(relative);
        var q = redacted.IndexOf('?');
        if (q >= 0)
        {
            var end = redacted.IndexOf('#', q);
            var pairs = (end < 0 ? redacted[(q + 1)..] : redacted[(q + 1)..end]).Split('&', StringSplitOptions.RemoveEmptyEntries);
            query = pairs.Select(pair =>
            {
                var eq = pair.IndexOf('=');
                var key = Uri.UnescapeDataString(eq < 0 ? pair : pair[..eq]);
                var value = eq < 0 ? "" : Uri.UnescapeDataString(pair[(eq + 1)..].Replace('+', ' '));
                // Already redacted by name and shape in the URL; the inspector applies value access on top.
                return new KeyValuePair<string, string>(key, value == BevRedactor.RedactedValue ? value : session.Inspector.Describe(key, value).Display);
            }).ToArray();
        }

        return session.ValueAccess == BevValueAccess.None && q >= 0 ? redacted[..q] + "?" + BevRedactor.HiddenValue : redacted;
    }
}
