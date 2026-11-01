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
    private readonly BelemetryErrorLimiter? _errors;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<IBelemetryProcessor, byte> _failedProcessors = new(ReferenceEqualityComparer.Instance);

    private BelemetryExporterSet? _exporters;
    private BelemetryActivityCollector? _activities;
    private BelemetryMeterCollector? _meters;
    private BelemetryAspNetCoreRequests? _requests;
    private ILogger? _diagnostics;
    private Task? _loop;
    private int _started;
    private int _disposed;
    private int _queuedSpans;
    private int _queuedLogs;
    private Task _storeCleared = Task.CompletedTask;

    public BelemetryPipeline(BelemetrySettings settings, BelemetryConsentState consent, IServiceProvider services)
    {
        Settings = settings;
        _consent = consent;
        _services = services;
        _privacy = new BelemetryPrivacy(settings.Options.Privacy);

        // One user's app floods only its own queue and backend quota; a server's errors are every user's, and a cap
        // there would hide real failures under load.
        _errors = settings.IsServer ? null : new BelemetryErrorLimiter(settings.Options.Errors);
        _consent.Changed += OnConsentChanged;
    }

    public BelemetrySettings Settings { get; }

    /// <summary>Whether the collectors are running.</summary>
    public bool IsCollecting => _started == 1 && Settings.CollectDotnet && Settings.HasExporters;

    internal BelemetryExporterSet Exporters => _exporters ??= new BelemetryExporterSet(Settings, _services, this);

    /// <summary>The user's revokes: a batch built before a revoke of one of its features goes no further.</summary>
    internal BelemetryRevocations Revocations { get; } = new();

    /// <summary>
    /// Completes once the store is cleared after the last revoke. Set before the revoke is counted, so a resend pass
    /// whose mark counts a revoke waits for its clearing rather than loading the requests it is about to remove.
    /// </summary>
    internal Task StoreCleared => Volatile.Read(ref _storeCleared);

    /// <summary>Belemetry's own health metrics (the <c>Bit.Belemetry</c> meter).</summary>
    internal BelemetrySelfTelemetry Self { get; } = new();

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

        // The loop outlives whoever started it - the first resolution of the logger factory, which can happen on a
        // MAUI/WPF/WinForms UI thread or inside a request. Started as is, every await in it would resume on that
        // thread's SynchronizationContext (serialization, gzip and file I/O on the UI thread) and carry its ambient
        // state for good (Activity.Current, AsyncLocal values). With the flow suppressed it carries none of that state.
        // SuppressFlow throws when the flow is suppressed already - and then nothing is captured anyway.
        if (ExecutionContext.IsFlowSuppressed())
        {
            _loop = StartLoop();
        }
        else
        {
            using (ExecutionContext.SuppressFlow())
            {
                _loop = StartLoop();
            }
        }
    }

    /// <summary>
    /// Task.Run gives the loop a thread-pool thread, off any UI thread. Not in the browser: with WasmEnableThreads that
    /// is a worker thread, where JS interop - the request store hands undelivered requests to the script - is not
    /// allowed. Started in place, it stays on the thread that started it, the one there is without threads.
    /// </summary>
    private Task StartLoop() => OperatingSystem.IsBrowser() ? RunAsync(_stopping.Token) : Task.Run(() => RunAsync(_stopping.Token));

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

    /// <summary>
    /// Whether a log message must not render the value of <paramref name="key"/>: a blocked or redacted attribute, or a
    /// user id the record will not carry because the user has not granted <see cref="BelemetryFeatures.UserTracking"/>.
    /// </summary>
    internal bool HidesValue(string key) =>
        _privacy.HidesValue(key) || (Settings.IsServer is false && _consent.IsGranted(BelemetryFeatures.UserTracking) is false && IsUserId(key));

    /// <summary>Whether <paramref name="key"/> is one of the user ids, which only <see cref="BelemetryFeatures.UserTracking"/> lets a record carry.</summary>
    internal static bool IsUserId(string key) => key is BelemetrySemanticConventions.EndUserPseudoId or BelemetrySemanticConventions.UserId;

    /// <summary>
    /// The features the browser script is told a request's records belong to (<see cref="BelemetryHttpRequest.Features"/>):
    /// <see cref="BelemetryFeatures.UserTracking"/> only while it is granted, as the records carry no user id otherwise,
    /// and the script would give up every request of a page that never granted it.
    /// </summary>
    internal BelemetryFeatures BrowserFeatures(BelemetryFeatures features) =>
        _consent.IsGranted(BelemetryFeatures.UserTracking) ? features : features & ~BelemetryFeatures.UserTracking;

    /// <param name="span">The span.</param>
    /// <param name="resource">Its resource; the .NET side's when null.</param>
    /// <param name="relayed">Relayed by the browser script, which applied the user's consent and the error limits already.</param>
    public void Enqueue(BelemetrySpan span, BelemetryResource? resource = null, bool relayed = false)
    {
        if (relayed is false && CanRecord(span.Feature, allowPending: true) is false) return;

        _privacy.Apply(span);
        BelemetryValueLimit.Apply(span, Settings.Options.MaxValueLength);
        var userIds = Settings.IsServer is false && Enrich(span, relayed);

        foreach (var processor in Settings.Options.Processors)
        {
            try
            {
                if (processor.OnSpan(span) is false)
                {
                    Self.Dropped("filtered", span: true);
                    return;
                }
            }
            catch (Exception ex)
            {
                // A faulty processor is skipped for this record rather than breaking the pipeline.
                ReportProcessorFailure(processor, ex);
            }
        }

        Add(new Entry(span, resource ?? Settings.Resource, span.Feature, PreConsented: relayed, userIds));
    }

    /// <param name="log">The log record.</param>
    /// <param name="resource">Its resource; the .NET side's when null.</param>
    /// <param name="relayed">Relayed by the browser script, which applied the user's consent and the error limits already.</param>
    public void Enqueue(BelemetryLogRecord log, BelemetryResource? resource = null, bool relayed = false)
    {
        if (relayed is false && CanRecord(log.Feature, allowPending: true) is false) return;

        if (relayed is false && log.Feature == BelemetryFeatures.Errors && _errors?.ShouldRecord(log) is false)
        {
            Self.Dropped("error_limit", span: false);
            return;
        }

        _privacy.Apply(log);
        BelemetryValueLimit.Apply(log, Settings.Options.MaxValueLength);
        var userIds = Settings.IsServer is false && Enrich(log, relayed);

        foreach (var processor in Settings.Options.Processors)
        {
            try
            {
                if (processor.OnLog(log) is false)
                {
                    Self.Dropped("filtered", span: false);
                    return;
                }
            }
            catch (Exception ex)
            {
                // A faulty processor is skipped for this record rather than breaking the pipeline.
                ReportProcessorFailure(processor, ex);
            }
        }

        Add(new Entry(log, resource ?? Settings.Resource, log.Feature, PreConsented: relayed, userIds));
    }

    /// <summary>
    /// Accepts a batch the browser script relayed (its consent was already applied in the page). With a
    /// <paramref name="source"/>, the page's rate limit applies and its resource is reused across batches.
    /// </summary>
    public void Relay(BelemetryRelayBatch batch, BelemetryRelaySource? source = null)
    {
        if (Settings.Options.Enabled is false || Settings.HasExporters is false) return;

        var budget = int.MaxValue;
        if (source?.Limiter is { } limiter)
        {
            var logCount = Math.Min(BelemetryRelayConverter.MaxRecordsPerBatch, batch.Logs?.Count ?? 0);
            var spanCount = Math.Min(BelemetryRelayConverter.MaxRecordsPerBatch, batch.Spans?.Count ?? 0);
            budget = limiter.Take(logCount + spanCount);
            if (budget < logCount + spanCount)
            {
                if (source.Dropped == 0) LogDiagnostic(null, "A page relays more records than its rate limit allows; the excess is dropped.", LogLevel.Warning);
                source.Dropped += logCount + spanCount - budget;
                Self.Dropped("relay_limit", span: false, Math.Max(0, logCount - budget));
                Self.Dropped("relay_limit", span: true, Math.Min(spanCount, logCount + spanCount - budget));
            }
            if (budget == 0) return;
        }

        var resource = RelayResource(batch, source);

        // Logs first: errors, page views and events are worth more than dependency spans when the budget runs out. A
        // feature the configuration switched off is dropped even though the page says it consented: a page can be
        // anything that speaks the interop protocol.
        foreach (var log in BelemetryRelayConverter.ToLogs(batch))
        {
            if (budget-- <= 0) return;
            if ((Settings.ConfiguredFeatures & log.Feature) != 0) Enqueue(log, resource, relayed: true);
        }

        foreach (var span in BelemetryRelayConverter.ToSpans(batch))
        {
            if (budget-- <= 0) return;
            if ((Settings.ConfiguredFeatures & span.Feature) != 0) Enqueue(span, resource, relayed: true);
        }
    }

    // The resource keys that say which service a record belongs to.
    private static readonly string[] _serviceIdentity =
    [
        BelemetrySemanticConventions.ServiceName, BelemetrySemanticConventions.ServiceNamespace,
        BelemetrySemanticConventions.ServiceVersion, BelemetrySemanticConventions.DeploymentEnvironmentName,
    ];

    private BelemetryResource RelayResource(BelemetryRelayBatch batch, BelemetryRelaySource? source)
    {
        var key = batch.Resource is null ? string.Empty : string.Join('\u0001', batch.Resource.Select(p => p.Key + '\u0002' + p.Value.GetRawText()));
        if (source is { Resource: { } cached } && source.ResourceKey == key) return cached;

        var browser = BelemetryRelayConverter.ToAttributes(batch.Resource);
        BelemetryResource resource;
        if (Settings.IsServer)
        {
            // Anyone can open a server's pages: a page describes itself (browser, language, its tab's instance id), but
            // which service it belongs to is the server's to say. And the server's own host, process and cloud
            // attributes describe the server, not the page, so they are not added - as with the Direct transport.
            resource = new BelemetryResource();
            foreach (var name in _serviceIdentity) resource.Attributes.Set(name, Settings.Resource.Attributes[name]);
            foreach (var (attribute, value) in browser)
            {
                if (Array.IndexOf(_serviceIdentity, attribute) < 0) resource.Attributes.Set(attribute, value);
            }
        }
        else
        {
            // WebAssembly and Hybrid: the page runs on the device the process describes.
            resource = Settings.Resource.Merge(browser);
        }

        if (source is not null)
        {
            source.ResourceKey = key;
            source.Resource = resource;
        }

        return resource;
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

        // The browser sends these with keepalive, so they carry the exporter's headers - and the features of their
        // records, as the page may have revoked one that .NET has not heard of yet.
        var features = BrowserFeatures(FeaturesOf(entries, metrics: metrics.Count > 0));
        foreach (var batch in BuildBatches(entries, metrics))
        {
            foreach (var exporter in Exporters.HttpExporters)
            {
                requests.AddRange(exporter.CreateRequests(BelemetryExporterSet.WithMinimumSeverity(batch, exporter.MinimumSeverity)).Select(r => r.ForBrowser(exporter.Headers, features)));
            }
        }

        return requests;
    }

    /// <summary>
    /// The features of queued records (and of metrics), with <see cref="BelemetryFeatures.UserTracking"/>: any of them
    /// may carry the user ids, so a revoke of it withdraws them too.
    /// </summary>
    private static BelemetryFeatures FeaturesOf(List<Entry> entries, bool metrics = false)
    {
        var features = BelemetryFeatures.UserTracking | (metrics ? BelemetryFeatures.Metrics : BelemetryFeatures.None);
        foreach (var entry in entries) features |= entry.Feature;
        return features;
    }

    /// <returns>Whether the record may carry the user ids: <see cref="BelemetryFeatures.UserTracking"/> was granted.</returns>
    private bool Enrich(object record, bool relayed)
    {
        var attributes = record is BelemetrySpan span ? span.Attributes : ((BelemetryLogRecord)record).Attributes;

        // After the privacy rules, which leave the ids alone - except an id the operator blocked, which is not added.
        if (_consent.SessionId is { } sessionId && _privacy.IsBlocked(BelemetrySemanticConventions.SessionId) is false)
        {
            attributes.SetIfAbsent(BelemetrySemanticConventions.SessionId, sessionId);
        }

        if (_consent.IsGranted(BelemetryFeatures.UserTracking))
        {
            if (_consent.PseudoId is { } pseudoId && _privacy.IsBlocked(BelemetrySemanticConventions.EndUserPseudoId) is false)
            {
                attributes.SetIfAbsent(BelemetrySemanticConventions.EndUserPseudoId, pseudoId);
            }

            if (_consent.UserId is { } userId && _privacy.IsBlocked(BelemetrySemanticConventions.UserId) is false)
            {
                attributes.SetIfAbsent(BelemetrySemanticConventions.UserId, userId);
            }

            return true;
        }

        if (relayed is false) RemoveUserIds(record);
        return false;
    }

    /// <summary>
    /// Takes the user ids off a record: its attributes, those of a span's events and links, and a log message that
    /// renders one, which falls back to its template.
    /// </summary>
    private static void RemoveUserIds(object record)
    {
        if (record is BelemetrySpan span)
        {
            RemoveUserIds(span.Attributes);
            foreach (var e in span.Events) RemoveUserIds(e.Attributes);
            foreach (var link in span.Links) RemoveUserIds(link.Attributes);
        }
        else
        {
            var log = (BelemetryLogRecord)record;
            RemoveUserIds(log.Attributes);
            if (log.BodyTemplate is { } template) log.Body = template;
        }
    }

    private static void RemoveUserIds(BelemetryAttributes attributes)
    {
        attributes.Remove(BelemetrySemanticConventions.EndUserPseudoId);
        attributes.Remove(BelemetrySemanticConventions.UserId);
    }

    private void Add(Entry entry)
    {
        var signal = false;

        lock (_lock)
        {
            // Under the lock, as a revoke of UserTracking takes the ids off the queued records: a record enriched while it
            // was granted cannot slip into the queue with them just after one. A relayed record had the page's consent applied.
            if (entry.UserIds && entry.PreConsented is false && _consent.IsGranted(BelemetryFeatures.UserTracking) is false)
            {
                RemoveUserIds(entry.Record);
            }

            var held = entry.PreConsented is false && Settings.IsServer is false && _consent.IsGranted(entry.Feature) is false;
            if (held)
            {
                // Not decided yet: keep it in memory - never on the wire, never on disk - until the user chooses.
                if (Settings.Options.Consent.PendingLimit <= 0)
                {
                    Self.Dropped("pending_full", entry.Record is BelemetrySpan);
                    return;
                }
                if (_pending.Count >= Settings.Options.Consent.PendingLimit)
                {
                    Self.Dropped("pending_full", _pending[0].Record is BelemetrySpan);
                    _pending.RemoveAt(0);
                }
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
                    Self.Dropped("queue_full", isSpan);
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
        TaskCompletionSource? cleared = null;
        lock (_lock)
        {
            if (revoked != BelemetryFeatures.None)
            {
                // Before the revoke is counted: a resend pass that counts it waits for the store to be cleared.
                cleared = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Volatile.Write(ref _storeCleared, cleared.Task);

                // Under the lock, with the purge: a batch taken from the queue before it is the one that escaped it.
                Revocations.Revoke(revoked);

                for (var node = _queue.First; node is not null;)
                {
                    var next = node.Next;
                    if ((node.Value.Feature & revoked) != 0)
                    {
                        _queue.Remove(node);
                        if (node.Value.Record is BelemetrySpan) _queuedSpans--; else _queuedLogs--;
                        Self.Dropped("consent_revoked", node.Value.Record is BelemetrySpan);
                    }
                    node = next;
                }
            }

            if (_consent.IsDecided)
            {
                foreach (var entry in _pending)
                {
                    if (_consent.IsGranted(entry.Feature)) AddToQueue(entry);
                    else Self.Dropped("not_consented", entry.Record is BelemetrySpan);
                }
                _pending.Clear();
            }

            if ((revoked & BelemetryFeatures.UserTracking) != 0)
            {
                foreach (var entry in _queue.Concat(_pending)) RemoveUserIds(entry.Record);
            }
        }

        if (cleared is not null) _ = ClearStoreAsync(cleared);
        Signal();
    }

    private async Task ClearStoreAsync(TaskCompletionSource cleared)
    {
        try
        {
            if (Exporters.Store is { } store) await store.ClearAsync();
        }
        catch (Exception ex)
        {
            LogDiagnostic(ex, "Clearing the persisted telemetry failed.");
        }
        finally
        {
            cleared.TrySetResult();
        }
    }

    private async Task RunAsync(CancellationToken stopping)
    {
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
        BelemetryConsentMark mark;
        lock (_lock)
        {
            entries = TakeReady(Math.Max(1, Settings.Options.Batch.MaxBatchSize));
            more = _queue.Count > 0;

            // Under the lock, with the take: a revoke from here on is one these records escaped the purge of.
            mark = Revocations.Mark(FeaturesOf(entries));
        }

        if (entries.Count == 0) return false;

        foreach (var batch in BuildBatches(entries, []))
        {
            await ExportAsync(batch, mark, cancellationToken);
        }

        return more;
    }

    private async Task ExportMetricsAsync(CancellationToken cancellationToken)
    {
        if (_meters is null) return;

        // Before the grant is read, to take the user ids off and to send at all: a revoke after it withdraws the batch.
        var mark = Revocations.Mark(BelemetryFeatures.Metrics | BelemetryFeatures.UserTracking);
        var metrics = RunMetricProcessors(_meters.Collect());

        // Collected either way so the delta intervals stay aligned; only sent when consented.
        if (metrics.Count == 0 || CanRecord(BelemetryFeatures.Metrics) is false) return;

        await ExportAsync(new BelemetryBatch(Settings.Resource, metrics: metrics), mark, cancellationToken);
    }

    private List<BelemetryMetric> RunMetricProcessors(List<BelemetryMetric> metrics)
    {
        // Consent's to add, as on spans and logs: without UserTracking a measurement's user id is dropped.
        var userIds = Settings.IsServer || _consent.IsGranted(BelemetryFeatures.UserTracking);
        foreach (var metric in metrics)
        {
            foreach (var point in metric.Points)
            {
                _privacy.ApplyToAttributes(point.Attributes);
                if (userIds is false) RemoveUserIds(point.Attributes);
            }
        }

        if (Settings.Options.Processors.Count == 0) return metrics;

        return metrics.Where(metric => Settings.Options.Processors.All(p =>
        {
            try
            {
                return p.OnMetric(metric);
            }
            catch (Exception ex)
            {
                ReportProcessorFailure(p, ex);
                return true;
            }
        })).ToList();
    }

    /// <summary>
    /// Says once per processor that it threw: silently, a broken enrichment or filter would look like data that is
    /// just not there. Once, because it runs for every record and would otherwise flood the log it is reported to.
    /// </summary>
    private void ReportProcessorFailure(IBelemetryProcessor processor, Exception exception)
    {
        if (_failedProcessors.TryAdd(processor, 0) is false) return;
        LogDiagnostic(exception, $"The processor {processor.GetType().FullName} threw; it is skipped for every record it throws on (only this first failure is logged).", LogLevel.Warning);
    }

    private async Task ExportAsync(BelemetryBatch batch, BelemetryConsentMark mark, CancellationToken cancellationToken)
    {
        await _exportLock.WaitAsync(cancellationToken);
        try
        {
            using var _ = BelemetrySuppression.Begin();
            await Exporters.ExportAsync(batch, mark, cancellationToken);
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
        // Idempotent: a host may dispose the container while the app disposes the pipeline it resolved itself.
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

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
        Self.Dispose();
        _stopping.Dispose();
    }

    /// <param name="Record">The span or log record.</param>
    /// <param name="Resource">Its resource.</param>
    /// <param name="Feature">Its consent feature.</param>
    /// <param name="PreConsented">Relayed by the browser script, which applied the page's consent.</param>
    /// <param name="UserIds">Enriched while <see cref="BelemetryFeatures.UserTracking"/> was granted: it may carry the user ids.</param>
    private sealed record Entry(object Record, BelemetryResource Resource, BelemetryFeatures Feature, bool PreConsented, bool UserIds);
}
