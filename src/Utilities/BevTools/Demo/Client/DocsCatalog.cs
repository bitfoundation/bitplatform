using Bit.BevTools.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.BevTools.Demo.Client;

/// <summary>
/// One documentation page: <paramref name="Url"/> is its address, <paramref name="Keywords"/> feeds
/// the header's search palette, and <paramref name="PageType"/> is the component the address renders -
/// which is what the "Edit this page on GitHub" link under every page is built from.
/// </summary>
public record DocsPageInfo(string Url, string Title, string Description, string Keywords, Type PageType);

public record DocsSection(string Title, DocsPageInfo[] Pages);

/// <summary>
/// A link in the header. <paramref name="MatchesDocs"/> marks the entry that stands for the docs as a
/// whole: it is the current section on every page no other entry claims.
/// </summary>
public record SiteNavItem(string Href, string Label, bool MatchesDocs = false);

/// <summary>
/// What the shared site chrome in Layout/ - the header, the docs shell, the palette and the footer -
/// says about this particular demo. Those components are the same file in every Utilities demo; this
/// class and <see cref="DocsCatalog"/> are what each demo fills in for itself.
/// </summary>
public static class SiteInfo
{
    /// <summary>The library's name, as the header's wordmark and the breadcrumb's root show it.</summary>
    public const string Name = "BevTools";

    public const string PackageId = "Bit.BevTools";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/BevTools";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "BevTools is an in-app developer tool for Blazor: a live component tree, a render profiler that explains " +
        "why a component rendered, a timeline, state, network, JS interop, errors, DI and circuit diagnostics. " +
        "This site is a Blazor app with BevTools switched on - press Ctrl+Shift+D to open it.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["render", "state", "network", "interop", "circuit"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    /// <remarks>Off here: while the BevTools panel is open, Ctrl+K opens its own search and command palette.</remarks>
    public static readonly bool PaletteCtrlK = false;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/getting-started", "Docs", MatchesDocs: true),
        new("/problems", "Problems"),
    ];

    public const string RepositoryUrl = "https://github.com/bitfoundation/bitplatform";

    public static string ProjectUrl => $"{RepositoryUrl}/tree/develop/{ProjectPath}";

    public static string NuGetUrl => $"https://www.nuget.org/packages/{PackageId}";

    /// <summary>
    /// The page's source file on GitHub. Razor gives a component the namespace of its folder, so the
    /// path below Demo/Client is the type's namespace past this assembly's root, one folder a segment.
    /// </summary>
    public static string SourceUrlOf(Type page)
    {
        var root = typeof(SiteInfo).Namespace!;
        var relative = page.FullName!.StartsWith(root + ".", StringComparison.Ordinal)
            ? page.FullName[(root.Length + 1)..]
            : page.Name;

        return $"{RepositoryUrl}/blob/develop/{ProjectPath}/Demo/Client/{relative.Replace('.', '/')}.razor";
    }
}

