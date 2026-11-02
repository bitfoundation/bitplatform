using Bit.Belemetry.Demo.Client.Pages;
using Microsoft.AspNetCore.Components;

namespace Bit.Belemetry.Demo.Client;

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
    public const string Name = "Belemetry";

    public const string PackageId = "Bit.Belemetry";

    /// <summary>The library's folder in the repository, which the GitHub links point into.</summary>
    public const string ProjectPath = "src/Utilities/Belemetry";

    /// <summary>What the library is, in the two or three sentences the footer has room for.</summary>
    public const string Tagline =
        "Belemetry is one lightweight telemetry SDK for every Blazor app: page views, web vitals, errors and " +
        "fetch spans from the browser, ILogger, Activity and Meter from .NET - OpenTelemetry-native without the " +
        "OpenTelemetry packages, under per-feature consent. This site measures itself with it.";

    public static readonly string[] Badges = ["MIT", "net10.0", "net11.0"];

    /// <summary>Words the search palette suggests when a query finds nothing.</summary>
    public static readonly string[] SearchHints = ["consent", "otlp", "web vitals", "ilogger", "azure"];

    /// <summary>
    /// Whether Ctrl+K / Cmd+K opens the site's search palette. "/" always does. A demo whose own
    /// library answers to Ctrl+K turns this off so the two do not fight over the key.
    /// </summary>
    public static readonly bool PaletteCtrlK = true;

    public static readonly SiteNavItem[] Primary =
    [
        new("/", "Home"),
        new("/web-app", "Docs", MatchesDocs: true),
        new("/live", "Live telemetry"),
        new("/mcp-server", "MCP server"),
    ];

    public const string RepositoryUrl = "https://github.com/bitfoundation/bitplatform";

    public static string ProjectUrl => $"{RepositoryUrl}/tree/develop/{ProjectPath}";

    public static string NuGetUrl => $"https://www.nuget.org/packages/{PackageId}";

    /// <summary>
    /// The page's source file below the project folder (<c>Demo/Client/Pages/ConsentPage.razor</c>) - the path
    /// the MCP server hands it out under, too. Razor gives a component the namespace of its folder, so the path
    /// below Demo/Client is the type's namespace past this assembly's root, one folder a segment.
    /// </summary>
    public static string SourcePathOf(Type page)
    {
        var root = typeof(SiteInfo).Namespace!;
        var relative = page.FullName!.StartsWith(root + ".", StringComparison.Ordinal)
            ? page.FullName[(root.Length + 1)..]
            : page.Name;

        return $"Demo/Client/{relative.Replace('.', '/')}.razor";
    }

    /// <summary>The page's source file on GitHub.</summary>
    public static string SourceUrlOf(Type page) => $"{RepositoryUrl}/blob/develop/{ProjectPath}/{SourcePathOf(page)}";
}

/// <summary>
/// The single source of truth for the site's information architecture. The docs nav panel, the
/// header's search palette, the previous/next pager, the breadcrumb and the footer's link columns are
/// all driven from here, so adding a page means adding exactly one entry.
/// <para>
/// It is also where the demo's MCP server gets the title, description, keywords and source file it reports
/// for a demo page from <c>SearchBelemetry</c>, so the site's search and an AI agent looking for a worked
/// example read the same words.
/// </para>
/// </summary>
public static class DocsCatalog
{
    /// <summary>
    /// The landing page. It is not a document - it has no place in the reading order, the nav panel or the
    /// pager - but it is still a page the MCP server's search can point at.
    /// </summary>
    public static readonly DocsPageInfo Landing =
        new("/", "Home",
            "The landing page: what Belemetry is, the OpenTelemetry-native pitch without the OpenTelemetry packages, and the twelve consent features.",
            "home landing overview introduction start opentelemetry otel features",
            typeof(Home));

