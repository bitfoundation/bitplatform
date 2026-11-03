
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>Where the BevTools panel is docked.</summary>
public enum BevToolsDock
{
    Bottom,
    Right,
}

public enum BevToolsTheme
{
    Auto,
    Dark,
    Light,
}

/// <summary>
/// How much of the application's own values BevTools reads: component parameters, cascading values and fields, state
/// provider snapshots, authentication claims, HTTP header values and browser storage previews. Structure, counts,
/// timings, causes, events, requests and errors are recorded at every level; secrets are redacted at every level.
/// </summary>
public enum BevValueAccess
{
    /// <summary>Values are read and shown, objects can be expanded member by member, with redaction.</summary>
    Full,

    /// <summary>
    /// Only scalar values are shown (numbers, booleans, enums, dates, strings that do not look like credentials);
    /// objects and collections show their type and size and are never walked or printed through <c>ToString</c>.
    /// Forced re-render detection needs <see cref="Full"/>, since it compares parameters member by member.
    /// </summary>
    Shallow,

    /// <summary>No application value is read: parameters, cascading values, fields and state show names and types only.</summary>
    None,
}

/// <summary>Thresholds used by the built-in diagnostics rules. All are evidence-based; raise them for noisy apps.</summary>
public sealed class BevDiagnosticsOptions
{
    /// <summary>Renders of a single component within <see cref="RenderStormWindowMs"/> that count as a render storm.</summary>
    public int RenderStormCount { get; set; } = 30;

    public int RenderStormWindowMs { get; set; } = 2000;

    /// <summary>
    /// Renders within 10 seconds, all caused by an ancestor re-rendering, that count as an avoidable render pattern.
    /// Deliberately much lower than <see cref="RenderStormCount"/>: the evidence here is the ratio, not the rate.
    /// </summary>
    public int ParentDrivenRenderCount { get; set; } = 10;

    /// <summary>Number of components rendered in one batch that counts as a render cascade.</summary>
    public int RenderCascadeSize { get; set; } = 25;

    /// <summary>Single render (BuildRenderTree) duration considered slow, in milliseconds.</summary>
    public double SlowRenderMs { get; set; } = 16;

    /// <summary>Identical HTTP requests (method + URL) within <see cref="DuplicateHttpWindowMs"/> that count as duplicates.</summary>
    public int DuplicateHttpCount { get; set; } = 4;

    public int DuplicateHttpWindowMs { get; set; } = 1000;

    /// <summary>
    /// Requests to one endpoint that differ only in their query values (a search box, a filter) within
    /// <see cref="HttpBurstWindowMs"/> that count as a burst: the pattern of a request per keystroke.
    /// </summary>
    public int HttpBurstCount { get; set; } = 4;

    public int HttpBurstWindowMs { get; set; } = 2000;

    /// <summary>
    /// Requests to one endpoint, each for a different id in its path (a number, GUID or hash segment), within
    /// <see cref="HttpBurstWindowMs"/> that count as one request per item: the N+1 pattern of a list whose rows each
    /// load their own data.
    /// </summary>
    public int HttpPerItemCount { get; set; } = 5;

    public double SlowHttpMs { get; set; } = 2000;

    public double SlowJsInteropMs { get; set; } = 250;

    /// <summary>JS interop calls to the same identifier within one second that count as excessive.</summary>
    public int ExcessiveJsInteropCount { get; set; } = 50;

    /// <summary>Event handler duration considered slow (it blocks the renderer), in milliseconds.</summary>
    public double SlowEventHandlerMs { get; set; } = 100;

    /// <summary>Occurrences of the same error that count as a repeated error.</summary>
    public int RepeatedErrorCount { get; set; } = 3;

    /// <summary>
    /// Log entries with one message template (whatever their values), from one category, within ten seconds that count
    /// as a log flood: logging on a hot path, such as a render or a list item.
    /// </summary>
    public int LogFloodCount { get; set; } = 100;

    /// <summary>
    /// Bytes a component's render fragment allocates per render, on average over its renders of the last 30 seconds, that
    /// count as a heavy render (rule <c>render.allocations</c>). Where the runtime counts allocations per thread.
    /// </summary>
    public long HeavyRenderAllocationBytes { get; set; } = 256 * 1024;

