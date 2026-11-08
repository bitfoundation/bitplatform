using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Streams Application Insights Live Metrics (the QuickPulse protocol, API version <see cref="ApiVersion"/>): it pings
/// the live endpoint every few seconds and, while someone has the Live Metrics blade open (the service answers
/// <c>x-ms-qps-subscribed: true</c>), posts one data point per second with request, dependency and exception rates plus
/// process CPU and memory. Each response carries the portal's collection configuration
/// (<see cref="AzureMonitorLiveConfiguration"/>) when it changed: the data points then also carry the sample telemetry
/// its streams take, as filtered there, and the values of its custom charts.
/// </summary>
/// <remarks>Not available in the browser: the live endpoint does not allow cross-origin calls.</remarks>
internal sealed class AzureMonitorLiveMetrics : IAsyncDisposable
{
    internal const string ApiVersion = "2024-04-01-preview";

    // 5: filters documents and metrics as the portal configures them (as the Azure Monitor OpenTelemetry distros say).
    private const int InvariantVersion = 5;

    // A bound on the samples waiting for the next data point while a post is slow; the streams' quotas keep it far lower.
    private const int MaxDocuments = 100;

    // The configuration is a few kilobytes; nothing a live endpoint answers should take more.
    private const int MaxResponseBytes = 1024 * 1024;

    private readonly BelemetryAzureMonitorExporter _exporter;
    private readonly BelemetrySettings _settings;
    private readonly HttpClient _client;
    private readonly string _streamId = Guid.NewGuid().ToString("N");
    private readonly Action<Exception?, string> _diagnostic;
    private readonly TimeProvider _time;

    // The samples waiting for the next data point, with the consent feature of the record each stands for. The lock makes a
    // configuration's replacement and the enqueueing of a sample taken for it one or the other, never both (Sample).
    private readonly ConcurrentQueue<(BelemetryFeatures Feature, byte[] Document)> _documents = new();
    private readonly object _documentsLock = new();
    private BelemetryPipeline? _pipeline;
    private Uri _endpoint;
    private string? _etag;
    private string? _unreadableEtag;
    private Task? _loop;

    // The configuration applied while someone watches; null otherwise, when nothing is sampled.
    private volatile AzureMonitorLiveConfiguration? _configuration;
    private int _documentCount;
    private int _sampleFailed;

    private long _requests, _requestsFailed, _requestTicks;
    private long _dependencies, _dependenciesFailed, _dependencyTicks;
    private long _exceptions;
    private TimeSpan _lastCpu;
    private long _lastCpuTimestamp;

    /// <summary>A Live Metrics stream for <paramref name="exporter"/>; it does nothing until <see cref="Start"/>.</summary>
    /// <param name="exporter">The exporter whose resource it streams to, and whose signals and log level the samples follow.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="diagnostic">Where it reports its failures.</param>
    /// <param name="handler">The handler that sends its requests (tests); a new one when null.</param>
    /// <param name="time">The clock the streams' quotas earn samples back by (tests); the system's when null.</param>
    public AzureMonitorLiveMetrics(BelemetryAzureMonitorExporter exporter, BelemetrySettings settings, Action<Exception?, string> diagnostic, HttpMessageHandler? handler = null,
                                   TimeProvider? time = null)
    {
        _exporter = exporter;
        _settings = settings;
        _diagnostic = diagnostic;
        _time = time ?? TimeProvider.System;
        _endpoint = exporter.ConnectionString.LiveEndpoint;
        _client = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _client.Timeout = TimeSpan.FromSeconds(10);
        _client.MaxResponseContentBufferSize = MaxResponseBytes;
    }

    /// <summary>Counts and samples the records <paramref name="pipeline"/> accepts, and pings and posts until <paramref name="stopping"/>.</summary>
    public void Start(BelemetryPipeline pipeline, CancellationToken stopping)
    {
        _pipeline = pipeline;
        pipeline.RecordAccepted += OnRecord;
        _loop = RunAsync(pipeline, stopping);
    }

