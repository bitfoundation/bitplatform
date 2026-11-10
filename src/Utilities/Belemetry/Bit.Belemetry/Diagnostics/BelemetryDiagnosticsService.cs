using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry;

/// <summary>
/// The scoped <see cref="IBelemetryDiagnostics"/>: on WebAssembly and Hybrid every one shares the process's timeline; on a
/// server every service of a circuit - or a prerendering request - shares that circuit's, found by the renderer's
/// synchronization context.
/// </summary>
internal sealed class BelemetryDiagnosticsService : IBelemetryDiagnostics, IAsyncDisposable
{
    private readonly BelemetryDiagnosticHub _hub;
    private readonly IServiceProvider _services;
    private BelemetryDiagnosticRecorder? _recorder;
    private SynchronizationContext? _context;
    private int _disposed;

    public BelemetryDiagnosticsService(BelemetryPipeline pipeline, BelemetryJsBridge bridge, IServiceProvider services)
    {
        _hub = pipeline.Diagnostics;
        _services = services;
        Bridge = bridge;

        if (_hub.Enabled is false) return;

        // On a server, a timeline of its own until it finds its circuit's (EnsureRegistered): what it records meanwhile is
        // moved there.
        _recorder = _hub.Global ?? new BelemetryDiagnosticRecorder(_hub.Options);
        _recorder.AddOwner(this);
        _recorder.CaptureAdded += OnCaptureAdded;
        bridge.DiagnosticTriggered += OnBrowserTrigger;
        EnsureRegistered();
    }

    internal BelemetryJsBridge Bridge { get; }

    internal bool IsDisposed => _disposed == 1;

    public bool IsEnabled => _recorder is not null;

    public IReadOnlyList<BelemetryDiagnosticCapture> PendingCaptures => _recorder?.Pending() ?? [];

    public event Action<BelemetryDiagnosticCapture>? CaptureAvailable;

    /// <summary>
    /// On a server, registers this circuit's timeline under the renderer's synchronization context - the one this circuit's
    /// code runs on. Called again from every entry point (and by <see cref="BelemetryInitializer"/>), as the service may
    /// first be resolved somewhere else.
    /// </summary>
    internal void EnsureRegistered()
    {
        if (_recorder is null || _hub.Settings.IsServer is false || _context is not null || IsDisposed) return;

        var current = SynchronizationContext.Current;
        if (current is null) return;

        var own = _recorder;
        var shared = _hub.Acquire(current, this);
        _context = current;
        if (ReferenceEquals(own, shared)) return;

        own.CaptureAdded -= OnCaptureAdded;
        own.RemoveOwner(this, closeWhenUnowned: true);
        shared.Import(own);
        shared.CaptureAdded += OnCaptureAdded;
        _recorder = shared;
    }

    public void Record(string name, IReadOnlyDictionary<string, object?>? attributes = null, BelemetrySeverity severity = BelemetrySeverity.Info, BelemetryDiagnosticCategories category = BelemetryDiagnosticCategories.Custom)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (_recorder is null) return;