    /// <summary>
    /// A user interaction's time to the next paint that counts as slow (rule <c>browser.slow-interaction</c>): 200 ms is
    /// where the Web Vitals' Interaction to Next Paint stops being good.
    /// </summary>
    public double SlowInteractionMs { get; set; } = 200;

    /// <summary>
    /// The blocking time of one frame (the part beyond 50 ms per task, input kept waiting) that counts as a blocked main
    /// thread (rule <c>browser.long-frame</c>).
    /// </summary>
    public double LongFrameBlockingMs { get; set; } = 150;

    /// <summary>
    /// Executions of one SQL command (Entity Framework Core, read from its logs) within two seconds that count as one
    /// query per item (rule <c>db.per-item</c>): the N+1 pattern of a list whose rows each load their own data.
    /// </summary>
    public int DbPerItemCount { get; set; } = 10;

    /// <summary>A database command's elapsed time, as Entity Framework Core reported it, that counts as slow (rule <c>db.slow</c>).</summary>
    public double SlowDbCommandMs { get; set; } = 500;

    /// <summary>Rule ids that are disabled.</summary>
    public HashSet<string> DisabledRules { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Runs the rules as data arrives (at most once a second) while the panel is closed too. Off by default: they then
    /// run while the panel is open, when a frontend reads the session and when it is exported - and most of them look
    /// back over a window of 10 to 60 seconds, so an export made after a longer quiet spell misses what they would have
    /// found in the activity before it. Turn it on for a run whose export a CI gate checks (<see cref="BevSessionCheck"/>);
    /// it needs the panel component on the page, open or not.
    /// </summary>
    public bool EvaluateWhileClosed { get; set; }
}

public sealed class BevToolsUiOptions
{
    public bool OpenByDefault { get; set; }

    public BevToolsDock Dock { get; set; } = BevToolsDock.Bottom;

    public BevToolsTheme Theme { get; set; } = BevToolsTheme.Auto;

    /// <summary>How often the UI checks for new activity while open, in milliseconds. The UI never re-renders without changes.</summary>
    public int RefreshIntervalMs { get; set; } = 250;

    /// <summary>
    /// When true (default) the open tab re-reads captured data as it arrives. Turn off on a very busy app and use
    /// the Refresh button; capture itself keeps running either way.
    /// </summary>
    public bool LiveUpdates { get; set; } = true;

    /// <summary>Keyboard shortcut that toggles the panel. Format: modifiers + key, e.g. "Ctrl+Shift+D".</summary>
    public string ToggleShortcut { get; set; } = "Ctrl+Shift+D";

    /// <summary>Show a small floating badge when the panel is closed.</summary>
    public bool ShowBadge { get; set; } = true;

    /// <summary>
    /// The link that opens a source location in an editor, with <c>{path}</c>, <c>{line}</c> and <c>{column}</c> filled in:
    /// VS Code by default; <c>vscode-insiders://file/{path}:{line}:{column}</c>, or null for no links (paths are still
    /// shown). Locations come from the app's symbols (PDB), so there are none in a build without them.
    /// </summary>
    public string? EditorUrlTemplate { get; set; } = "vscode://file/{path}:{line}:{column}";
}

/// <summary>Configuration for <c>AddBitBevToolsServices</c>.</summary>
public sealed class BitBevToolsOptions
{
    /// <summary>
    /// Explicitly enables or disables BevTools. When null (default) BevTools is enabled only when the host environment
    /// reports Development (<c>IHostEnvironment</c> on the server, <c>IWebAssemblyHostEnvironment</c> in the browser).
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>Maximum number of timeline events kept in memory per session (circuit).</summary>
    public int MaxEvents { get; set; } = 5000;

    /// <summary>Maximum render samples kept per component instance.</summary>
    public int MaxRendersPerComponent { get; set; } = 100;

    /// <summary>
    /// Upper bound on tracked component instances per session. Components are referenced weakly, so this only caps
    /// bookkeeping; when it is reached new components are not tracked and the UI says so instead of growing forever.
    /// </summary>
    public int MaxTrackedComponents { get; set; } = 20_000;

    /// <summary>How long a disposed component stays visible in the tree and inspector before its record is dropped.</summary>
    public int DisposedComponentRetentionSeconds { get; set; } = 60;

