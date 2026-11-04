using Microsoft.Extensions.Logging;

namespace Bit.Belemetry;

/// <summary>
/// Production diagnostics: a bounded, in-memory timeline of what the app did (navigation, component events,
/// exceptions, failed requests, browser errors, circuit transitions), exception grouping, and support bundles the user
/// or a support agent generates, previews and exports on purpose. Off by default.
/// </summary>
/// <remarks>
/// <para>
/// The timeline holds what the telemetry pipeline accepted - after consent, the privacy rules and the app's processors -
/// so a feature the user did not grant is not in it either, and a revoke purges it. Nothing in it leaves the process
/// until the app exports a capture, which needs an explicit <see cref="BelemetryDiagnosticCapture.Confirm"/>.
/// </para>
/// <para>
/// It works without any exporter. On Blazor Server each circuit gets a timeline of its own, holding only what can be
/// attributed to that circuit; see the README's "Production diagnostics" section.
/// </para>
/// </remarks>
public sealed class BelemetryDiagnosticsOptions
{
    /// <summary>Whether the diagnostic timeline records anything. Defaults to <see langword="false"/>.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The most activity events (navigation, component events, requests, logs) the timeline keeps; the oldest are
    /// evicted first. Defaults to 300. On Blazor Server this is per circuit.
    /// </summary>
    public int MaxEvents { get; set; } = 300;

    /// <summary>
    /// The most failure events (exceptions, failed requests, circuit failures) the timeline keeps, apart from the
    /// activity events so a chatty page cannot evict the error being diagnosed. Defaults to 50.
    /// </summary>
    public int MaxFailureEvents { get; set; } = 50;

    /// <summary>
    /// The approximate memory the timeline may use, in bytes (estimated from the strings it holds); past it the oldest
    /// activity events go first, then the oldest failures. Defaults to 1 MiB.
    /// </summary>
    public int MaxMemoryBytes { get; set; } = 1024 * 1024;

    /// <summary>The kinds of events recorded. Defaults to <see cref="BelemetryDiagnosticCategories.All"/>.</summary>
    public BelemetryDiagnosticCategories Categories { get; set; } = BelemetryDiagnosticCategories.All;

    /// <summary>The lowest <c>ILogger</c> level recorded on the timeline. Defaults to <see cref="LogLevel.Warning"/>.</summary>
    public LogLevel MinimumLogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// Whether successful activity (navigations, handled UI events, successful requests) is kept as context for what
    /// preceded a failure. Without it only failures and warnings are recorded. Defaults to <see langword="true"/>.
    /// </summary>
    public bool IncludeSuccessfulActivity { get; set; } = true;

    /// <summary>
    /// Whether a log event keeps its rendered message (after the privacy rules) rather than its message template.
    /// Defaults to <see langword="false"/>: <c>"Order {OrderId} failed"</c>, not the value.
    /// </summary>
    public bool IncludeLogMessages { get; set; }

    /// <summary>
    /// Whether an exception keeps its message (after the privacy rules). Defaults to <see langword="true"/>: it is
    /// usually what tells one failure from another. Turn it off when the app's exception messages carry user data the
    /// privacy patterns cannot recognize; the message template is kept for grouping either way.
    /// </summary>
    public bool IncludeExceptionMessages { get; set; } = true;

    /// <summary>
    /// Attribute keys an event may keep besides the ones Belemetry keeps for its category (HTTP method, status code,
    /// component type, ...). Every other attribute is left out: diagnostics keep metadata, not values. Empty by default.
    /// </summary>
    public List<string> AllowedAttributes { get; } = [];

    /// <summary>
    /// Query parameter names a URL in the timeline keeps (with their values, after the privacy rules). Every other
    /// parameter, and the fragment, is left out. Empty by default.
    /// </summary>
    public List<string> AllowedQueryParameters { get; } = [];

    /// <summary>
    /// Keys (case-insensitive, whole key or last dot segment; <c>prefix*</c>) whose values are replaced with
    /// <c>REDACTED</c> in snapshots and event attributes, on top of <see cref="BelemetryPrivacyOptions.RedactedAttributes"/>.
    /// The defaults cover Blazor Server's circuit and connection ids, which are bearer secrets.
    /// </summary>
    public List<string> RedactedKeys { get; } = ["circuitid", "circuitidsecret", "connectionid", "circuit.id", "aspnetcore.components.circuit.id"];

