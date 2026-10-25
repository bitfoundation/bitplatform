using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>Per-type lifecycle numbers observed through the framework's meters (.NET 10+): SetParametersAsync durations.</summary>
public sealed class BevLifecycleTypeMetrics
{
    /// <summary>The full type name, namespace included, as the framework reports it; the same as <see cref="BevComponentRecord.TypeName"/>.</summary>
    public required string TypeName { get; init; }

    /// <summary>The type name without its namespace, for display.</summary>
    public required string DisplayName { get; init; }

    public int ParameterUpdates;

    public double ParameterUpdateMs;

    public double MaxParameterUpdateMs;

    public int EventHandlers;

    public double EventHandlerMs;
}

/// <summary>
/// Everything BevTools knows about one Blazor session: a circuit on the server, the app in WebAssembly, a request in
/// static rendering. Scoped service; created lazily when the first component is activated.
/// </summary>
public sealed class BevToolsSession : IDisposable
{
    private static long _nextId;
    private long _version;
    private bool _activated;
    private int _capturing = 1;
    private int _skippedWhilePaused;

    public BevToolsSession(IServiceProvider services, BitBevToolsOptions options, BevToolsRegistry registry)
    {
        Id = Interlocked.Increment(ref _nextId);
        Services = services;
        Options = options;
        Registry = registry;
        IsEnabled = registry.IsEnabled;
        Redactor = new BevRedactor(options.SensitiveNamePatterns);
        ValueAccessIsConfigured = options.ValueAccess is not null;
        ValueAccess = options.ValueAccess ?? (IsEnabled && IsInMultiUserHost && !IsDevelopmentHost(services) ? BevValueAccess.None : BevValueAccess.Full);
        Inspector = new BevObjectInspector(options, Redactor, registry.Formatters, ValueAccess);
        Overhead = new BevOverheadMeter();
        Timeline = new Timeline(this, options.MaxEvents);
        Components = new ComponentRegistry(this, options);
        Leaks = new BevLeakTracker(this);
        Forms = new BevFormTracker(this);
        Errors = new ErrorCenter(this, options);
        Http = new HttpStore(this, options);
        Interop = new JsInteropStore(this, options);
        State = new StateStore(this, options);
        Diagnostics = new DiagnosticsEngine(this, options, registry);
        Commands = new CommandRegistry();
        RenderTracker = new RenderTracker(this, options);
        Ui = new BevToolsUiState
        {
            IsOpen = options.Ui.OpenByDefault,
            Dock = options.Ui.Dock,
            Theme = options.Ui.Theme,
            LiveUpdates = options.Ui.LiveUpdates,
        };
        Browser = new BevBrowserState();
        StartedAt = DateTimeOffset.UtcNow;
        StartedTicks = System.Diagnostics.Stopwatch.GetTimestamp();

        if (IsEnabled)
        {
            SessionResolver.Register(this);
        }
    }

    public long Id { get; }

    public DateTimeOffset StartedAt { get; }

    /// <summary>The high-resolution timestamp taken with <see cref="StartedAt"/>: the zero of a performance trace.</summary>
    internal long StartedTicks { get; }

    public bool IsEnabled { get; }

    public bool IsDisposed { get; private set; }

    public bool IsAttachedAtRuntime => Attachment is not null;

    internal BevRuntimeAttachment? Attachment { get; init; }

    public BitBevToolsOptions Options { get; }

    public BevToolsRegistry Registry { get; }

    internal IServiceProvider Services { get; }

    public BevRedactor Redactor { get; }

    /// <summary>
    /// How much of the application's values this session reads: <see cref="BitBevToolsOptions.ValueAccess"/> when set,
    /// otherwise Full, except outside Development in a process that serves many users, where it is None.
    /// </summary>
    public BevValueAccess ValueAccess { get; }

    /// <summary>
    /// True when <see cref="ValueAccess"/> comes from <see cref="BitBevToolsOptions.ValueAccess"/> rather than the
    /// default: a None the app chose must not be explained as the multi-user default it did not hit.
    /// </summary>
    internal bool ValueAccessIsConfigured { get; }

    public BevObjectInspector Inspector { get; }

    public BevOverheadMeter Overhead { get; }

    internal Timeline Timeline { get; }

    internal ComponentRegistry Components { get; }

    /// <summary>Disposed components, held weakly, and which of them garbage collection did not reclaim.</summary>
    internal BevLeakTracker Leaks { get; }

    /// <summary>The session's forms (EditContexts): fields, validation outcomes, validators and message displays.</summary>
    internal BevFormTracker Forms { get; }