        try
        {
            EnsureRegistered();
            _hub.Record(_recorder, _hub.Factory.Custom(name, attributes, severity, category, _recorder));
        }
        catch (Exception ex)
        {
            LogFailure(_services, ex, "Recording a diagnostic event failed; it is dropped.");
        }
    }

    public async ValueTask RecordSnapshotAsync(string label, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(label);
        if (_recorder is null) return;

        try
        {
            EnsureRegistered();
            var counters = new BelemetrySanitizeCounters();
            var safeLabel = _hub.Sanitizer.Text(label, 128, ref counters) ?? "snapshot";
            using (BelemetryDiagnosticHub.Suppress())
            {
                var snapshots = await BelemetryDiagnosticCaptureBuilder.TakeSnapshotsAsync(_hub, SnapshotProviders(), names: null,
                    new BelemetrySnapshotContext { Reason = safeLabel, ForCapture = false }, safeLabel, cancellationToken);
                foreach (var snapshot in snapshots) _recorder.AddSnapshot(snapshot);
            }
            _hub.Record(_recorder, _hub.Factory.Custom("snapshot." + safeLabel, null, BelemetrySeverity.Info, BelemetryDiagnosticCategories.Custom, _recorder));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogFailure(_services, ex, "Recording a diagnostic snapshot failed.");
        }
    }

    public async ValueTask<BelemetryDiagnosticCapture> CaptureAsync(BelemetryDiagnosticCaptureOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new BelemetryDiagnosticCaptureOptions();
        var trigger = new BelemetryCaptureTrigger(BelemetryCaptureTriggerKind.Manual, string.IsNullOrWhiteSpace(options.Reason) ? "manual" : options.Reason);
        if (_recorder is null) return BelemetryDiagnosticCapture.Failed(trigger, "Diagnostics are not enabled (BelemetryOptions.Diagnostics.Enabled).");

        EnsureRegistered();
        try
        {
            return await new BelemetryDiagnosticCaptureBuilder(_hub, _recorder, this).BuildAsync(options, trigger, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogFailure(_services, ex, "A diagnostic capture failed.");
            return BelemetryDiagnosticCapture.Failed(trigger, $"The capture failed ({ex.GetType().Name}).");
        }
    }

    /// <summary>An automatic capture, run on this session's synchronization context (snapshot providers may read component state).</summary>
    internal Task<BelemetryDiagnosticCapture> CaptureForTriggerAsync(BelemetryDiagnosticCaptureOptions options, BelemetryCaptureTrigger trigger)
    {
        var context = _context;
        if (context is null || ReferenceEquals(SynchronizationContext.Current, context))
        {
            return new BelemetryDiagnosticCaptureBuilder(_hub, _recorder!, this).BuildAsync(options, trigger, CancellationToken.None);
        }

        var completion = new TaskCompletionSource<BelemetryDiagnosticCapture>(TaskCreationOptions.RunContinuationsAsynchronously);
        context.Post(async _ =>
        {
            try
            {
                using (BelemetryDiagnosticHub.Suppress())
                {
                    completion.TrySetResult(await new BelemetryDiagnosticCaptureBuilder(_hub, _recorder!, this).BuildAsync(options, trigger, CancellationToken.None));
                }
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }, null);
        return completion.Task;
    }

    public IReadOnlyList<BelemetryDiagnosticEvent> GetEvents()
    {
        if (_recorder is null) return [];
        return _recorder.Events().Where(e => _hub.MayCapture(e.Feature)).ToList();
    }

    public IReadOnlyList<BelemetryExceptionGroup> GetExceptionGroups() =>
        _recorder is null || _hub.MayCapture(BelemetryFeatures.Errors) is false ? [] : _recorder.Groups();

    public bool Dismiss(BelemetryDiagnosticCapture capture)
    {
        ArgumentNullException.ThrowIfNull(capture);
        return _recorder?.RemovePending(capture) ?? false;
    }

    public async ValueTask<bool> DownloadAsync(BelemetryDiagnosticCapture capture, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(capture);
        var bytes = await capture.ExportAsync(cancellationToken);
        return await Bridge.DownloadAsync(capture.SuggestedFileName, bytes, cancellationToken);
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        _recorder?.Clear();

        // The page's timeline too, and its copy kept for the next load: forgetting is not done until both are.
        if (_recorder is not null) await Bridge.ClearDiagnosticsAsync();
    }

    internal IEnumerable<IBelemetrySnapshotProvider> SnapshotProviders() => _services.GetServices<IBelemetrySnapshotProvider>();

    internal IEnumerable<IBelemetryComponentContextProvider> ComponentProviders() => _services.GetServices<IBelemetryComponentContextProvider>();

    private void OnCaptureAdded(BelemetryDiagnosticCapture capture)
    {
        try
        {
            CaptureAvailable?.Invoke(capture);
        }
        catch (Exception ex)
        {
            LogFailure(_services, ex, "A CaptureAvailable handler threw.");
        }
    }

    /// <summary>A JavaScript error the page reported as a trigger (<see cref="BelemetryDiagnosticTriggers.BrowserErrors"/>).</summary>
    private void OnBrowserTrigger(string? type, string? message)
    {
        if (_recorder is null || (_hub.Options.AutoCaptureTriggers & (BelemetryDiagnosticTriggers.BrowserErrors | BelemetryDiagnosticTriggers.AllExceptions)) == 0) return;
        if (_hub.CanRecordFor(BelemetryFeatures.Errors) is false) return;

        try
        {
            // The page is not trusted (any client of a server can call this): only the type and message are taken, as a
            // trigger key, sanitized and bounded; the evidence itself comes from the page's timeline in the capture.
            var counters = new BelemetrySanitizeCounters();
            var exception = _hub.Factory.FromText(_hub.Sanitizer.Text(type, 256, ref counters) ?? "Error", _hub.Sanitizer.Text(message, 1024, ref counters), null, "browser", ref counters);
            if (exception is null) return;

            var trigger = new BelemetryDiagnosticEvent
            {
                Sequence = _recorder.NextSequence(),
                TimeUnixNano = BelemetryTime.NowUnixNano(),
                Category = BelemetryDiagnosticCategories.Exception,
                Name = "browser.trigger",
                Severity = BelemetrySeverity.Error,
                Outcome = BelemetryDiagnosticOutcome.Failure,
                Source = BelemetryDiagnosticSource.Belemetry,
                Feature = BelemetryFeatures.Errors,
                Exception = exception,
            };
            _hub.ScheduleAutomaticCapture(_recorder, trigger, "browser-error");
        }
        catch (Exception ex)
        {
            LogFailure(_services, ex, "A browser diagnostic trigger failed.");
        }
    }

    internal static void LogFailure(IServiceProvider services, Exception exception, string message)
    {
        try
        {
            using (BelemetryDiagnosticHub.Suppress())
            {
                services.GetService<ILoggerFactory>()?.CreateLogger(BelemetryLogger.DiagnosticsCategory).Log(LogLevel.Debug, exception, "{Message}", message);
            }
        }
        catch
        {
            // Diagnostics about diagnostics must never fail.
        }
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return ValueTask.CompletedTask;

        Bridge.DiagnosticTriggered -= OnBrowserTrigger;
        if (_recorder is null) return ValueTask.CompletedTask;

        _recorder.CaptureAdded -= OnCaptureAdded;
        if (_context is not null) _hub.Release(_context, _recorder, this);
        else _recorder.RemoveOwner(this, closeWhenUnowned: _hub.Settings.IsServer);

        return ValueTask.CompletedTask;
    }
}