    /// <summary>The exceptions counted since the last data point.</summary>
    internal long Exceptions => Interlocked.Read(ref _exceptions);

    /// <summary>The configuration applied; null while nobody watches.</summary>
    internal AzureMonitorLiveConfiguration? Configuration => _configuration;

    /// <summary>Counts a record the pipeline accepted into the data point's rates and, while someone watches, samples it.</summary>
    internal void OnRecord(object record)
    {
        switch (record)
        {
            case BelemetrySpan span:
                if (span.Kind is BelemetrySpanKind.Server or BelemetrySpanKind.Consumer)
                {
                    Interlocked.Increment(ref _requests);
                    Interlocked.Add(ref _requestTicks, span.Duration.Ticks);
                    if (IsFailure(span, 500)) Interlocked.Increment(ref _requestsFailed);
                }
                else
                {
                    Interlocked.Increment(ref _dependencies);
                    Interlocked.Add(ref _dependencyTicks, span.Duration.Ticks);
                    if (IsFailure(span, 400)) Interlocked.Increment(ref _dependenciesFailed);
                }

                // The exceptions recorded on it count as the Azure Monitor exporter counts them (a stack trace alone is no
                // item) - but not one the error limits dropped, nor one whose log record was counted already.
                foreach (var spanEvent in span.Events)
                {
                    if (spanEvent.IsException && spanEvent.Attributes.HasException && spanEvent.Limited is false && spanEvent.LoggedAs is null)
                    {
                        Interlocked.Increment(ref _exceptions);
                    }
                }
                break;
            case BelemetryLogRecord log when log.HasException:
                Interlocked.Increment(ref _exceptions);
                break;
        }

        if (_configuration is not { } configuration) return;

        try
        {
            Sample(configuration, record);
        }
        catch (Exception ex)
        {
            // Never into the pipeline that raised the event: the record is in, Live Metrics only misses its sample.
            if (Interlocked.Exchange(ref _sampleFailed, 1) == 0) _diagnostic(ex, "Live Metrics: a record could not be sampled.");
        }
    }

    /// <summary>
    /// Offers <paramref name="record"/> to the document streams and custom charts of <paramref name="configuration"/> - only
    /// what the exporter itself sends (its signals and minimum log level): Live Metrics shows no data Application Insights
    /// was not meant to get.
    /// </summary>
    private void Sample(AzureMonitorLiveConfiguration configuration, object record)
    {
        var options = _exporter.Options;

        switch (record)
        {
            case BelemetrySpan span when options.Signals.HasFlag(BelemetrySignals.Traces):
                if (configuration.Takes(AzureMonitorLiveItem.TypeOf(span))) Sample(configuration, AzureMonitorLiveItem.FromSpan(span), span.Feature);

                // Its events the exporter sends as items of their own (AzureMonitorEnvelopeSerializer.IsItem): an exception as an
                // Exception, any other named event as a Message - a trace.
                var exceptions = configuration.Takes(AzureMonitorLiveTelemetryType.Exception);
                var traces = configuration.Takes(AzureMonitorLiveTelemetryType.Trace);
                if (exceptions is false && traces is false) break;

                foreach (var spanEvent in span.Events)
                {
                    if (spanEvent.Name == BelemetrySemanticConventions.ExceptionEvent)
                    {
                        if (exceptions && spanEvent.Attributes.HasException && spanEvent.Limited is false
                            && (spanEvent.LoggedAs is not { } logged || SendsLog(logged) is false))
                        {
                            Sample(configuration, AzureMonitorLiveItem.FromException(spanEvent.Attributes, null), BelemetryFeatures.Errors);
                        }
                    }
                    else if (traces && string.IsNullOrEmpty(spanEvent.Name) is false)
                    {
                        Sample(configuration, AzureMonitorLiveItem.FromSpanEvent(spanEvent), span.Feature);
                    }
                }
                break;

            case BelemetryLogRecord log when options.Signals.HasFlag(BelemetrySignals.Logs)
                                             && BelemetryExporterSet.PassesMinimumSeverity(log.EventName is not null, log.Severity, _exporter.MinimumSeverity):
                if (AzureMonitorLiveItem.TypeOf(log) is { } type && configuration.Takes(type)) Sample(configuration, AzureMonitorLiveItem.FromLog(log)!, log.Feature);
                break;
        }
    }

