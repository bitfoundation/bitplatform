using Microsoft.Extensions.DependencyInjection;

namespace Bit.Belemetry;

/// <summary>
/// The exporters configured for the .NET side, the shared HTTP sender, and the persistent store - built once, lazily,
/// by the pipeline.
/// </summary>
internal sealed class BelemetryExporterSet : IAsyncDisposable
{
    private readonly BelemetrySettings _settings;
    private readonly BelemetryPipeline _pipeline;
    private readonly BelemetryHttpSender _sender;
    private readonly List<IBelemetryHttpExporter> _httpExporters = [];
    private readonly List<IBelemetryExporter> _exporters = [];
    private readonly List<AzureMonitorLiveMetrics> _liveMetrics = [];

    public BelemetryExporterSet(BelemetrySettings settings, IServiceProvider services, BelemetryPipeline pipeline)
    {
        _settings = settings;
        _pipeline = pipeline;
        _sender = new BelemetryHttpSender(services.GetService<BelemetryHttpMessageHandlerProvider>()?.Handler, pipeline.LogDiagnostic,
                                          (ex, message) => pipeline.LogDiagnostic(ex, message, Microsoft.Extensions.Logging.LogLevel.Warning));

        var options = settings.Options;

        for (var i = 0; i < settings.OtlpExporters.Count; i++)
        {
            _httpExporters.Add(new BelemetryOtlpExporter(settings.OtlpExporters[i], options.Metrics.Temporality, $"otlp{(i == 0 ? string.Empty : i.ToString(System.Globalization.CultureInfo.InvariantCulture))}"));
        }

        for (var i = 0; i < settings.AzureMonitorExporters.Count; i++)
        {
            var azureOptions = settings.AzureMonitorExporters[i];
            try
            {
                var exporter = new BelemetryAzureMonitorExporter(azureOptions, $"azuremonitor{(i == 0 ? string.Empty : i.ToString(System.Globalization.CultureInfo.InvariantCulture))}");
                _httpExporters.Add(exporter);

                if (azureOptions.EnableLiveMetrics && settings.IsBrowser is false)
                {
                    _liveMetrics.Add(new AzureMonitorLiveMetrics(exporter, settings, pipeline.LogDiagnostic));
                }
            }
            catch (FormatException ex)
            {
                pipeline.LogDiagnostic(ex, "The Azure Monitor exporter is skipped: its connection string is invalid.");
            }
        }

        if (options.ConsoleExporter is { } console) _exporters.Add(new BelemetryConsoleExporter(console));
        _exporters.AddRange(options.Exporters);

        if (options.Persistence.Enabled)
        {
            Store = settings.IsBrowser
                ? new BelemetryBrowserRequestStore(services.GetRequiredService<BelemetryBrowserBridgeRegistry>())
                : new BelemetryFileRequestStore(options.Persistence.Directory ?? BelemetryFileRequestStore.DefaultDirectory(settings.ServiceName),
                                                options.Persistence.MaxBytes, options.Persistence.MaxAge);
        }
    }

    public IReadOnlyList<IBelemetryHttpExporter> HttpExporters => _httpExporters;

    public IBelemetryRequestStore? Store { get; }

    public void StartLiveMetrics(CancellationToken stopping)
    {
        foreach (var live in _liveMetrics) live.Start(_pipeline, stopping);
    }

