
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.BevTools;

/// <summary>Configuration for <c>AddBitBevToolsServices</c>.</summary>
public sealed class BevToolsOptions
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
    /// A response's body is read before the response is handed on - a handler cannot tell how its caller will read it -
    /// so a caller that asked for the headers only (<c>HttpCompletionOption.ResponseHeadersRead</c>) gets them once that
    /// body, of a known length within the limit, has arrived.
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
    /// How much of the application's own values BevTools reads (see <see cref="BevToolsValueAccess"/>). Null (default)
    /// means <see cref="BevToolsValueAccess.Full"/> in Development and in a process that serves one user (Blazor WebAssembly,
    /// Blazor Hybrid), where the panel only ever shows people their own data, and <see cref="BevToolsValueAccess.None"/> when
    /// BevTools runs outside Development in a process that serves many users: there the objects a circuit reaches can
    /// hold other users' data (a singleton cache, a shared service), so values stay unread unless the app says otherwise.
    /// </summary>
    public BevToolsValueAccess? ValueAccess { get; set; }

    /// <summary>
    /// Member, header, query, storage and key names matching any of these are redacted: case-insensitively, anywhere in
    /// the name for a pattern of four characters or more ("token" in <c>AccessToken</c> and <c>accesstoken</c>), and for a
    /// shorter one only where it starts or ends a word of the name - "otp" in <c>Otp</c>, <c>UserOtp</c>, <c>otp_code</c> and
    /// <c>?otp=</c>, but not in <c>NotPublished</c> or <c>Footprint</c>; "ssn" not in <c>ClassName</c>. Words end at
    /// characters other than letters and digits, and where a lower case letter or digit meets an upper case one.
    /// </summary>
    public List<string> SensitiveNamePatterns { get; } =
    [
        "password", "passwd", "pwd", "secret", "token", "apikey", "api_key", "api-key", "authorization", "cookie",
        "connectionstring", "credential", "ssn", "creditcard", "cardnumber", "cvv", "privatekey", "clientsecret", "bearer",
        "otp", "totp",
    ];

    /// <summary>
    /// Service registrations an integration needs next to its extension (<c>AddBelemetry</c>'s Belemetry hooks), which
    /// <c>AddBitBevToolsServices</c> applies once its configure callback has run. A session attached at runtime has no
    /// service collection left to apply them to, which the integration documents.
    /// </summary>
    internal List<Action<IServiceCollection>> ServiceRegistrations { get; } = [];

    /// <summary>Namespace prefixes whose components are hidden from the component tree.</summary>
    /// <remarks>
    /// BevTools' own components need no entry: they are recognized by assembly and namespace and never shown. A prefix
    /// such as "Bit.BevTools" would also hide every application that lives under it, the demo's included.
    /// </remarks>
    public List<string> HiddenComponentNamespaces { get; } = ["Microsoft.AspNetCore.Components"];

    /// <summary>Additional per-type predicates to exclude components from tracking entirely.</summary>
    public List<Func<Type, bool>> IgnoreComponentPredicates { get; } = [];

    public BevToolsDiagnosticsOptions Diagnostics { get; } = new();

    public BevToolsUiOptions Ui { get; } = new();

    /// <summary>Extensions contributed by libraries or the application.</summary>
    public List<IBevToolsExtension> Extensions { get; } = [];

    /// <summary>
    /// Adds a panel of the application's own, as a tab after the built-in ones, without writing an
    /// <see cref="IBevToolsExtension"/>: <c>o.AddPanel&lt;MyApiPanel&gt;("my-api", "My API")</c>. The component receives no
    /// parameters; it can inject the application's services (to call its own APIs, say) and BevTools' ones, and when it
    /// inherits <see cref="BevToolsTab"/> it reads the session and re-reads whenever the panel refreshes. Like every panel it
    /// is BevTools' own UI, so its renders and clicks are not recorded as the application's. Give it a component of its
    /// own: an instance the application renders on its pages stays the application's, and since the framework's events
    /// name only a component type, a type rendered in both places has its clicks in the panel recorded as the
    /// application's too. Adding a panel with an id that was already added this way replaces it; when an
    /// <see cref="IBevToolsExtension"/> contributes the same id, the one later in <see cref="Extensions"/> wins and About
    /// reports the clash.
    /// </summary>
    /// <param name="id">The panel's id, for <c>ShowPanel</c>. A built-in panel's id ("logs", "about", ...) throws an <see cref="ArgumentException"/>.</param>
    /// <param name="title">The tab's text.</param>
    /// <param name="order">Where the tab goes among the extension panels: lower first.</param>
    /// <param name="requiredPlatform">When set, the panel shows only where the renderer has this name (e.g. "Server").</param>
    public BevToolsOptions AddPanel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent>(string id, string title, int order = 100, string? requiredPlatform = null)
        where TComponent : IComponent
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (BevToolsPanel.IsBuiltInPanelId(id))
        {
            // Here the mistake fails where it is made; the registry would only list it in About.
            throw new ArgumentException($"Panel id '{id}' is a built-in panel's; choose another.", nameof(id));
        }

        // A configure callback can run more than once on the same options (each AddBitBevToolsServices call of a host
        // configures the options already registered), so adding the same id again replaces it rather than listing the
        // extension twice in About.
        Extensions.RemoveAll(e => e is BevToolsPanelExtension existing && existing.Panel.Id == id);
        Extensions.Add(new BevToolsPanelExtension(new BevToolsPanelDescriptor(id, title, typeof(TComponent), order) { RequiredPlatform = requiredPlatform }));
        return this;
    }

    /// <summary>The decision <c>AddBitBevToolsServices</c> made, so companion packages do not have to repeat it.</summary>
    internal bool ResolvedEnabled { get; set; }
}
