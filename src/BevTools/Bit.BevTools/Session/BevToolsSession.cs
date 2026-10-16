using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools;

/// <summary>Per-type lifecycle numbers observed through the framework's meters (.NET 10+): SetParametersAsync durations.</summary>
public sealed class BevLifecycleTypeMetrics
{
    public required string TypeName { get; init; }

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
        Inspector = new BevObjectInspector(options, Redactor, registry.Formatters);
        Overhead = new BevOverheadMeter();
        Timeline = new Timeline(this, options.MaxEvents);
        Components = new ComponentRegistry(this, options);
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

        if (IsEnabled)
        {
            SessionResolver.Register(this);
        }
    }

    public long Id { get; }

    public DateTimeOffset StartedAt { get; }

    public bool IsEnabled { get; }

    public bool IsDisposed { get; private set; }

    public BitBevToolsOptions Options { get; }

    public BevToolsRegistry Registry { get; }

    internal IServiceProvider Services { get; }

    public BevRedactor Redactor { get; }

    public BevObjectInspector Inspector { get; }

    public BevOverheadMeter Overhead { get; }

    internal Timeline Timeline { get; }

    internal ComponentRegistry Components { get; }

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

    public bool IsInteractive { get; internal set; }

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

    public void ClearAll()
    {
        Timeline.Clear();
        Errors.Clear();
        Http.Clear();
        Interop.Clear();
        State.ClearHistory();
        Components.ClearDisposed();
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
    }
}