    /// <summary>
    /// Whether a developer may force a full garbage collection from BevTools: everywhere but a process that serves many
    /// users outside Development, where it would pause every circuit for one person's check.
    /// </summary>
    internal bool AllowsForcedCollection => !IsInMultiUserHost || IsDevelopmentHost(Services);

    internal ErrorCenter Errors { get; }

    internal HttpStore Http { get; }

    internal JsInteropStore Interop { get; }

    internal StateStore State { get; }

    internal DiagnosticsEngine Diagnostics { get; }

    internal CommandRegistry Commands { get; }

    internal RenderTracker RenderTracker { get; }

    public BevToolsUiState Ui { get; }

    public BevBrowserState Browser { get; }

    /// <summary>Public timeline API for extensions.</summary>
    public IBevToolsTimeline TimelineApi => Timeline;

    public IBevStateProviderRegistry StateProviders => State;

    /// <summary>The renderer dispatcher owning this session; used to attribute ambient activity (HTTP, logs, metrics) to the session.</summary>
    public Dispatcher? Dispatcher { get; internal set; }

    /// <summary>"Server", "WebAssembly", "Static" or "WebView" as reported by RendererInfo; null until a component reports it.</summary>
    public string? Platform { get; internal set; }

    // 0: not checked yet, 1: no, 2: yes. An int so concurrent first reads cannot see a torn value.
    private int _inServerProcess;

    /// <summary>
    /// True when the session lives in a process that serves many users - an ASP.NET Core server, home of Blazor Server
    /// circuits, static SSR requests and prerendering - rather than WebAssembly, a WebView or a test host, where the only
    /// session is the only possible owner of activity that cannot be attributed otherwise. Decided by the host, not by
    /// <see cref="Platform"/> alone: a circuit's platform is unknown until something reports it (always on .NET 8, where
    /// RendererInfo does not exist), and a request scope that renders nothing never reports one.
    /// </summary>
    internal bool IsInMultiUserHost
    {
        get
        {
            if (Platform is "Server" or "Static")
            {
                return true;
            }

            var known = Volatile.Read(ref _inServerProcess);
            if (known == 0)
            {
                known = HostsAServer(Services) ? 2 : 1;
                Volatile.Write(ref _inServerProcess, known);
            }

            return known == 2;
        }
    }

    private static readonly Type? ServerType = FindServerType();

