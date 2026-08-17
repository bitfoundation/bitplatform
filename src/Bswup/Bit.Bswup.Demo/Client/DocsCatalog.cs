using Bit.Bswup.Demo.Client.Pages;

namespace Bit.Bswup.Demo.Client;

/// <summary>
/// One documentation page: <paramref name="Slug"/> is its route (empty for the home page),
/// <paramref name="Keywords"/> are the terms it covers, and <paramref name="PageType"/> is the
/// component the router renders for it - which is what lets the MCP server render a page's
/// documentation on demand instead of keeping a second copy of it.
/// </summary>
public record DocsPageInfo(string Slug, string Title, string Description, string Keywords, Type PageType)
{
    public string Url => Slug.Length == 0 ? "/" : $"/{Slug}";
}

public record DocsSection(string Title, DocsPageInfo[] Pages);

/// <summary>
/// The documentation site's pages, grouped the way Shared/NavMenu.razor groups them.
/// <para>
/// The nav panel writes its links out by hand (it is plain markup with a client-side filter);
/// this catalog exists for everything that needs the pages as data - today the MCP server, which
/// renders any page to Markdown for an AI agent. Adding a page means adding it here too, or the
/// agent-facing docs simply will not know about it.
/// </para>
/// </summary>
public static class DocsCatalog
{
    public static readonly DocsSection[] Sections =
    [
        new("Overview",
        [
            new("", "Introduction",
                "What Bswup is: install progress, controlled updates and offline support for Blazor WebAssembly apps.",
                "introduction overview home what is bswup pwa offline install progress features",
                typeof(HomePage)),
            new("getting-started", "Getting Started",
                "Add install progress, controlled updates and offline support to a Blazor WebAssembly app in six small steps.",
                "install setup nuget package script tag autostart handler service worker steps quick start",
                typeof(GettingStartedPage)),
            new("how-it-works", "How It Works",
                "The mental model: who starts Blazor, what the worker caches, how an update is staged, and which requests the worker takes over.",
                "mental model lifecycle install activate waiting staged claim clients fetch handler cache bucket boot",
                typeof(HowItWorksPage)),
        ]),
        new("Reference",
        [
            new("script-options", "Script Options",
                "Every attribute of the bit-bswup.js script tag, with its default.",
                "scope log sw handler blazorScript updateInterval updateOnVisibility stallTimeout persistStorage options attribute",
                typeof(ScriptOptionsPage)),
            new("service-worker", "Service Worker Settings",
                "Everything configurable in service-worker.js before importing the Bswup engine.",
                "assetsInclude assetsExclude externalAssets defaultUrl assetsUrl prohibitedUrls serverHandledUrls serverRenderedUrls isPassive errorTolerance maxRetries retryDelay cacheVersion mode integrity diagnostics",
                typeof(ServiceWorkerPage)),
            new("events", "Events & Handler",
                "The lifecycle messages Bswup hands to the global handler function, and what each payload carries.",
                "bitBswupHandler BswupMessage downloadStarted downloadProgress downloadFinished updateReady updateFound updateNotFound updateCheckFailed stateChanged activate error reload",
                typeof(EventsPage)),
            new("progress-ui", "Progress UI",
                "The built-in splash: the BswupProgress component, its parameters, and the element ids the script drives.",
                "BswupProgress splash AutoReload ShowAssets ShowLogs HideApp AutoHide AppContainer ChildContent reload button progress bar css",
                typeof(ProgressUIPage)),
            new("javascript-api", "JavaScript API",
                "The global BitBswup object: checkForUpdate, persistStorage, skipWaiting and forceRefresh.",
                "BitBswup checkForUpdate persistStorage skipWaiting forceRefresh polling reset caches unregister",
                typeof(JsApiPage)),
        ]),
        new("Showcase",
        [
            new("playground", "Live Playground",
                "This site running on Bswup: inspect its worker, drive the JavaScript API, and watch the events arrive live.",
                "playground demo live events log inspect registration caches try it",
                typeof(PlaygroundPage)),
        ]),
        new("Guides",
        [
            new("recipes", "Recipes",
                "Production-shaped answers: hosting headers, sub-path deployments, build-stamped cache versions, custom update banners, keeping the API out of the worker.",
                "recipes cache-control headers sub-path base href cacheVersion build stamp update banner api bypass cdn hosting",
                typeof(RecipesPage)),
            new("troubleshooting", "Troubleshooting & FAQ",
                "Symptoms mapped back to their causes: stale versions, blank first loads, assets not caching, offline deep links.",
                "troubleshooting faq problem stale not updating blank splash stuck offline 404 deep link cache error install failed",
                typeof(TroubleshootingPage)),
            new("cleanup", "Backing Out of Bswup",
                "Remove Bswup from a deployed app - or recover clients stuck on a broken worker - with the self-destructing cleanup worker.",
                "cleanup remove uninstall unregister sw-cleanup purge caches recover broken worker rollback",
                typeof(CleanupPage)),
            new("migration", "Migrating to v-10-6-0",
                "What is visible when upgrading: AutoReload, 403 for prohibited URLs, scoped cache buckets, literal string entries.",
                "migration upgrade breaking change v-10-6-0 AutoReload 403 scoped cache bucket string entries updateReady",
                typeof(MigrationPage)),
        ]),
    ];

    public static readonly DocsPageInfo[] AllPages = Sections.SelectMany(s => s.Pages).ToArray();

    /// <summary>Finds the catalog entry for a slug ("" for the home page), or null when there is none.</summary>
    public static DocsPageInfo? FindBySlug(string? slug)
    {
        var trimmed = (slug ?? string.Empty).Trim('/');

        return AllPages.FirstOrDefault(p => string.Equals(p.Slug, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The section a page belongs to.</summary>
    public static string SectionOf(DocsPageInfo page)
        => Sections.First(s => s.Pages.Contains(page)).Title;
}