    public static readonly DocsSection[] Sections =
    [
        new("Getting started",
        [
            new("/web-app", "Blazor Web App",
                "Setting Belemetry up in a Blazor Web App: both projects, BelemetryScript in the head, BelemetryInitializer in Routes, for the Server, WebAssembly and Auto render modes and static SSR.",
                "setup install getting started web app blazor server webassembly auto ssr static render mode belemetryscript belemetryinitializer app.razor routes.razor _imports.razor using download size",
                typeof(WebAppPage)),
            new("/wasm", "Standalone WebAssembly",
                "Setting Belemetry up in a standalone Blazor WebAssembly app: one Program.cs, BelemetryInitializer, and the optional script tag in index.html.",
                "setup install standalone webassembly wasm index.html script tag program.cs cors",
                typeof(StandaloneWasmPage)),
            new("/hybrid", "Blazor Hybrid",
                "Setting Belemetry up in MAUI, WPF and WinForms: relay transport, file persistence, and flushing on suspend.",
                "setup install hybrid maui wpf winforms blazorwebview relay onsleep flush mobile desktop",
                typeof(HybridPage)),
            new("/live", "Live telemetry",
                "Your own spans, logs, events and metrics, read back from the demo server's in-process OTLP receiver as you use the site.",
                "live viewer telemetry feed spans logs events metrics session receiver otlp json realtime",
                typeof(LiveTelemetryPage)),
        ]),
        new("What it collects",
        [
            new("/consent", "Consent and GDPR",
                "Per-feature consent: groups, opt-in by default, Global Privacy Control, the remembered choice shared across tabs, purge on revoke and the pending buffer, sessions and user ids - with a live consent panel and SetUserAsync.",
                "consent gdpr privacy opt-in banner features groups diagnostics analytics monitoring gpc global privacy control revoke purge pending persist tabs ibelemetryconsent session sessions user tracking setuserasync setuser pseudo id user.id sampling sessionsampleratio",
                typeof(ConsentPage)),
            new("/browser", "Browser instrumentation",
                "Page views, rage clicks, web vitals, document load, resources, long tasks, fetch/XHR with traceparent, errors and their limits, console, custom events and the JavaScript API - each with a live trigger.",
                "browser javascript page view click interaction rage click web vitals lcp inp cls fcp ttfb back forward cache bfcache restore document load resource long task fetch xhr traceparent aborterror _other error exception duplicatewindow maxperminute console trackevent trackpageview trackexception getstats window.bitbelemetry api data-bit-belemetry-name data-bit-belemetry-ignore",
                typeof(BrowserPage)),
            new("/dotnet", ".NET instrumentation",
                "ILogger logs, ActivitySource traces and Meter metrics collected by the BCL listeners - levels, sources, sampling, temporality, buckets, the cardinality limit, and Belemetry's own health meter.",
                "dotnet ilogger logging logs activity activitysource traces tracing sampling meter metrics counter histogram temporality cardinality buckets logging:belemetry entity framework efcore hangfire self telemetry health belemetry.records.dropped belemetry.records.exported httpactivitypropagationsupport",
                typeof(DotnetPage)),
        ]),
        new("Exporters",
        [
            new("/otlp", "OTLP",
                "The OTLP exporter: protobuf, JSON or gRPC, headers and BrowserHeaders, per-signal endpoints, BrowserEndpoint, retries and partial success, and the Aspire dashboard, OpenTelemetry Collector (with CORS), Grafana, Jaeger and Honeycomb.",
                "otlp exporter opentelemetry protocol protobuf json grpc headers browserheaders endpoint browserendpoint retry partial success aspire dashboard collector cors grafana tempo loki jaeger honeycomb datadog",
                typeof(OtlpPage)),
            new("/azure-monitor", "Azure Monitor",
                "The native Application Insights exporter: connection string, Entra ID token provider, MaskIp and Live Metrics.",
                "azure monitor application insights appinsights connection string entra id token maskip live metrics quickpulse track",
                typeof(AzureMonitorPage)),
            new("/exporters", "Console, in-memory and custom",
                "The console exporter for development, BelemetryInMemoryExporter for tests and in-app viewers, and writing your own IBelemetryExporter.",
                "console in-memory memory custom exporter ibelemetryexporter belemetrybatch tests viewer",
                typeof(ExportersPage)),
        ]),
        new("Shaping the data",
        [
            new("/privacy", "Privacy",
                "Data minimization on both sides: query-parameter redaction, credential attribute names, pattern scrubbing, stripped fragments, blocked attributes, the user agent and the value length limit.",
                "privacy redaction redact redactedattributes password secret scrub scrubbing pii email jwt card ip query string fragment blockedattributes useragent collectuseragent maxvaluelength truncate",
                typeof(PrivacyPage)),
            new("/processors", "Processors",
                "IBelemetryProcessor: enrich, rewrite or drop spans, logs and metrics on the .NET side - including the records relayed from the browser - and addProcessor and setAttributes in the browser.",
                "processor processors ibelemetryprocessor enrich filter drop tenant onspan onlog onmetric tail sampling addprocessor setattributes browser javascript",
                typeof(ProcessorsPage)),
            new("/persistence", "Offline persistence",
                "Failed exports kept on the device - localStorage in the browser, files in Hybrid and on servers - and retried when the network is back; which statuses are retried, and the unload keepalive budget.",
                "offline persistence retry localstorage files queue maxbytes maxage retryinterval network keepalive unload 429 503",
                typeof(PersistencePage)),
        ]),
        new("Hosting",
        [
            new("/server-pipeline", "Server pipeline",
                "Server.CollectDotnetTelemetry: the server's own logs, request spans and metrics, governed by operator features rather than one user's consent.",
                "server pipeline collectdotnettelemetry asp.net core aspnetcore request spans kestrel operator features resource detection",
                typeof(ServerPipelinePage)),
            new("/transport", "Transport",
                "How browser records leave the page: Direct (fetch to the endpoints, CORS) or Relay (through .NET over JS interop, no credentials in the page), and how a server guards the relay.",
                "transport direct relay cors credentials interop circuit hybrid rate limit untrusted relay_limit",
                typeof(TransportPage)),
        ]),
        new("Reference",
        [
            new("/semantic-conventions", "Semantic conventions",
                "The attributes and events Belemetry emits - resource, page views, interactions, web vitals, fetch, errors, ILogger - and the feature bits and storage keys.",
                "semantic conventions attributes events resource session.id url.full web_vital exception feature bits storage keys reference",
                typeof(SemanticConventionsPage)),
            new("/azure-mapping", "Azure mapping",
                "How every record maps onto Application Insights: envelope types, tags, properties and retries.",
                "azure mapping application insights envelope requests dependencies exceptions pageviews custommetrics customevents traces tags ai.operation.id",
                typeof(AzureMappingPage)),
            new("/configuration", "Configuration",
                "Every option of BelemetryOptions and its sub-options, with types, defaults and documentation reflected from the shipped assembly.",
                "configuration options reference belemetryoptions defaults settings appsettings environment variables otel_sdk_disabled errors maxvaluelength all options api",
                typeof(ConfigurationPage)),
            new("/troubleshooting", "Troubleshooting",
                "Why telemetry does not arrive and how to find out: consent, getStats() and its drop reasons, the diagnostics switches, CORS, the Aspire dashboard's gRPC endpoint and missing WebAssembly metrics - with a live check of the page.",
                "troubleshooting debug diagnostics nothing arrives missing telemetry getstats getcontext dropped rejected not_consented queue_full error_limit cors aspire grpc metricssupport warnings _imports",
                typeof(TroubleshootingPage)),
            new("/mcp-server", "MCP server",
                "The demo's Model Context Protocol server: the tools, prompts and resources that let an AI agent set up and review Belemetry correctly.",
                "mcp model context protocol ai agent llm tools prompts resources claude cursor copilot server integration generator review",
                typeof(McpServerPage)),
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
