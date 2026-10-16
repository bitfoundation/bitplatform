using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry;

/// <summary>
/// The .NET side of Belemetry: gates records on consent, applies privacy rules and processors, queues them, and
/// exports them on a timer. Hosts the collectors when <see cref="BelemetrySettings.CollectDotnet"/> is on, and is
/// also the export engine for browser records relayed to .NET.
/// </summary>
/// <remarks>
/// Everything runs on async loops - never on a dedicated thread - because browser WebAssembly has exactly one.
/// </remarks>
internal sealed class BelemetryPipeline : IAsyncDisposable
{
    private readonly object _lock = new();
    private readonly LinkedList<Entry> _queue = new();
    private readonly List<Entry> _pending = [];
    private readonly SemaphoreSlim _exportLock = new(1, 1);
    private readonly SemaphoreSlim _signal = new(0, 1);
    private readonly CancellationTokenSource _stopping = new();
    private readonly IServiceProvider _services;
    private readonly BelemetryConsentState _consent;
    private readonly BelemetryPrivacy _privacy;

    private BelemetryExporterSet? _exporters;
    private BelemetryActivityCollector? _activities;
    private BelemetryMeterCollector? _meters;
    private BelemetryAspNetCoreRequests? _requests;
    private ILogger? _diagnostics;
    private Task? _loop;
    private int _started;
    private int _queuedSpans;
    private int _queuedLogs;

    public BelemetryPipeline(BelemetrySettings settings, BelemetryConsentState consent, IServiceProvider services)
    {
        Settings = settings;
        _consent = consent;
        _services = services;
        _privacy = new BelemetryPrivacy(settings.Options.Privacy);
        _consent.Changed += OnConsentChanged;
    }

    public BelemetrySettings Settings { get; }

    /// <summary>Whether the collectors are running.</summary>
    public bool IsCollecting => _started == 1 && Settings.CollectDotnet && Settings.HasExporters;

    internal BelemetryExporterSet Exporters => _exporters ??= new BelemetryExporterSet(Settings, _services, this);

    /// <summary>Raised for every record accepted into the queue (used by live metrics).</summary>
    internal event Action<object>? RecordAccepted;

