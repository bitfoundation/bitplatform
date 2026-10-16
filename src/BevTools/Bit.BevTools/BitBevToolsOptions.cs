
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

    public double SlowHttpMs { get; set; } = 2000;

    public double SlowJsInteropMs { get; set; } = 250;

    /// <summary>JS interop calls to the same identifier within one second that count as excessive.</summary>
    public int ExcessiveJsInteropCount { get; set; } = 50;

    /// <summary>Event handler duration considered slow (it blocks the renderer), in milliseconds.</summary>
    public double SlowEventHandlerMs { get; set; } = 100;

    /// <summary>Occurrences of the same error that count as a repeated error.</summary>
    public int RepeatedErrorCount { get; set; } = 3;

    /// <summary>Rule ids that are disabled.</summary>
    public HashSet<string> DisabledRules { get; } = new(StringComparer.OrdinalIgnoreCase);
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

    public int MaxStateChangesPerProvider { get; set; } = 100;

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

    /// <summary>Capture request/response header names and values (sensitive headers are always redacted). Bodies are never captured.</summary>
    public bool CaptureHttpHeaders { get; set; } = true;

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