    /// <summary>
    /// Offers <paramref name="item"/> to the document streams of <paramref name="configuration"/>, queueing the sample they
    /// take for the next data point, and to its custom charts.
    /// </summary>
    /// <param name="configuration">The configuration applied when the record came.</param>
    /// <param name="item">The item.</param>
    /// <param name="feature">The consent feature of the record it stands for: a sample of one the user revokes is not sent.</param>
    private void Sample(AzureMonitorLiveConfiguration configuration, AzureMonitorLiveItem item, BelemetryFeatures feature)
    {
        if (Volatile.Read(ref _documentCount) < MaxDocuments && configuration.Sample(item) is { } streamIds)
        {
            // Written before it takes a place in the queue: a sample that fails to serialize holds none.
            var document = item.ToDocument(streamIds);
            lock (_documentsLock)
            {
                // Not once the configuration is replaced: the samples taken for it were discarded, and this one goes too.
                if (_configuration == configuration && Volatile.Read(ref _documentCount) < MaxDocuments)
                {
                    _documents.Enqueue((feature, document));
                    Interlocked.Increment(ref _documentCount);
                }
            }
        }

        configuration.Project(item);
    }

    /// <summary>
    /// Whether the user revoked <paramref name="feature"/> since a sample of it was taken: it is not sent. A server answers to
    /// its operator's configuration, not to a user's consent.
    /// </summary>
    private bool IsWithdrawn(BelemetryFeatures feature) => _settings.IsServer is false && _pipeline is { } pipeline && pipeline.CanRecord(feature) is false;

    /// <summary>Whether the exporter sends the log record of <paramref name="logged"/>, which then stands for the exception on the span.</summary>
    private bool SendsLog(BelemetryLoggedException logged) =>
        _exporter.Options.Signals.HasFlag(BelemetrySignals.Logs) && logged.IsDropped is false && logged.Passes(_exporter.MinimumSeverity);

    /// <summary>Whether a span failed: its status is an error, or its HTTP status code is <paramref name="threshold"/> or more.</summary>
    private static bool IsFailure(BelemetrySpan span, int threshold)
    {
        return span.Status == BelemetryStatusCode.Error
               || span.Attributes.GetInt64(BelemetrySemanticConventions.HttpResponseStatusCode) >= threshold;
    }