    /// <summary>Starts the collectors and the export loop. Idempotent.</summary>
    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1) return;
        if (Settings.Options.Enabled is false || Settings.HasExporters is false) return;

        if (Settings.CollectDotnet)
        {
            if ((Settings.ConfiguredFeatures & BelemetryFeatures.Traces) != 0) _activities = new BelemetryActivityCollector(Settings, this);
            if ((Settings.ConfiguredFeatures & BelemetryFeatures.Metrics) != 0) _meters = new BelemetryMeterCollector(Settings);

            if (_activities is not null && Settings.IsServer && CanRecord(BelemetryFeatures.Traces))
            {
                // Without these the Microsoft.AspNetCore request spans would carry no method, route or status.
                BelemetryAspNetCoreRequests.EnableNativeTags();
                _requests = new BelemetryAspNetCoreRequests();
            }
        }

        _loop = RunAsync(_stopping.Token);
    }

    /// <summary>
    /// Whether a record of <paramref name="feature"/> may be produced now. With <paramref name="allowPending"/>, records
    /// of features the user has not decided on yet count as producible (they are held until the decision).
    /// </summary>
    public bool CanRecord(BelemetryFeatures feature, bool allowPending = false)
    {
        if ((Settings.ConfiguredFeatures & feature) == 0) return false;

        // The server pipeline serves every user at once, so it answers to operator configuration, not user consent.
        if (Settings.IsServer) return (Settings.Options.Server.Features & feature) != 0;

        if (_consent.IsGranted(feature)) return true;
        return allowPending && _consent.IsDecided is false;
    }

    public void Enqueue(BelemetrySpan span, BelemetryResource? resource = null, bool preConsented = false)
    {
        if (preConsented is false && CanRecord(span.Feature, allowPending: true) is false) return;

        _privacy.Apply(span);
        if (Settings.IsServer is false) Enrich(span.Attributes);

        foreach (var processor in Settings.Options.Processors)
        {
            try
            {
                if (processor.OnSpan(span) is false) return;
            }
            catch
            {
                // A faulty processor skips this record rather than breaking the pipeline.
            }
        }

        Add(new Entry(span, resource ?? Settings.Resource, span.Feature, preConsented));
    }

    public void Enqueue(BelemetryLogRecord log, BelemetryResource? resource = null, bool preConsented = false)
    {
        if (preConsented is false && CanRecord(log.Feature, allowPending: true) is false) return;

        _privacy.Apply(log);
        if (Settings.IsServer is false) Enrich(log.Attributes);

        foreach (var processor in Settings.Options.Processors)
        {
            try
            {
                if (processor.OnLog(log) is false) return;
            }
            catch
            {
                // A faulty processor skips this record rather than breaking the pipeline.
            }
        }

        Add(new Entry(log, resource ?? Settings.Resource, log.Feature, preConsented));
    }

    /// <summary>Accepts a batch the browser script relayed (its consent was already applied in the page).</summary>
    public void Relay(BelemetryRelayBatch batch)
    {
        if (Settings.Options.Enabled is false || Settings.HasExporters is false) return;

        var resource = Settings.Resource.Merge(BelemetryRelayConverter.ToAttributes(batch.Resource));
        foreach (var span in BelemetryRelayConverter.ToSpans(batch)) Enqueue(span, resource, preConsented: true);
        foreach (var log in BelemetryRelayConverter.ToLogs(batch)) Enqueue(log, resource, preConsented: true);
    }

    /// <summary>Exports everything queued (and, with <paramref name="includeMetrics"/>, collects and exports metrics) now.</summary>
    public async Task FlushAsync(bool includeMetrics, CancellationToken cancellationToken)
    {
        if (Settings.HasExporters is false) return;

        while (await ExportQueuedAsync(cancellationToken)) { }
        if (includeMetrics) await ExportMetricsAsync(cancellationToken);
    }

    /// <summary>
    /// Takes everything queued and serializes it for the HTTP exporters, synchronously - for the browser to send with
    /// <c>keepalive</c> while the page unloads, when async .NET work would never complete.
    /// </summary>
    public List<BelemetryHttpRequest> DrainForUnload()
    {
        var requests = new List<BelemetryHttpRequest>();
        if (Settings.HasExporters is false) return requests;

        List<Entry> entries;
        lock (_lock)
        {
            entries = TakeReady(int.MaxValue);
        }

        var metrics = _meters is not null && CanRecord(BelemetryFeatures.Metrics) ? RunMetricProcessors(_meters.Collect()) : [];
        foreach (var batch in BuildBatches(entries, metrics))
        {
            // The browser sends these with keepalive, so they carry the exporter's headers.
            foreach (var exporter in Exporters.HttpExporters)
            {
                requests.AddRange(exporter.CreateRequests(BelemetryExporterSet.WithMinimumSeverity(batch, exporter.MinimumSeverity)).Select(r => r.WithHeaders(exporter.Headers)));
            }
        }

        return requests;
    }

    private void Enrich(BelemetryAttributes attributes)
    {
        if (_consent.SessionId is { } sessionId) attributes.SetIfAbsent(BelemetrySemanticConventions.SessionId, sessionId);
        if (_consent.IsGranted(BelemetryFeatures.UserTracking))
        {
            if (_consent.PseudoId is { } pseudoId) attributes.SetIfAbsent(BelemetrySemanticConventions.EndUserPseudoId, pseudoId);
            if (_consent.UserId is { } userId) attributes.SetIfAbsent(BelemetrySemanticConventions.UserId, userId);
        }
    }

    private void Add(Entry entry)
    {
        var signal = false;

        lock (_lock)
        {
            var held = entry.PreConsented is false && Settings.IsServer is false && _consent.IsGranted(entry.Feature) is false;
            if (held)
            {
                // Not decided yet: keep it in memory - never on the wire, never on disk - until the user chooses.
                if (_pending.Count >= Math.Max(0, Settings.Options.Consent.PendingLimit)) _pending.RemoveAt(0);
                _pending.Add(entry);
                return;
            }

            AddToQueue(entry);
            signal = _queuedSpans + _queuedLogs >= Settings.Options.Batch.MaxBatchSize;
        }

        RecordAccepted?.Invoke(entry.Record);
        if (signal) Signal();
    }

    private void AddToQueue(Entry entry)
    {
        var max = Math.Max(1, Settings.Options.Batch.MaxQueueSize);
        var isSpan = entry.Record is BelemetrySpan;

        if ((isSpan ? _queuedSpans : _queuedLogs) >= max)
        {
            // Drop the oldest record of the same signal: fresh data is worth more than stale data.
            for (var node = _queue.First; node is not null; node = node.Next)
            {
                if ((node.Value.Record is BelemetrySpan) == isSpan)
                {
                    _queue.Remove(node);
                    if (isSpan) _queuedSpans--; else _queuedLogs--;
                    break;
                }
            }
        }

        _queue.AddLast(entry);
        if (isSpan) _queuedSpans++; else _queuedLogs++;
    }

    private List<Entry> TakeReady(int max)
    {
        var taken = new List<Entry>(Math.Min(max, _queue.Count));
        while (taken.Count < max && _queue.First is { } node)
        {
            _queue.RemoveFirst();
            if (node.Value.Record is BelemetrySpan) _queuedSpans--; else _queuedLogs--;
            taken.Add(node.Value);
        }
        return taken;
    }

    private void OnConsentChanged(BelemetryFeatures previous, BelemetryFeatures current)
    {
        if (Settings.IsServer) return;

        var revoked = previous & ~current;
        lock (_lock)
        {
            if (revoked != BelemetryFeatures.None)
            {
                for (var node = _queue.First; node is not null;)
                {
                    var next = node.Next;
                    if (node.Value.PreConsented is false && (node.Value.Feature & revoked) != 0)
                    {
                        _queue.Remove(node);
                        if (node.Value.Record is BelemetrySpan) _queuedSpans--; else _queuedLogs--;
                    }
                    node = next;
                }
            }

            if (_consent.IsDecided)
            {
                foreach (var entry in _pending)
                {
                    if (_consent.IsGranted(entry.Feature)) AddToQueue(entry);
                }
                _pending.Clear();
            }
        }

        if ((revoked & BelemetryFeatures.Persistence) != 0) _ = ClearStoreAsync();
        Signal();
    }

    private async Task ClearStoreAsync()
    {
        try
        {
            if (Exporters.Store is { } store) await store.ClearAsync();
        }
        catch (Exception ex)
        {
            LogDiagnostic(ex, "Clearing the persisted telemetry failed.");
        }
    }

    private async Task RunAsync(CancellationToken stopping)
    {
        // Yield first so Start() returns immediately; on WebAssembly this also lets the app finish booting.
        await Task.Yield();

        // Said here rather than in Start(), which runs while the logger factory is still being built. A warning, not a
        // diagnostic: it is a misconfiguration that loses a whole signal without any other symptom.
        if (_meters is not null && Settings.Meters.Count > 0 && Settings.MetricsSupported is false)
        {
            LogDiagnostic(null, "System.Diagnostics.Metrics is turned off in this app (MetricsSupport=false), so no Meter records anything and no metrics are exported. " +
                                "The WebAssembly SDK defaults to that; the Bit.Belemetry package turns it back on unless the project sets it, or references Bit.Belemetry as a project rather than a package. " +
                                "Set <MetricsSupport>true</MetricsSupport> in the project file, or remove BelemetryFeatures.Metrics from BelemetryOptions.Features to silence this warning.",
                          LogLevel.Warning);
        }

        // Misconfigurations the settings found while resolving the options (an exporter the host cannot use, ...):
        // settings have no logger of their own, and like the metrics warning above these deserve a Warning.
        foreach (var warning in Settings.StartupWarnings) LogDiagnostic(null, warning, LogLevel.Warning);

        var options = Settings.Options;
        var flushInterval = options.Batch.FlushInterval > TimeSpan.Zero ? options.Batch.FlushInterval : TimeSpan.FromSeconds(5);
        var metricsInterval = options.Metrics.ExportInterval > TimeSpan.Zero ? options.Metrics.ExportInterval : TimeSpan.FromSeconds(60);
        var retryInterval = options.Persistence.RetryInterval > TimeSpan.Zero ? options.Persistence.RetryInterval : TimeSpan.FromMinutes(1);
        var nextMetrics = DateTime.UtcNow + metricsInterval;
        var nextRetry = DateTime.UtcNow + TimeSpan.FromSeconds(10);

        Exporters.StartLiveMetrics(stopping);

        while (stopping.IsCancellationRequested is false)
        {
            try
            {
                await _signal.WaitAsync(flushInterval, stopping);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                while (await ExportQueuedAsync(stopping)) { }

                if (_meters is not null && DateTime.UtcNow >= nextMetrics)
                {
                    nextMetrics = DateTime.UtcNow + metricsInterval;
                    await ExportMetricsAsync(stopping);
                }

                if (DateTime.UtcNow >= nextRetry)
                {
                    nextRetry = DateTime.UtcNow + retryInterval;
                    await Exporters.RetryStoredAsync(CanRecord(BelemetryFeatures.Persistence), stopping);
                }
            }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LogDiagnostic(ex, "The export loop failed; it continues with the next interval.");
            }
        }
    }

    /// <summary>Exports one batch of queued records; returns whether more are waiting.</summary>
    private async Task<bool> ExportQueuedAsync(CancellationToken cancellationToken)
    {
        List<Entry> entries;
        bool more;
        lock (_lock)
        {
            entries = TakeReady(Math.Max(1, Settings.Options.Batch.MaxBatchSize));
            more = _queue.Count > 0;
        }

        if (entries.Count == 0) return false;

        foreach (var batch in BuildBatches(entries, []))
        {
            await ExportAsync(batch, cancellationToken);
        }

        return more;
    }

    private async Task ExportMetricsAsync(CancellationToken cancellationToken)
    {
        if (_meters is null) return;

        var metrics = RunMetricProcessors(_meters.Collect());

        // Collected either way so the delta intervals stay aligned; only sent when consented.
        if (metrics.Count == 0 || CanRecord(BelemetryFeatures.Metrics) is false) return;

        await ExportAsync(new BelemetryBatch(Settings.Resource, metrics: metrics), cancellationToken);
    }

    private List<BelemetryMetric> RunMetricProcessors(List<BelemetryMetric> metrics)
    {
        if (Settings.Options.Processors.Count == 0) return metrics;

        return metrics.Where(metric => Settings.Options.Processors.All(p =>
        {
            try
            {
                return p.OnMetric(metric);
            }
            catch
            {
                return true;
            }
        })).ToList();
    }

    private async Task ExportAsync(BelemetryBatch batch, CancellationToken cancellationToken)
    {
        await _exportLock.WaitAsync(cancellationToken);
        try
        {
            using var _ = BelemetrySuppression.Begin();
            await Exporters.ExportAsync(batch, CanRecord(BelemetryFeatures.Persistence), cancellationToken);
        }
        finally
        {
            _exportLock.Release();
        }
    }

    private IEnumerable<BelemetryBatch> BuildBatches(List<Entry> entries, List<BelemetryMetric> metrics)
    {
        // Records relayed from different browsers carry different resources; OTLP and Azure both group per resource.
        foreach (var group in entries.GroupBy(e => e.Resource))
        {
            var spans = group.Select(e => e.Record).OfType<BelemetrySpan>().ToList();
            var logs = group.Select(e => e.Record).OfType<BelemetryLogRecord>().ToList();
            yield return new BelemetryBatch(group.Key, spans, logs);
        }

        if (metrics.Count > 0) yield return new BelemetryBatch(Settings.Resource, metrics: metrics);
    }

    private void Signal()
    {
        try
        {
            if (_signal.CurrentCount == 0) _signal.Release();
        }
        catch (SemaphoreFullException)
        {
            // Already signalled.
        }
    }

    /// <summary>Logs at Debug, or at Information with <see cref="BelemetryOptions.Debug"/>.</summary>
    internal void LogDiagnostic(Exception? exception, string message) =>
        LogDiagnostic(exception, message, Settings.Options.Debug ? LogLevel.Information : LogLevel.Debug);

    internal void LogDiagnostic(Exception? exception, string message, LogLevel level)
    {
        try
        {
            // Resolved lazily: the logger factory depends on this pipeline through the logger provider, so taking
            // ILogger in the constructor would be a cycle.
            _diagnostics ??= _services.GetService<ILoggerFactory>()?.CreateLogger(BelemetryLogger.DiagnosticsCategory);
            _diagnostics?.Log(level, exception, "{Message}", message);
        }
        catch
        {
            // Diagnostics must never fail the pipeline.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _consent.Changed -= OnConsentChanged;
        _activities?.Dispose();
        _requests?.Dispose();

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await FlushAsync(includeMetrics: true, timeout.Token);
        }
        catch
        {
            // Best effort on shutdown.
        }

        _meters?.Dispose();
        await _stopping.CancelAsync();
        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch
            {
                // Already stopping.
            }
        }

        if (_exporters is not null) await _exporters.DisposeAsync();
        _stopping.Dispose();
    }

    private sealed record Entry(object Record, BelemetryResource Resource, BelemetryFeatures Feature, bool PreConsented);
}