    private static Type? FindServerType()
    {
        if (OperatingSystem.IsBrowser())
        {
            return null;
        }

        try
        {
            // ASP.NET Core registers its IServer (Kestrel, IIS, TestServer) in every host that accepts requests. Looked up
            // by name: this package does not reference the ASP.NET Core framework, and WebAssembly does not ship it. The
            // loader goes by the same registration, so both agree on what a server process is.
            return Type.GetType(BevToolsAttachPolicy.ServerTypeName + ", Microsoft.AspNetCore.Hosting.Server.Abstractions", throwOnError: false);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Whether the host reports Development. Only asked of a server process, which always registers <c>IHostEnvironment</c>.</summary>
    private static bool IsDevelopmentHost(IServiceProvider services)
    {
        try
        {
            return services.GetService<Microsoft.Extensions.Hosting.IHostEnvironment>() is { } environment
                && Microsoft.Extensions.Hosting.HostEnvironmentEnvExtensions.IsDevelopment(environment);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>What an attach without Bit.BevTools.Loader goes by; with it, <see cref="BevToolsLoaderContext.IsMultiUserHost"/> reads the same registration from the service collection.</summary>
    internal static bool HostsAServer(IServiceProvider services)
    {
        try
        {
            return ServerType is not null && services.GetService<IServiceProviderIsService>()?.IsService(ServerType) == true;
        }
        catch
        {
            // a disposed container; nothing it hosts can be attributed to this session any more.
            return true;
        }
    }

    public bool IsInteractive { get; internal set; }

    /// <summary>What prerendering did for this page, handed over through persistent component state; null when the page was not prerendered, or the summary did not arrive.</summary>
    public BevPrerenderSummary? Prerender { get; internal set; }

    public string? RenderModeName { get; internal set; }

    /// <summary>Monotonic version incremented on any change; the UI polls it to decide whether to re-render.</summary>
    public long Version => Volatile.Read(ref _version);

    /// <summary>
    /// False while capture is paused: no renders, timeline events, HTTP requests, JS interop calls or state changes
    /// are recorded, so the buffers stay exactly as they were when the developer pressed pause. Errors are still
    /// collected (losing one because the timeline was frozen would be worse than an inconsistent row), and the
    /// instrumentation itself stays installed so resuming is instant.
    /// </summary>
    public bool IsCapturing => Volatile.Read(ref _capturing) == 1;

    public DateTimeOffset? PausedAt { get; private set; }

    /// <summary>Freezes every buffer. Idempotent.</summary>
    public void PauseCapture()
    {
        if (!IsEnabled || Interlocked.Exchange(ref _capturing, 0) == 0)
        {
            return;
        }

        // Recorded through the buffer directly: Record() refuses new events once the flag is down.
        PausedAt = DateTimeOffset.UtcNow;
        Timeline.RecordUnconditionally(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Custom,
            Category = "bevtools",
            Title = "Capture paused",
            Detail = "Nothing below this line was recorded until capture resumed; errors were still collected.",
            Severity = BevToolsSeverity.Warning,
        });
        Touch();
    }

    /// <summary>Resumes capture. Idempotent.</summary>
    public void ResumeCapture()
    {
        if (!IsEnabled || Interlocked.Exchange(ref _capturing, 1) == 1)
        {
            return;
        }

        var pausedFor = PausedAt is { } at ? DateTimeOffset.UtcNow - at : TimeSpan.Zero;
        PausedAt = null;
        var skipped = Interlocked.Exchange(ref _skippedWhilePaused, 0);
        Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Custom,
            Category = "bevtools",
            Title = "Capture resumed",
            Detail = $"paused for {pausedFor.TotalSeconds:0.#} s" + (skipped > 0 ? $"; {skipped} component render{(skipped == 1 ? "" : "s")} happened meanwhile and were not recorded" : ""),
        });
        Touch();
    }

    /// <summary>Set once the renderer synchronization context has been mapped to this session.</summary>
    internal bool SynchronizationContextNoted { get; set; }

    internal long? LastUiEventId { get; set; }

    internal long? LastNavigationEventId { get; set; }

    /// <summary>
    /// The navigation event the framework's Navigate activity recorded that a LocationChanged has already been matched
    /// to: each activity stands for one navigation, so it can absorb only one LocationChanged.
    /// </summary>
    internal long? MatchedNavigationEventId { get; set; }

    /// <summary>The last navigation event recorded from LocationChanged itself rather than from the activity.</summary>
    internal long? LastLocationChangedEventId { get; set; }

    /// <summary>The UI event (or navigation) currently executing on this session, resolved from Activity context.</summary>
    internal long? CurrentTriggerEventId => ActivityObserver.CurrentTriggerEvent(this)?.Id;

    private FailedTrigger? _failedTrigger;

    /// <summary>
    /// The framework stops a UI event's activity, marked failed with the exception type, before it logs the handler's
    /// exception, so by the time that log entry arrives the event is no longer on the Activity stack. The failure is
    /// kept briefly so the logged exception can still point at the event that threw.
    /// </summary>
    internal void NoteFailedTrigger(long eventId, string errorType) => Volatile.Write(ref _failedTrigger, new FailedTrigger(eventId, errorType, DateTimeOffset.UtcNow));

    /// <summary>Claims the recently failed UI event if it failed with exactly this exception type; each failure is claimed once.</summary>
    internal long? TakeFailedTrigger(string exceptionType)
    {
        var failed = Volatile.Read(ref _failedTrigger);
        if (failed is null || !string.Equals(failed.ErrorType, exceptionType, StringComparison.Ordinal) || DateTimeOffset.UtcNow - failed.At > TimeSpan.FromSeconds(5))
        {
            return null;
        }

        return ReferenceEquals(Interlocked.CompareExchange(ref _failedTrigger, null, failed), failed) ? failed.EventId : null;
    }

    private sealed record FailedTrigger(long EventId, string ErrorType, DateTimeOffset At);

    internal ConcurrentDictionary<string, BevLifecycleTypeMetrics> TypeMetrics { get; } = new(StringComparer.Ordinal);

    /// <summary>Raised when something needs the symbols of an assembly that has none on disk (WebAssembly); the panel downloads them.</summary>
    internal event Action<System.Reflection.Assembly>? SymbolsWanted;

    internal void WantSymbols(System.Reflection.Assembly assembly) => SymbolsWanted?.Invoke(assembly);

    private WeakReference<IComponent>? _panel;

    /// <summary>
    /// The interactive BevTools panel of this session. Its element is the one place in the page known to be rendered by
    /// this runtime, so the page hub finds this runtime's root component from it (see <see cref="BevDomMap"/>). Held
    /// weakly, like every component BevTools knows.
    /// </summary>
    internal IComponent? PanelComponent
    {
        get => _panel is not null && _panel.TryGetTarget(out var panel) ? panel : null;
        set => _panel = value is null ? null : new WeakReference<IComponent>(value);
    }

    private long? _externallyInspected;

    /// <summary>
    /// What selecting a component in the in-app inspector does, for a frontend outside the app (the companion window):
    /// the component's state is captured on its renders from now on, so its next render says what changed, and the one
    /// inspected before stops being captured unless the in-app panel has it selected.
    /// </summary>
    internal void InspectExternally(BevComponentRecord record)
    {
        if (!Options.CaptureComponentStateOnRender && _externallyInspected is { } previousId && previousId != record.InstanceId
            && previousId != Ui.SelectedComponentId && Components.Get(previousId) is { } previous)
        {
            previous.CaptureState = false;
        }

        _externallyInspected = record.InstanceId;
        record.CaptureState = true;
    }

    internal void Touch() => Interlocked.Increment(ref _version);

    /// <summary>Called by the render tracker for each application render that happened while capture was paused.</summary>
    internal void NoteSkippedRender(BevComponentRecord record)
    {
        record.NoteSkippedRender();
        Interlocked.Increment(ref _skippedWhilePaused);
    }

    /// <summary>Wires DI-registered state providers, extension providers and commands. Called by the host component (not the constructor) to avoid DI cycles.</summary>
    internal void EnsureActivated()
    {
        if (_activated || !IsEnabled)
        {
            return;
        }

        _activated = true;
        try
        {
            foreach (var provider in Services.GetServices<IBevStateProvider>())
            {
                State.Attach(provider, "DI");
            }

            if (BevAuthenticationStateProvider.TryCreate(Services) is { } authentication)
            {
                _authentication = authentication;
                State.Attach(authentication, "built-in");
            }

            foreach (var factory in Registry.StateProviderFactories)
            {
                try
                {
                    State.Attach(factory(Services), "extension");
                }
                catch (Exception ex)
                {
                    Errors.Record(ex, "bevtools", "A state provider factory threw.");
                }
            }

            Commands.AddRange(BuiltInCommands.Create());
            Commands.AddRange(Registry.Commands);
            Commands.AddRange(Services.GetServices<IBevToolsCommand>());
        }
        catch (Exception ex)
        {
            Errors.Record(ex, "bevtools", "BevTools activation failed.");
        }

        Touch();
    }

    /// <summary>Whether the framework diagnostics services this session depends on are present in its container.</summary>
    public (bool ActivitySource, bool Metrics) FrameworkInstrumentation => ActivityObserver.ResolveFrameworkServices(Services);

    public IReadOnlyList<BevToolsCapability> Capabilities => CapabilityReport.Build(this);

    /// <summary>
    /// Attributes ambient activity on the current async flow (HTTP requests, framework logs, metrics) to this session
    /// until the scope is disposed. Use it in background work (Task.Run, hosted services) that runs outside the
    /// renderer's dispatcher, where the session cannot be inferred.
    /// </summary>
    public IDisposable UseAmbient() => SessionResolver.PushAmbient(this);

    private readonly object _navigationCausesLock = new();
    private readonly Queue<long> _navigationCauseOrder = new();
    private readonly Dictionary<long, string> _navigationCauses = [];

    /// <summary>
    /// Records how a navigation event came about - "intercepted link" (a person clicked) or "programmatic" (code called
    /// NavigateTo) - whichever recorded the event: on .NET 10 the router's activity records it before LocationChanged
    /// says how it happened. Bounded to the latest 200.
    /// </summary>
    internal void NoteNavigationCause(long eventId, string cause)
    {
        lock (_navigationCausesLock)
        {
            if (_navigationCauses.TryAdd(eventId, cause))
            {
                _navigationCauseOrder.Enqueue(eventId);
                while (_navigationCauseOrder.Count > 200)
                {
                    _navigationCauses.Remove(_navigationCauseOrder.Dequeue());
                }
            }
        }
    }

    /// <summary>How a navigation event came about, when LocationChanged said so.</summary>
    internal string? NavigationCause(long eventId)
    {
        lock (_navigationCausesLock)
        {
            return _navigationCauses.GetValueOrDefault(eventId);
        }
    }

    public void ClearAll()
    {
        Timeline.Clear();
        Errors.Clear();
        Http.Clear();
        Interop.Clear();
        State.ClearHistory();
        Components.ClearDisposed();
        Leaks.Clear();
        Forms.Clear();
        Browser.Accessibility = null;
        Browser.AccessibilityCheckedAt = null;
        Browser.AccessibilityError = null;
        Diagnostics.Clear();
        TypeMetrics.Clear();
        Touch();
    }

    public void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        SessionResolver.Unregister(this);
        State.Dispose();
        _authentication?.Dispose();
    }

    private BevAuthenticationStateProvider? _authentication;
}
