using Microsoft.AspNetCore.Components;

namespace Bit.Blex.Demo.Client;

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
    public const string Name = "Blex";

    public const string PackageId = "Bit.Blex";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Blex";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Blex is source-generator-powered reactive state management for Blazor and .NET MAUI: one [BlexState] " +
        "field and one [BlexAction] method, with effects, persistence, undo/redo and Redux DevTools time-travel " +
        "built in. This site is itself a Blex app.";

    public static readonly string[] Badges = ["MIT", "net8.0", "net9.0", "net10.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["effect", "persist", "selector", "undo", "devtools"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/introduction", "Docs", MatchesDocs: true),
        new("/api", "API"),
        new("/mcp", "MCP server"),
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
/// all driven from here, so adding a page means adding exactly one entry. The MCP server's docs tool
/// (Server/Services/DocsPageRenderer.cs) reads the same list, so an agent sees the pages a reader does.
/// </summary>
public static class DocsCatalog
{
    // The page types are named through Pages. on purpose: Stores and Testing are also namespaces
    // (Bit.Blex.Demo.Client.Stores, Bit.Blex.Testing), and a bare name would bind to those first.
    public static readonly DocsSection[] Sections =
    [
        new("Getting started",
        [
            new("/introduction", "Introduction",
                "What Blex is, the whole store a counter takes, and how it compares with Fluxor.",
                "overview why fluxor redux flux comparison packages boilerplate generator",
                typeof(Pages.Introduction)),
            new("/installation", "Installation",
                "The package, the DI registrations, the <BlexProvider> around the router, headless hosts and trimming.",
                "install setup nuget package AddBitBlexServices AddBitBlexStore BlexProvider program.cs aot trimming json",
                typeof(Pages.Installation)),
            new("/quick-start", "Quick start",
                "A complete, reactive, time-travelable counter in three files and about twenty lines.",
                "quick start counter first store tutorial Subscribe BlexComponentBase",
                typeof(Pages.QuickStart)),
        ]),
        new("Core concepts",
        [
            new("/stores", "Stores & state",
                "[BlexStore] and [BlexState]: the reactive container, its fields, change detection and its JSON snapshot.",
                "BlexStore BlexState store state field property SerializeState DeserializeState RestoreState BlexStoreBase equality immutable EnforceActions InstanceKey dispatcher InvokeAsync thread Blazor Server SignalR timer ReportOffDispatcherChanges IBlexDispatcher DetectInPlaceMutations in-place mutation List.Add",
                typeof(Pages.Stores)),
            new("/computed", "Computed state",
                "[BlexComputed]: memoized derived values that are recalculated only after the state changes.",
                "BlexComputed computed derived memoized getter Compute Get invalidate",
                typeof(Pages.Computed)),
            new("/actions", "Actions",
                "[BlexAction] methods, typed action descriptors, dispatch by name, batching, \"Set X\" assignments, EnforceActions, Batch, ResetState and async actions.",
                "BlexAction action dispatch batch ResetState ResetAll reducer mutation Set arguments payload async typed actions descriptor BlexAction Actions TryGetPayload DispatchAsync EnforceActions strict private setter optional default BLEX020 BLEX021 RollbackOnError rollback transaction throw exception undo partial",
                typeof(Pages.Actions)),
            new("/effects", "Effects",
                "[BlexEffect]: async work with a generated loading flag, error slot, cancellation, concurrency policy, debounce, retries and timeouts.",
                "BlexEffect effect async loading error cancel CancellationToken Latest Drop Queue Parallel debounce side effect retry RetryCount backoff timeout TimeoutMilliseconds TimeoutException BLEX022 BLEX023",
                typeof(Pages.Effects)),
            new("/queries", "Server state & queries",
                "BlexQueryClient: a cache for server data - shared fetches, freshness, retries, invalidation, mutations with optimistic rollback, and the prerender handoff.",
                "query queries server state cache BlexQueryClient UseQuery BlexQueryView BlexQuery BlexQueryKey stale StaleTime GcTime invalidate InvalidateAsync mutation BlexMutation optimistic rollback Optimistic SetQueryData GetQueryData FetchAsync PrefetchAsync tanstack react query swr dedupe refetch polling RefetchInterval KeepPreviousData Dehydrate Hydrate",
                typeof(Pages.Queries)),
            new("/scoped-stores", "Scoped stores",
                "One instance of a store per dialog, tab or document: <BlexScope> and CreateScope, instance keys, and persistence that follows each instance.",
                "scoped store scope BlexScope CreateScope BlexStoreScope instance key multiple instances dialog tab document CascadingParameter lifetime per component",
                typeof(Pages.ScopedStores)),
        ]),
        new("Reacting to state",
        [
            new("/subscriptions", "Components & selectors",
                "BlexComponentBase, <BlexSelect>, <BlexView> and selector subscriptions, so only the markup that uses a value re-renders - plus IObservable for Rx.",
                "subscribe subscription selector BlexComponentBase rerender re-render comparer fireImmediately BlexComparers BlexSelect BlexView selective rendering IObservable Observe ObserveActions Rx reactive",
                typeof(Pages.Subscriptions)),
            new("/cross-store", "Cross-store coordination",
                "BlexManager: react to one store's actions from another, and derive values that span stores.",
                "BlexManager cross store SubscribeTo SubscribeToAction typed CreateSelector global state coordination reactor StoreRegistered DispatchAsync WhenAsync when condition wait workflow",
                typeof(Pages.CrossStore)),
            new("/entities", "Entity adapter",
                "BlexEntityState and BlexEntityAdapter: normalized collections with generated CRUD and automatic sorting.",
                "BlexEntityAdapter BlexEntityState entity normalized collection crud AddOne UpdateOne RemoveOne SetAll sort",
                typeof(Pages.Entities)),
        ]),
        new("The pipeline",
        [
            new("/middleware", "Middleware",
                "IBlexMiddleware: observe every action after it applies, veto it before it runs, or report it when it throws.",
                "IBlexMiddleware middleware veto filter UseMiddleware UseFilter typed filter BeforeAction OnAction OnActionError logger validation",
                typeof(Pages.Middleware)),
            new("/errors", "Error isolation",
                "Where the failures Blex keeps out of the dispatch pipeline go - options.OnError and BlexError - and logging, tracing and metrics.",
                "OnError BlexError error isolation exception subscriber failure sink logging ILogger UseLogging tracing OpenTelemetry ActivitySource Meter metrics BlexDiagnostics observability telemetry",
                typeof(Pages.Errors)),
        ]),
        new("State lifecycle",
        [
            new("/persistence", "Persistence",
                "Persist = true: rehydration on startup and on late registration, transient fields, debounced writes, versioning, migrations and sync across tabs.",
                "persistence persist localStorage sessionStorage rehydrate hydrate debounce version migrate SyncAcrossTabs IBlexStorage transient Persist = false late registration scoped",
                typeof(Pages.Persistence)),
            new("/history", "Undo & redo",
                "BlexHistory: in-app undo and redo over the state tree or a chosen scope of it, with labels, jumps, grouping and filters.",
                "BlexHistory undo redo history jump checkpoint groupBy filter scope scoped per store BlexHistory<TStore> Covers",
                typeof(Pages.History)),
            new("/devtools", "DevTools & time travel",
                "The Redux DevTools bridge: every action and state tree streamed to the extension, time travel, the dispatcher, and redaction.",
                "redux devtools time travel extension sanitizer redact RedactDevToolsKeys EnableDevTools jump dispatcher dispatch ACTION",
                typeof(Pages.DevTools)),
        ]),
        new("Quality",
        [
            new("/testing", "Testing",
                "Bit.Blex.Testing: the test harness, the action log, typed payload assertions, deterministic time, replay and waiting on async state.",
                "testing unit test BlexTestHarness BlexActionLog WaitForAsync harness mstest xunit TimeProvider FakeTimeProvider deterministic replay ReplayAsync PayloadsOf",
                typeof(Pages.Testing)),
            new("/diagnostics", "Compile-time diagnostics",
                "Every BLEX diagnostic the generator can report, with the store it rejects and the fix.",
                "diagnostics BLEX001 analyzer generator compile error warning build",
                typeof(Pages.Diagnostics)),
        ]),
        new("Reference",
        [
            new("/api", "API reference",
                "Every public type Blex ships, grouped by assembly and namespace, with the page that explains it.",
                "api reference types members namespace Bit.Blex.Blazor Bit.Blex.Testing reserved json",
                typeof(Pages.ApiReference)),
            new("/mcp", "MCP server",
                "The Model Context Protocol server this site hosts, so an AI agent works against Blex's real API, docs and generator.",
                "mcp model context protocol ai agent tools resources prompts claude copilot server InspectBlexStore",
                typeof(Pages.McpServer)),
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