    /// <summary>
    /// What creates a capture automatically, waiting for the user or a support agent to review it
    /// (<see cref="IBelemetryDiagnostics.PendingCaptures"/>). Defaults to <see cref="BelemetryDiagnosticTriggers.None"/>.
    /// </summary>
    public BelemetryDiagnosticTriggers AutoCaptureTriggers { get; set; } = BelemetryDiagnosticTriggers.None;

    /// <summary>How long an automatic capture waits after its trigger, so what followed it is included. Defaults to 2 seconds.</summary>
    public TimeSpan PostTriggerWindow { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// How long a failure with the same fingerprint does not trigger another automatic capture. Defaults to 10 minutes.
    /// </summary>
    public TimeSpan AutoCaptureCooldown { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>The most automatic captures kept for review; the oldest is discarded first. Defaults to 3.</summary>
    public int MaxPendingCaptures { get; set; } = 3;

    /// <summary>How long an automatic capture is kept for review. Defaults to 30 minutes.</summary>
    public TimeSpan CaptureRetention { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// The largest support bundle, in bytes of uncompressed JSON; past it the oldest timeline events are left out
    /// (and the manifest says how many). Defaults to 4 MiB.
    /// </summary>
    public int MaxBundleBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>The most exception groups remembered; the least recently seen group is forgotten first. Defaults to 50.</summary>
    public int MaxExceptionGroups { get; set; } = 50;

    /// <summary>
    /// Whether one failure in two releases is one exception group. Defaults to <see langword="true"/>; set it to
    /// <see langword="false"/> to keep each release's occurrences apart (the release then is part of the fingerprint).
    /// </summary>
    public bool GroupAcrossReleases { get; set; } = true;

    /// <summary>
    /// Whether per-component render statistics are collected from the framework's
    /// <c>Microsoft.AspNetCore.Components.Lifecycle</c> meter (a dictionary update per render). Defaults to
    /// <see langword="false"/>.
    /// </summary>
    public bool ComponentRenderStatistics { get; set; }

    /// <summary>
    /// Whether Belemetry makes the framework create its component activities (<c>Navigate</c>, <c>HandleEvent</c>,
    /// <c>StartCircuit</c>) when no exporter of Belemetry traces them - they are what names the component and the event
    /// handler a failure happened in. An activity that exists becomes the parent of the spans started under it: an
    /// unsampled one makes a parent-based sampler (OpenTelemetry's default) drop them, so on a host whose own tracer does
    /// not listen to <c>Microsoft.AspNetCore.Components</c>, creating them changes what that tracer exports. Null (the
    /// default) creates them on WebAssembly and Hybrid, where Belemetry is the app's tracer, and not on a server. When
    /// Belemetry's own traces cover a source, its export decides, and an activity name in
    /// <see cref="BelemetryTracingOptions.IgnoredActivityNames"/> is never created. Activities something else creates
    /// are observed either way.
    /// </summary>
    public bool? CreateFrameworkActivities { get; set; }

    /// <summary>
    /// The most events the browser script keeps in its own timeline (page views, fetch/XHR, JS errors, interactions,
    /// reconnection). Defaults to 200.
    /// </summary>
    public int BrowserMaxEvents { get; set; } = 200;

    /// <summary>
    /// Whether the browser timeline survives a reload of the tab (in <c>sessionStorage</c>, only while
    /// <see cref="BelemetryFeatures.Persistence"/> is granted), so a capture after a crash and reload still shows what
    /// led to it. Defaults to <see langword="false"/>.
    /// </summary>
    public bool PersistBrowserTimeline { get; set; }

    /// <summary>The release markers attached to every capture.</summary>
    public BelemetryReleaseOptions Release { get; } = new();

    /// <summary>Limits applied to the application's snapshot providers.</summary>
    public BelemetrySnapshotOptions Snapshots { get; } = new();
}