/// <summary>
/// The single source of truth for the site's information architecture. The docs nav panel, the
/// header's search palette, the previous/next pager, the breadcrumb and the footer's link columns are
/// all driven from here, so adding a page means adding exactly one entry.
/// </summary>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Start here",
        [
            new("/getting-started", "Getting started",
                "Install the packages, register BevTools in the server and the client, place the panel once and open it with Ctrl+Shift+D.",
                "install setup nuget package AddBitBevToolsServices AddBitBevToolsServerServices BevToolsPanel program.cs MetricsSupport interactive auto enabled development shortcut",
                typeof(GettingStarted)),
            new("/problems", "Problems to diagnose",
                "Ten real problems planted in this app, each with the panels and the steps that lead from the symptom to its cause.",
                "problems index scenarios exercises planted bugs walkthrough diagnose symptoms causes",
                typeof(Problems)),
        ]),
        new("Rendering",
        [
            new("/dashboard", "Dashboard",
                "A component hierarchy with a cascading value, HTTP data and a live clock: the component tree, render causes and renders driven only by a parent.",
                "components tree inspector profiler cascading value CascadingParameter StatsCard LiveClock timer StateHasChanged parent render forced re-render",
                typeof(Dashboard)),
            new("/products", "Products",
                "A 120-row list that re-renders on every keystroke and a search that fires one request per key: render storms, cascades and duplicate HTTP.",
                "render storm cascade ShouldRender FilterState ProductList ProductRow keystroke oninput duplicate requests search debounce profiler by type",
                typeof(Products)),
            new("/counter", "Counter",
                "The baseline: one click, one event, one render - and ten renders from a single event that awaits between them.",
                "counter baseline click event render timeline StateHasChanged await caused by",
                typeof(Counter)),
        ]),
        new("Network & state",
        [
            new("/orders", "Orders",
                "Slow, very slow, flaky and missing endpoints and a burst of identical requests, for the Network panel and the HTTP rules.",
                "network http slow flaky 404 duplicate requests IHttpClientFactory status duration trigger",
                typeof(Orders)),
            new("/checkout", "Checkout",
                "A cart and a validated form whose every action is a named state change with a before/after diff.",
                "state CartState AppState IBevStateProvider AddBevToolsStateProvider diff history form validation EditForm",
                typeof(Checkout)),
        ]),
        new("Interop, errors & circuits",
        [
            new("/interop", "JS interop",
                "Measured, slow, chatty and failing JS calls, JS → .NET callbacks and browser storage, each attributed to the component that made it.",
                "js interop IJSRuntime IJSObjectReference module slow chatty callbacks DotNetObjectReference JSInvokable localStorage sessionStorage redaction",
                typeof(Interop)),
            new("/errors", "Errors",
                "Exceptions from an event handler, an async continuation, a render, a child's OnInitialized and an HTTP call, with the component and the click before each.",
                "errors exceptions ErrorBoundary unhandled stack trace component handler render lifecycle OnInitialized http 404",
                typeof(Errors)),
            new("/circuit", "Circuit",
                "Block the Blazor Server circuit's only thread, or end the circuit with an unhandled exception, and read the circuit diagnostics.",
                "circuit blazor server blocked thread sleep reconnect disconnect inbound message slow DispatchExceptionAsync",
                typeof(Circuit)),
        ]),
    ];

    public static readonly DocsPageInfo[] AllPages = [.. Sections.SelectMany(s => s.Pages)];

    /// <summary>The current address relative to the base, without its query string or fragment.</summary>
    public static string PathOf(NavigationManager nav)
    {
        var path = nav.ToBaseRelativePath(nav.Uri);
        var cut = path.IndexOfAny(['?', '#']);
        return (cut >= 0 ? path[..cut] : path).Trim('/');
    }

    /// <summary>Whether a link's address and a base-relative path name the same page, ignoring case as the router does.</summary>
    public static bool IsSamePath(string href, string path)
        => string.Equals(href.Trim('/'), path.Trim('/'), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Finds the catalog entry for a base-relative path, or null when the page is not in the catalog. A
    /// page whose route takes a parameter is listed by its bare address, so a deeper path falls back to
    /// the entry it starts with.
    /// </summary>
    public static DocsPageInfo? FindByPath(string path)
        => AllPages.FirstOrDefault(p => IsSamePath(p.Url, path))
           ?? AllPages.FirstOrDefault(p => path.Trim('/').StartsWith(p.Url.Trim('/') + "/", StringComparison.OrdinalIgnoreCase));

    /// <summary>Previous/next pages in reading order for the bottom-of-page pager.</summary>
    public static (DocsPageInfo? Previous, DocsPageInfo? Next) NeighborsOf(DocsPageInfo page)
    {
        var index = Array.IndexOf(AllPages, page);
        if (index < 0) return (null, null);
        return (index > 0 ? AllPages[index - 1] : null,
                index < AllPages.Length - 1 ? AllPages[index + 1] : null);
    }

    /// <summary>Section a page belongs to (the breadcrumb's middle step and the palette's group label).</summary>
    public static string SectionOf(DocsPageInfo page)
        => Sections.First(s => s.Pages.Contains(page)).Title;
}
