namespace Bit.Belemetry.Demo.Client.Shared;

/// <summary>
/// One entry of the docs navigation. The list itself is <see cref="All"/>, and it is the site's
/// single index: the nav panel groups it, the command palette searches it, the landing page prints
/// it, the breadcrumb reads a page's category out of it and the pager walks it in order. A page
/// therefore appears everywhere by having an entry here, and the five of them can never disagree
/// about what the site contains.
/// <para>
/// It is also where the demo's MCP server gets the description, keywords and route it reports for
/// a demo page in <c>GetBelemetrySourceFile</c>, which is why each entry carries them alongside the
/// file it is implemented in. A page therefore describes itself once, and the site's search, the
/// nav panel and an AI agent looking for a worked example all read the same words.
/// </para>
/// </summary>
/// <param name="Title">The label shown in the nav panel.</param>
/// <param name="Href">The route, relative to the app base. Empty for the landing page.</param>
/// <param name="Category">The group this page is filed under; see <see cref="Groups"/>.</param>
/// <param name="Description">What the page demonstrates, in one sentence.</param>
/// <param name="Keywords">Space-separated terms the page covers, for search and the MCP index.</param>
/// <param name="Source">The page's file name under Client/Pages, for GetBelemetrySourceFile.</param>
public sealed record NavItem(
    string Title,
    string Href,
    string Category,
    string Description,
    string Keywords,
    string Source)
{
    /// <summary>The path the MCP server hands this page's source out under.</summary>
    public string SourcePath => $"Demo/Client/Pages/{Source}";

    /// <summary>The route as a link target, which for the landing page is the app root.</summary>
    public string Url => Href.Length == 0 ? "/" : $"/{Href}";

    /// <summary>The icon the palette and the landing page show beside this page's group.</summary>
    public string Icon => CategoryIcon(Category);

    // The group names, written out once so a typo in an entry below cannot silently invent a
    // new group, and in the order the docs are meant to be read.
    private const string GettingStarted = "Getting started";
    private const string Collect = "What it collects";
    private const string Exporters = "Exporters";
    private const string Shape = "Shaping the data";
    private const string Hosting = "Hosting";
    private const string Reference = "Reference";

    /// <summary>Every demo page, in the order they are meant to be read.</summary>
    public static readonly NavItem[] All =
    [
        new("Home", "", GettingStarted,
            "The landing page: what Belemetry is, the OpenTelemetry-native pitch without the OpenTelemetry packages, and the feature grid.",
            "home landing overview introduction start opentelemetry otel features",
            "Home.razor"),
        new("Blazor Web App", "web-app", GettingStarted,
            "Setting Belemetry up in a Blazor Web App: both projects, BelemetryScript in the head, BelemetryInitializer in Routes, for the Server, WebAssembly and Auto render modes and static SSR.",
            "setup install getting started web app blazor server webassembly auto ssr static render mode belemetryscript belemetryinitializer app.razor routes.razor",
            "WebAppPage.razor"),
        new("Standalone WebAssembly", "wasm", GettingStarted,
            "Setting Belemetry up in a standalone Blazor WebAssembly app: one Program.cs, BelemetryInitializer, and the optional script tag in index.html.",
            "setup install standalone webassembly wasm index.html script tag program.cs",
            "StandaloneWasmPage.razor"),
        new("Blazor Hybrid", "hybrid", GettingStarted,
            "Setting Belemetry up in MAUI, WPF and WinForms: relay transport, file persistence, and flushing on suspend.",
            "setup install hybrid maui wpf winforms blazorwebview relay onsleep flush mobile desktop",
            "HybridPage.razor"),
        new("Live telemetry", "live", GettingStarted,
            "Your own spans, logs, events and metrics, read back from the demo server's in-process OTLP receiver as you use the site.",
            "live viewer telemetry feed spans logs events metrics session receiver otlp json realtime",
            "LiveTelemetryPage.razor"),
        new("Consent and GDPR", "consent", Collect,
            "Per-feature consent: groups, opt-in by default, Global Privacy Control, the remembered choice, purge on revoke and the pending buffer - with a live consent panel.",
            "consent gdpr privacy opt-in banner features groups diagnostics analytics monitoring gpc global privacy control revoke purge pending persist ibelemetryconsent",
            "ConsentPage.razor"),
        new("Browser instrumentation", "browser", Collect,
            "Page views, interactions, web vitals, document load, resources, long tasks, fetch/XHR with traceparent, errors and console - each with a live trigger.",
            "browser javascript page view click interaction web vitals lcp inp cls fcp ttfb document load resource long task fetch xhr traceparent error exception console trackevent trackpageview trackexception data-bit-belemetry-name data-bit-belemetry-ignore",
            "BrowserPage.razor"),
        new(".NET instrumentation", "dotnet", Collect,
            "ILogger logs, ActivitySource traces and Meter metrics collected by the BCL listeners - levels, sources, sampling, temporality, buckets and the cardinality limit.",
            "dotnet ilogger logging logs activity activitysource traces tracing sampling meter metrics counter histogram temporality cardinality buckets logging:belemetry",
            "DotnetPage.razor"),
        new("OTLP", "otlp", Exporters,
            "The OTLP/HTTP exporter: protobuf or JSON, headers, per-signal endpoints, BrowserEndpoint, and the Aspire dashboard, OpenTelemetry Collector (with CORS), Grafana, Jaeger and Honeycomb.",
            "otlp exporter opentelemetry protocol protobuf json headers endpoint browserendpoint aspire dashboard collector cors grafana tempo loki jaeger honeycomb datadog",
            "OtlpPage.razor"),
        new("Azure Monitor", "azure-monitor", Exporters,
            "The native Application Insights exporter: connection string, Entra ID token provider, MaskIp and Live Metrics.",
            "azure monitor application insights appinsights connection string entra id token maskip live metrics quickpulse track",
            "AzureMonitorPage.razor"),
        new("Console, in-memory and custom", "exporters", Exporters,
            "The console exporter for development, BelemetryInMemoryExporter for tests and in-app viewers, and writing your own IBelemetryExporter.",
            "console in-memory memory custom exporter ibelemetryexporter belemetrybatch tests viewer",
            "ExportersPage.razor"),
        new("Privacy", "privacy", Shape,
            "Data minimization on both sides: query-parameter redaction, pattern scrubbing, stripped fragments, blocked attributes and the user agent.",
            "privacy redaction redact scrub scrubbing pii email jwt card ip query string fragment blockedattributes useragent collectuseragent",
            "PrivacyPage.razor"),
        new("Processors", "processors", Shape,
            "IBelemetryProcessor: enrich, rewrite or drop spans, logs and metrics on the .NET side - including the records relayed from the browser.",
            "processor processors ibelemetryprocessor enrich filter drop tenant onspan onlog onmetric",
            "ProcessorsPage.razor"),
        new("Offline persistence", "persistence", Shape,
            "Failed exports kept on the device - localStorage in the browser, files in Hybrid and on servers - and retried when the network is back.",
            "offline persistence retry localstorage files queue maxbytes maxage retryinterval network",
            "PersistencePage.razor"),
        new("Server pipeline", "server-pipeline", Hosting,
            "Server.CollectDotnetTelemetry: the server's own logs, request spans and metrics, governed by operator features rather than one user's consent.",
            "server pipeline collectdotnettelemetry asp.net core aspnetcore request spans kestrel operator features",
            "ServerPipelinePage.razor"),
        new("Transport", "transport", Hosting,
            "How browser records leave the page: Direct (fetch to the endpoints, CORS) or Relay (through .NET over JS interop, no credentials in the page).",
            "transport direct relay cors credentials interop circuit hybrid",
            "TransportPage.razor"),
        new("Semantic conventions", "semantic-conventions", Reference,
            "The attributes and events Belemetry emits - resource, page views, interactions, web vitals, fetch, errors, ILogger - and the feature bits and storage keys.",
            "semantic conventions attributes events resource session.id url.full web_vital exception feature bits storage keys reference",
            "SemanticConventionsPage.razor"),
        new("Azure mapping", "azure-mapping", Reference,
            "How every record maps onto Application Insights: envelope types, tags, properties and retries.",
            "azure mapping application insights envelope requests dependencies exceptions pageviews custommetrics customevents traces tags ai.operation.id",
            "AzureMappingPage.razor"),
        new("Configuration", "configuration", Reference,
            "Every option of BelemetryOptions and its sub-options, with types, defaults and documentation reflected from the shipped assembly.",
            "configuration options reference belemetryoptions defaults settings all options api",
            "ConfigurationPage.razor"),
        new("MCP Server", "mcp-server", Reference,
            "The demo's Model Context Protocol server: the tools, prompts and resources that let an AI agent set up and review Belemetry correctly.",
            "mcp model context protocol ai agent llm tools prompts resources claude cursor copilot server integration generator review",
            "McpServerPage.razor"),
    ];

    /// <summary>
    /// The pages a reader navigates between: everything in <see cref="All"/> except the landing
    /// page, which is not a document and is reached from the header instead.
    /// </summary>
    public static readonly NavItem[] Docs = [.. All.Where(item => item.Href.Length > 0)];

    /// <summary>
    /// <see cref="Docs"/> in groups, in reading order. Both the order of the groups and the order
    /// within them come from the order of <see cref="All"/>, so re-filing a page is a one-line
    /// edit above and nothing here has to be kept in step with it.
    /// </summary>
    public static readonly (string Category, string Icon, NavItem[] Items)[] Groups =
    [
        .. Docs.GroupBy(item => item.Category)
               .Select(group => (group.Key, CategoryIcon(group.Key), group.ToArray()))
    ];

    /// <summary>The entry for a route, or null when the route is not a demo page.</summary>
    public static NavItem? ByHref(string href)
        => Array.Find(All, item => string.Equals(item.Href, href, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The pages either side of <paramref name="href"/> in reading order, for the pager at the
    /// foot of every page. Both are null for a route that is not a demo page - the error and
    /// not-found pages, which are not part of the sequence and have nowhere to go next.
    /// </summary>
    public static (NavItem? Previous, NavItem? Next) Neighbours(string href)
    {
        var index = Array.FindIndex(Docs, item => string.Equals(item.Href, href, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return (null, null);

        return (index > 0 ? Docs[index - 1] : null,
                index < Docs.Length - 1 ? Docs[index + 1] : null);
    }

    /// <summary>
    /// Whether this page answers to <paramref name="term"/>. Keywords are matched as well as the
    /// title and the category: a reader searching for "hover" or "stagger" is naming the feature,
    /// not the page it happens to live on, and every entry carries the terms it covers.
    /// </summary>
    public bool Matches(string term)
        => Title.Contains(term, StringComparison.OrdinalIgnoreCase)
        || Category.Contains(term, StringComparison.OrdinalIgnoreCase)
        || Keywords.Contains(term, StringComparison.OrdinalIgnoreCase)
        || Description.Contains(term, StringComparison.OrdinalIgnoreCase);

    // The one place a group is tied to a picture. Kept as a switch rather than as a field on the
    // record so the entries above stay a list of prose, and so two pages in the same group cannot
    // be given two different icons.
    private static string CategoryIcon(string category) => category switch
    {
        GettingStarted => Icons.Play,
        Collect => Icons.Pulse,
        Exporters => Icons.Send,
        Shape => Icons.Shield,
        Hosting => Icons.Layers,
        Reference => Icons.Book,
        _ => Icons.Book,
    };
}