    public int MaxErrors { get; set; } = 300;

    public int MaxHttpRequests { get; set; } = 500;

    public int MaxJsInteropCalls { get; set; } = 1000;

    /// <summary>Maximum number of log entries kept per session (Logs panel).</summary>
    public int MaxLogs { get; set; } = 1000;

    public int MaxStateChangesPerProvider { get; set; } = 100;

    /// <summary>
    /// Maximum number of diagnostic findings kept per session; the ones no rule has reported for the longest time are
    /// dropped first. Several rules report one finding per occurrence (each slow request, each cascading batch).
    /// </summary>
    public int MaxFindings { get; set; } = 200;

    /// <summary>Attribute renders to UI events, parent renders and navigation using <c>Activity</c> and render-batch analysis.</summary>
    public bool TrackRenderCauses { get; set; } = true;

    /// <summary>
    /// Record one timeline entry per component render. This is what lets the timeline answer "what did this click
    /// cause?", and it is also the single largest cost BevTools adds, because renders are the highest-frequency
    /// event in a Blazor application. Turning it off keeps render counts, durations, causes and the profiler intact;
    /// only the per-render rows in the timeline disappear. Consider it for very large or very render-heavy apps.
    /// </summary>
    public bool RecordRenderEvents { get; set; } = true;

    /// <summary>Track HTTP requests made through <c>IHttpClientFactory</c> clients.</summary>
    public bool TrackHttp { get; set; } = true;

    /// <summary>Capture request/response header names and values (sensitive headers are always redacted). Bodies are <see cref="CaptureHttpBodies"/>'s.</summary>
    public bool CaptureHttpHeaders { get; set; } = true;

    /// <summary>
    /// Capture request and response bodies (off by default). Text only - JSON, XML, forms, text, GraphQL - at most
    /// <see cref="MaxHttpBodyBytes"/>, redacted (JSON and form values under a sensitive name, anything credential-shaped),
    /// and never at the cost of the request: a stream is never read, and a response only when its length is known and
    /// within the limit. Only where <see cref="ValueAccess"/> is Full, and in a process that serves many users only in
    /// Development. Bodies are shown in the Network panel and written to exports: they routinely hold personal data.
    /// </summary>
    public bool CaptureHttpBodies { get; set; }

    /// <summary>The most of a body <see cref="CaptureHttpBodies"/> reads (16 KB).</summary>
    public int MaxHttpBodyBytes { get; set; } = 16 * 1024;

    /// <summary>
    /// Name the component (and the application method it called through) that sent each HTTP request, read from the
    /// stack when the request starts: a stack walk per request, not per render. Off, a request is attributed only to the
    /// UI event it ran in.
    /// </summary>
    public bool CaptureHttpCallers { get; set; } = true;

    /// <summary>
    /// Capture what the application's <c>ILogger</c>s write while this session's code runs (Logs panel): level, category,
    /// event id, message and template, the template's values, scopes, exception, the activity's trace id, the UI event
    /// it ran in and the component that logged. The host's logging configuration filters first, as for every provider
    /// (the provider's alias is <c>BevTools</c>); <see cref="MinimumLogLevel"/> applies after it. An application entry at
    /// Error or above is also an error (Errors panel). Off, only the warnings and errors Blazor itself logs are captured.
    /// </summary>
    public bool TrackLogs { get; set; } = true;

    /// <summary>The lowest level <see cref="TrackLogs"/> records. Information by default; Debug and Trace are usually far more.</summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Name the component (and the application method in between) that wrote each log entry, read from the stack when it
    /// is logged: a stack walk per recorded entry. Off, an entry is attributed only to the UI event it ran in.
    /// </summary>
    public bool CaptureLogCallers { get; set; } = true;

    /// <summary>Wrap <c>IJSRuntime</c> instances injected into components to attribute JS interop calls to components.</summary>
    public bool TrackJsInterop { get; set; } = true;

    /// <summary>
    /// Also wrap the <c>IJSObjectReference</c> values returned by tracked runtimes, so calls into imported JS modules
    /// are attributed. BevTools unwraps such references when they are passed back as arguments through a tracked
    /// runtime, but it cannot do so for references nested inside other arguments or passed to an untracked runtime.
    /// Turn this off if a module reference must round-trip through code BevTools does not see.
    /// </summary>
    public bool TrackJsModuleReferences { get; set; } = true;

