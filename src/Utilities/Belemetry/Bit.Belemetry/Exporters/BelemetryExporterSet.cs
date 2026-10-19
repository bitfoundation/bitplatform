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
        _sender = new BelemetryHttpSender(services.GetService<BelemetryHttpMessageHandlerProvider>()?.Handler, pipeline.LogDiagnostic);

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

    public async Task ExportAsync(BelemetryBatch batch, bool persistenceAllowed, CancellationToken cancellationToken)
    {
        foreach (var exporter in _httpExporters)
        {
            foreach (var request in exporter.CreateRequests(WithMinimumSeverity(batch, exporter.MinimumSeverity)))
            {
                var outcome = await _sender.SendAsync(exporter, request, retry: true, cancellationToken);
                if (outcome == BelemetrySendOutcome.Failed && persistenceAllowed && Store is not null)
                {
                    try
                    {
                        // The browser store sends the request itself, so it needs the headers; a file store must not get them.
                        await Store.SaveAsync(Store is BelemetryBrowserRequestStore ? request.WithHeaders(exporter.Headers) : request, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _pipeline.LogDiagnostic(ex, "Persisting an undelivered request failed; it is dropped.");
                    }
                }
            }
        }

        foreach (var exporter in _exporters)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(30));
                await exporter.ExportAsync(batch, timeout.Token);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || cancellationToken.IsCancellationRequested is false)
            {
                _pipeline.LogDiagnostic(ex, $"The exporter '{exporter.Name}' failed.");
            }
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

    /// <summary>Sends what the store holds; delivered and rejected requests leave the store, failed ones stay for the next pass.</summary>
    public async Task RetryStoredAsync(bool persistenceAllowed, CancellationToken cancellationToken)
    {
        if (Store is null || _settings.IsBrowser) return;

        if (persistenceAllowed is false)
        {
            await Store.ClearAsync();
            return;
        }

        var stored = await Store.LoadAsync(50, cancellationToken);
        foreach (var request in stored)
        {
            var exporter = _httpExporters.FirstOrDefault(e => e.Name == request.Exporter);
            if (exporter is null)
            {
                // The exporter it was meant for is no longer configured.
                await Store.DeleteAsync(request.Id, cancellationToken);
                continue;
            }

            using var _ = BelemetrySuppression.Begin();
            var outcome = await _sender.SendAsync(exporter, request, retry: false, cancellationToken);
            if (outcome != BelemetrySendOutcome.Failed)
            {
                await Store.DeleteAsync(request.Id, cancellationToken);
            }
            else
            {
                // Still offline: stop here rather than hammering the network with the rest of the backlog.
                break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var live in _liveMetrics) await live.DisposeAsync();
        _sender.Dispose();
    }
}