    /// <summary>Pings every few seconds, and posts a data point every second while someone watches, until <paramref name="stopping"/>.</summary>
    private async Task RunAsync(BelemetryPipeline pipeline, CancellationToken stopping)
    {
        await Task.Yield();
        var subscribed = false;
        var pingInterval = TimeSpan.FromSeconds(5);
        var lastPost = Stopwatch.GetTimestamp();

        while (stopping.IsCancellationRequested is false)
        {
            try
            {
                await Task.Delay(subscribed ? TimeSpan.FromSeconds(1) : pingInterval, stopping);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (_settings.IsServer is false && pipeline.CanRecord(BelemetryFeatures.Metrics) is false)
            {
                subscribed = false;
                StopSampling();
                continue;
            }

            try
            {
                using var _ = BelemetrySuppression.Begin();

                if (subscribed)
                {
                    var now = Stopwatch.GetTimestamp();
                    var elapsed = Stopwatch.GetElapsedTime(lastPost, now);
                    lastPost = now;
                    subscribed = await PostAsync(elapsed, stopping);
                }
                else
                {
                    var (isSubscribed, hint) = await PingAsync(stopping);
                    subscribed = isSubscribed;
                    if (hint is { } h) pingInterval = h;
                    if (subscribed)
                    {
                        // Discard what accumulated while nobody watched, so the first point is a real per-second rate.
                        ResetCounters();
                        lastPost = Stopwatch.GetTimestamp();
                    }
                }
            }
            catch (OperationCanceledException) when (stopping.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                subscribed = false;
                StopSampling();
                _diagnostic(ex, "Live Metrics: request failed.");
            }
        }
    }

    /// <summary>Asks the service whether someone watches, applying the configuration it answers with.</summary>
    internal async Task<(bool Subscribed, TimeSpan? PollingHint)> PingAsync(CancellationToken cancellationToken)
    {
        using var message = CreateMessage("ping", WriteBody(w => WriteDataPoint(w, metrics: null, configuration: null)));
        await _exporter.PrepareAsync(message, cancellationToken);
        using var response = await _client.SendAsync(message, cancellationToken);
        return await ReadResponseAsync(response, cancellationToken);
    }

    /// <param name="elapsed">The time since the last data point, which the rates are per second of.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    internal async Task<bool> PostAsync(TimeSpan elapsed, CancellationToken cancellationToken)
    {
        using var message = CreateMessage("post", CreatePostBody(elapsed));
        await _exporter.PrepareAsync(message, cancellationToken);
        using var response = await _client.SendAsync(message, cancellationToken);
        return (await ReadResponseAsync(response, cancellationToken)).Subscribed;
    }

    /// <summary>The body of a post: an array of one data point, with the samples and custom chart values gathered since the last one.</summary>
    internal byte[] CreatePostBody(TimeSpan elapsed)
    {
        var configuration = _configuration;
        var metrics = CreateDataPoint(elapsed);
        return WriteBody(w =>
        {
            w.WriteStartArray();
            WriteDataPoint(w, metrics, configuration);
            w.WriteEndArray();
        });
    }

    /// <summary>A ping or post request, with the stream's identity in the QuickPulse headers.</summary>
    private HttpRequestMessage CreateMessage(string operation, byte[] body)
    {
        var url = AzureMonitorConnectionString.Combine(_endpoint, $"QuickPulseService.svc/{operation}?api-version={ApiVersion}&ikey={Uri.EscapeDataString(_exporter.ConnectionString.InstrumentationKey)}");
        var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new ByteArrayContent(body) { Headers = { ContentType = new MediaTypeHeaderValue("application/json") } },
        };

        message.Headers.TryAddWithoutValidation("x-ms-qps-transmission-time", DateTimeOffset.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture));
        message.Headers.TryAddWithoutValidation("x-ms-qps-machine-name", MachineName);
        message.Headers.TryAddWithoutValidation("x-ms-qps-instance-name", _settings.ServiceInstanceId);
        message.Headers.TryAddWithoutValidation("x-ms-qps-stream-id", _streamId);
        message.Headers.TryAddWithoutValidation("x-ms-qps-role-name", _settings.ServiceName);
        message.Headers.TryAddWithoutValidation("x-ms-qps-invariant-version", InvariantVersion.ToString(CultureInfo.InvariantCulture));
        if (_etag is not null) message.Headers.TryAddWithoutValidation("x-ms-qps-configuration-etag", _etag);
        return message;
    }

    /// <summary>Reads whether someone watches, the polling hint, a redirect, and a changed configuration off a response.</summary>
    private async Task<(bool Subscribed, TimeSpan? PollingHint)> ReadResponseAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var subscribed = Header(response, "x-ms-qps-subscribed") is { } s && bool.TryParse(s, out var b) && b;

        var pollingHint = Header(response, "x-ms-qps-service-polling-interval-hint") is { } h
                          && long.TryParse(h, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms) && ms > 0
            ? TimeSpan.FromMilliseconds(ms)
            : (TimeSpan?)null;

        if (Header(response, "x-ms-qps-service-endpoint-redirect-v2") is { } redirect && Uri.TryCreate(redirect, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
        {
            _endpoint = uri;
        }

        if (subscribed is false)
        {
            StopSampling();
            return (false, pollingHint);
        }

        // The body holds the portal's configuration; the etag says whether it changed since the one applied.
        if (response.IsSuccessStatusCode && Header(response, "x-ms-qps-configuration-etag") is { } etag && etag != _etag)
        {
            ApplyConfiguration(await response.Content.ReadAsByteArrayAsync(cancellationToken), etag);
        }

        return (true, pollingHint);
    }

    /// <summary>
    /// Applies the configuration in the body of a response whose etag is <paramref name="etag"/>. Without a readable one,
    /// the one applied stays, and so does its etag - the service sends the new one again.
    /// </summary>
    internal void ApplyConfiguration(byte[] body, string etag)
    {
        if (body.Length == 0) return;

        AzureMonitorLiveConfiguration configuration;
        try
        {
            configuration = AzureMonitorLiveConfiguration.Parse(body, etag, _time);
        }
        catch (JsonException ex)
        {
            if (_unreadableEtag != etag)
            {
                _unreadableEtag = etag;
                _diagnostic(ex, "Live Metrics: the collection configuration the service sent is unreadable; the one applied before, if any, stays until it sends a readable one.");
            }
            return;
        }

        lock (_documentsLock)
        {
            // The samples waiting were taken for the streams of the configuration it replaces.
            DiscardDocuments();
            _configuration = configuration;
            _etag = etag;
        }
    }

    /// <summary>Nobody watches anymore: nothing is sampled until someone does, under the configuration the service sends then.</summary>
    private void StopSampling()
    {
        lock (_documentsLock)
        {
            _configuration = null;
            _etag = null;
            DiscardDocuments();
        }
    }

    /// <summary>Drops the samples waiting for the next data point.</summary>
    private void DiscardDocuments()
    {
        while (_documents.TryDequeue(out _)) Interlocked.Decrement(ref _documentCount);
    }

    /// <summary>The first value of a response header; null without one.</summary>
    private static string? Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }

    /// <summary>The data point's built-in metrics, per second of <paramref name="elapsed"/>; the counters start over.</summary>
    private (string Name, double Value)[] CreateDataPoint(TimeSpan elapsed)
    {
        var seconds = Math.Max(elapsed.TotalSeconds, 0.001);

        var requests = Interlocked.Exchange(ref _requests, 0);
        var requestsFailed = Interlocked.Exchange(ref _requestsFailed, 0);
        var requestTicks = Interlocked.Exchange(ref _requestTicks, 0);
        var dependencies = Interlocked.Exchange(ref _dependencies, 0);
        var dependenciesFailed = Interlocked.Exchange(ref _dependenciesFailed, 0);
        var dependencyTicks = Interlocked.Exchange(ref _dependencyTicks, 0);
        var exceptions = Interlocked.Exchange(ref _exceptions, 0);

        return
        [
            (@"\ApplicationInsights\Requests/Sec", requests / seconds),
            (@"\ApplicationInsights\Request Duration", requests == 0 ? 0 : TimeSpan.FromTicks(requestTicks / requests).TotalMilliseconds),
            (@"\ApplicationInsights\Requests Failed/Sec", requestsFailed / seconds),
            (@"\ApplicationInsights\Requests Succeeded/Sec", (requests - requestsFailed) / seconds),
            (@"\ApplicationInsights\Dependency Calls/Sec", dependencies / seconds),
            (@"\ApplicationInsights\Dependency Call Duration", dependencies == 0 ? 0 : TimeSpan.FromTicks(dependencyTicks / dependencies).TotalMilliseconds),
            (@"\ApplicationInsights\Dependency Calls Failed/Sec", dependenciesFailed / seconds),
            (@"\ApplicationInsights\Dependency Calls Succeeded/Sec", (dependencies - dependenciesFailed) / seconds),
            (@"\ApplicationInsights\Exceptions/Sec", exceptions / seconds),
            (@"\Memory\Committed Bytes", Environment.WorkingSet),
            (@"\Processor(_Total)\% Processor Time", CpuPercent()),
        ];
    }

    /// <summary>The process's share of all processors since the last call, in percent; 0 where process times are unavailable.</summary>
    private double CpuPercent()
    {
        if (OperatingSystem.IsBrowser()) return 0;

        try
        {
            var cpu = Process.GetCurrentProcess().TotalProcessorTime;
            var now = Stopwatch.GetTimestamp();
            var result = 0d;

            if (_lastCpuTimestamp != 0)
            {
                var wall = Stopwatch.GetElapsedTime(_lastCpuTimestamp, now).TotalMilliseconds;
                if (wall > 0) result = Math.Clamp((cpu - _lastCpu).TotalMilliseconds / (wall * Environment.ProcessorCount) * 100, 0, 100);
            }

            _lastCpu = cpu;
            _lastCpuTimestamp = now;
            return result;
        }
        catch
        {
            // Some sandboxes (iOS, Android) do not expose process times.
            return 0;
        }
    }

    /// <summary>Starts the counters over.</summary>
    private void ResetCounters()
    {
        Interlocked.Exchange(ref _requests, 0);
        Interlocked.Exchange(ref _requestsFailed, 0);
        Interlocked.Exchange(ref _requestTicks, 0);
        Interlocked.Exchange(ref _dependencies, 0);
        Interlocked.Exchange(ref _dependenciesFailed, 0);
        Interlocked.Exchange(ref _dependencyTicks, 0);
        Interlocked.Exchange(ref _exceptions, 0);
    }

    /// <param name="w">The writer.</param>
    /// <param name="metrics">The data point's metrics; null for a ping, which carries none of its arrays.</param>
    /// <param name="configuration">The configuration whose samples, custom charts and errors the data point carries.</param>
    private void WriteDataPoint(Utf8JsonWriter w, IReadOnlyList<(string Name, double Value)>? metrics, AzureMonitorLiveConfiguration? configuration)
    {
        w.WriteStartObject();
        w.WriteString("Version", BelemetrySettings.SdkVersion);
        w.WriteNumber("InvariantVersion", InvariantVersion);
        w.WriteString("Instance", _settings.ServiceInstanceId);
        w.WriteString("RoleName", _settings.ServiceName);
        w.WriteString("MachineName", MachineName);
        w.WriteString("StreamId", _streamId);
        w.WriteString("Timestamp", DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        w.WriteBoolean("IsWebApp", _settings.IsServer);
        w.WriteBoolean("PerformanceCollectionSupported", true);

        if (metrics is not null)
        {
            w.WriteStartArray("Metrics");
            foreach (var (name, value) in metrics)
            {
                w.WriteStartObject();
                w.WriteString("Name", name);
                w.WriteNumber("Value", double.IsFinite(value) ? value : 0);
                w.WriteNumber("Weight", 1);
                w.WriteEndObject();
            }
            configuration?.WriteMetrics(w);
            w.WriteEndArray();

            w.WriteStartArray("Documents");
            while (_documents.TryDequeue(out var document))
            {
                Interlocked.Decrement(ref _documentCount);
                if (IsWithdrawn(document.Feature) is false) w.WriteRawValue(document.Document, skipInputValidation: true);
            }
            w.WriteEndArray();

            w.WriteStartArray("TopCpuProcesses");
            w.WriteEndArray();

            w.WriteStartArray("CollectionConfigurationErrors");
            configuration?.WriteErrors(w);
            w.WriteEndArray();
        }

        w.WriteEndObject();
    }

    /// <summary>The bytes <paramref name="write"/> writes.</summary>
    private static byte[] WriteBody(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>(1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            write(writer);
        }
        return buffer.WrittenSpan.ToArray();
    }

    private static string MachineName
    {
        get
        {
            try
            {
                return Environment.MachineName;
            }
            catch
            {
                return "unknown";
            }
        }
    }

    /// <summary>Waits for the loop to stop (its token is the caller's), then releases the HTTP client.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_loop is not null)
        {
            try
            {
                await _loop;
            }
            catch
            {
                // Stopping.
            }
        }
        _client.Dispose();
    }
}