    /// <param name="batch">The batch.</param>
    /// <param name="mark">
    /// The consent the batch left the queue under: once one of its features is revoked, it goes no further - to no
    /// exporter that has not had it yet, and no request of it is retried or stored - as it may hold the revoked
    /// feature's records.
    /// </param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ExportAsync(BelemetryBatch batch, BelemetryConsentMark mark, CancellationToken cancellationToken)
    {
        foreach (var exporter in _httpExporters)
        {
            // Checked before the batch is serialized for it, which would only be thrown away.
            if (mark.IsWithdrawn)
            {
                _pipeline.Self.Exported(exporter.Name, "withdrawn", Count(batch));
                continue;
            }

            foreach (var created in exporter.CreateRequests(WithMinimumSeverity(batch, exporter.MinimumSeverity)))
            {
                var sent = await _sender.SendWithResultAsync(exporter, created, retry: true, mark, cancellationToken);
                _pipeline.Self.Exported(exporter.Name, "delivered", sent.Delivered);
                _pipeline.Self.Exported(exporter.Name, "rejected", sent.Rejected);
                if (sent.Outcome == BelemetrySendOutcome.Withdrawn) _pipeline.Self.Exported(exporter.Name, "withdrawn", sent.Remaining.Count);
                if (sent.Outcome != BelemetrySendOutcome.Failed) continue;

                // After a partial success only the items to retry are left to store: the accepted ones are in.
                var request = sent.Remaining;
                _pipeline.Self.Exported(exporter.Name, await StoreAsync(exporter, request, mark, cancellationToken), request.Count);
            }
        }

        foreach (var exporter in _exporters)
        {
            if (mark.IsWithdrawn)
            {
                _pipeline.Self.Exported(exporter.Name, "withdrawn", Count(batch));
                continue;
            }

            var result = "failed";
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                result = await exporter.ExportAsync(batch, timeout.Token) == BelemetryExportResult.Success ? "delivered" : "failed";
            }
            catch (Exception ex) when (ex is not OperationCanceledException || cancellationToken.IsCancellationRequested is false)
            {
                _pipeline.LogDiagnostic(ex, $"The exporter '{exporter.Name}' failed.");
            }

            _pipeline.Self.Exported(exporter.Name, result, Count(batch));
        }
    }

    private static int Count(BelemetryBatch batch) => batch.Spans.Count + batch.Logs.Count + batch.Metrics.Sum(m => m.Points.Count);

    /// <summary>
    /// Keeps an undelivered request for a later retry, when persistence is consented and the request was not withdrawn;
    /// returns its self-telemetry outcome.
    /// </summary>
    private async Task<string> StoreAsync(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, BelemetryConsentMark mark, CancellationToken cancellationToken)
    {
        if (mark.IsWithdrawn) return "withdrawn";
        if (Store is null || _pipeline.CanRecord(BelemetryFeatures.Persistence) is false) return "failed";

        try
        {
            if (Store is BelemetryBrowserRequestStore)
            {
                // The script sends it itself, so it needs the headers - and the features, as the page may have
                // revoked one that .NET has not heard of yet. A file store must get neither.
                await Store.SaveAsync(request.ForBrowser(exporter.Headers, _pipeline.BrowserFeatures(mark.Features)), cancellationToken);
                return "stored";
            }

            await Store.SaveAsync(request, cancellationToken);

            // Checked again once it is saved: a revoke meanwhile may have cleared the store before it landed.
            if (mark.IsWithdrawn || _pipeline.CanRecord(BelemetryFeatures.Persistence) is false)
            {
                await Store.DeleteAsync(request.Id, cancellationToken);
                return mark.IsWithdrawn ? "withdrawn" : "failed";
            }

            return "stored";
        }
        catch (Exception ex)
        {
            _pipeline.LogDiagnostic(ex, "Persisting an undelivered request failed; it is dropped.");
            return "failed";
        }
    }

    /// <summary>
    /// The batch an exporter with a minimum log level gets: the same spans and metrics, and only the plain log records
    /// at or above it. Events (page views, interactions, web vitals, custom events, exceptions) are log records on the
    /// wire but not "logs" to the person setting a log level, so they always pass. The batch is shared by every
    /// exporter, so it is copied rather than trimmed - and only when a record actually falls below the level, which
    /// keeps the common case allocation-free.
    /// </summary>
    internal static BelemetryBatch WithMinimumSeverity(BelemetryBatch batch, BelemetrySeverity? minimum)
    {
        if (minimum is null || batch.Logs.Count == 0) return batch;

        List<BelemetryLogRecord>? kept = null;
        for (var i = 0; i < batch.Logs.Count; i++)
        {
            var log = batch.Logs[i];
            if (log.EventName is not null || log.Severity >= minimum.Value)
            {
                kept?.Add(log);
            }
            else if (kept is null)
            {
                kept = new List<BelemetryLogRecord>(batch.Logs.Count - 1);
                for (var j = 0; j < i; j++) kept.Add(batch.Logs[j]);
            }
        }

        return kept is null ? batch : new BelemetryBatch(batch.Resource, batch.Spans, kept, batch.Metrics);
    }

    /// <summary>
    /// Sends what the store holds; delivered and rejected requests leave the store, failed ones stay for the next pass.
    /// A request that fails holds back the rest of its URL's backlog - that backend is still unreachable, and hammering
    /// it with every stored request would only waste the network - but not the requests to other backends.
    /// </summary>
    public async Task RetryStoredAsync(bool persistenceAllowed, CancellationToken cancellationToken)
    {
        if (Store is null || _settings.IsBrowser) return;

        if (persistenceAllowed is false)
        {
            await Store.ClearAsync();
            return;
        }

        // Any revoke clears the store, whichever feature it is of. Marked first, then the clearing of any revoke the mark
        // counts is awaited: what is loaded after it is from after that revoke.
        var mark = _pipeline.Revocations.Mark(BelemetryFeatures.All);
        await _pipeline.StoreCleared;
        var stored = await Store.LoadAsync(50, cancellationToken);
        HashSet<string>? unreachable = null;
        foreach (var request in stored)
        {
            // Revoked mid-pass: the stored requests are cleared, and must not be sent or come back.
            if (mark.IsWithdrawn) return;

            var exporter = _httpExporters.FirstOrDefault(e => e.Name == request.Exporter);
            if (exporter is null || exporter.PostsTo(request.Url) is false)
            {
                // The exporter it was meant for is no longer configured.
                await Store.DeleteAsync(request.Id, cancellationToken);
                continue;
            }

            if (unreachable?.Contains(request.Url) is true) continue;

            using var _ = BelemetrySuppression.Begin();
            var sent = await _sender.SendWithResultAsync(exporter, request, retry: false, cancellationToken);
            _pipeline.Self.Exported(exporter.Name, "delivered", sent.Delivered);
            _pipeline.Self.Exported(exporter.Name, "rejected", sent.Rejected);

            if (sent.Outcome != BelemetrySendOutcome.Failed)
            {
                await Store.DeleteAsync(request.Id, cancellationToken);
            }
            else if (ReferenceEquals(sent.Remaining, request) is false)
            {
                // Partly delivered: keep only what is left, or the accepted items would be resent every pass. The
                // backend answered, so the rest of its backlog still goes.
                await Store.DeleteAsync(request.Id, cancellationToken);
                if (mark.IsWithdrawn) return;
                await Store.SaveAsync(sent.Remaining, cancellationToken);
                if (mark.IsWithdrawn) await Store.DeleteAsync(sent.Remaining.Id, cancellationToken);
            }
            else
            {
                (unreachable ??= new(StringComparer.Ordinal)).Add(request.Url);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var live in _liveMetrics) await live.DisposeAsync();
        _sender.Dispose();
    }
}