    /// <summary>Install the browser bridge (JS errors, online/offline, reconnect state, storage inspection, keyboard shortcut).</summary>
    public bool EnableBrowserBridge { get; set; } = true;

    /// <summary>
    /// Count the page's WebSocket traffic in the browser bridge: the Blazor Server circuit and the app's own SignalR hub
    /// connections, by direction and SignalR message kind (render batches, JS interop, pings), with render batch sizes
    /// and the time the browser takes to apply them. Only sizes and message kinds are read, never payloads.
    /// </summary>
    public bool TrackWebSockets { get; set; } = true;

    /// <summary>
    /// Have the browser bridge report the page's responsiveness: every user interaction's time to the next paint, split
    /// into input delay, processing and presentation (Event Timing), and the frames the main thread could not paint on
    /// time, with the scripts that ran in them (long animation frames; long tasks where that is all a browser has). On
    /// WebAssembly, where .NET runs on that thread, each is linked to the UI event, navigation or render batch that ran
    /// inside it. Only timings and element descriptions (tag, id, class) are read.
    /// </summary>
    public bool TrackResponsiveness { get; set; } = true;

    /// <summary>
    /// Register the frameworks own components ActivitySource and meters when the host has not already done so.
    /// Server-side rendering registers them itself; WebAssembly and custom hosts do not, and without them BevTools
    /// cannot attribute a render to the UI event that caused it. Turn off to keep the app free of the meter factory.
    /// </summary>
    public bool UseFrameworkInstrumentation { get; set; } = true;

    /// <summary>
    /// Snapshot component fields and parameters on every render for every component and diff them (state changes and
    /// forced re-renders everywhere). Off by default because it costs reflection and a bounded parameter walk per
    /// render; the inspector always does this for the currently selected component only.
    /// </summary>
    public bool CaptureComponentStateOnRender { get; set; }

    public int MaxInspectionDepth { get; set; } = 8;

    public int MaxCollectionItems { get; set; } = 100;

    public int MaxStringLength { get; set; } = 500;

    /// <summary>
    /// How much of the application's own values BevTools reads (see <see cref="BevValueAccess"/>). Null (default)
    /// means <see cref="BevValueAccess.Full"/> in Development and in a process that serves one user (Blazor WebAssembly,
    /// Blazor Hybrid), where the panel only ever shows people their own data, and <see cref="BevValueAccess.None"/> when
    /// BevTools runs outside Development in a process that serves many users: there the objects a circuit reaches can
    /// hold other users' data (a singleton cache, a shared service), so values stay unread unless the app says otherwise.
    /// </summary>
    public BevValueAccess? ValueAccess { get; set; }

    /// <summary>Member/header/query names matching any of these (case-insensitive substring) are redacted.</summary>
    public List<string> SensitiveNamePatterns { get; } =
    [
        "password", "passwd", "pwd", "secret", "token", "apikey", "api_key", "api-key", "authorization", "cookie",
        "connectionstring", "credential", "ssn", "creditcard", "cardnumber", "cvv", "privatekey", "clientsecret", "bearer",
    ];

    /// <summary>Namespace prefixes whose components are hidden from the component tree.</summary>
    /// <remarks>
    /// BevTools' own components need no entry: they are recognized by assembly and namespace and never shown. A prefix
    /// such as "Bit.BevTools" would also hide every application that lives under it, the demo's included.
    /// </remarks>
    public List<string> HiddenComponentNamespaces { get; } = ["Microsoft.AspNetCore.Components"];

    /// <summary>Additional per-type predicates to exclude components from tracking entirely.</summary>
    public List<Func<Type, bool>> IgnoreComponentPredicates { get; } = [];

    public BevDiagnosticsOptions Diagnostics { get; } = new();

    public BevToolsUiOptions Ui { get; } = new();

    /// <summary>Extensions contributed by libraries or the application.</summary>
    public List<IBevToolsExtension> Extensions { get; } = [];

    /// <summary>The decision <c>AddBitBevToolsServices</c> made, so companion packages do not have to repeat it.</summary>
    internal bool ResolvedEnabled { get; set; }
}
