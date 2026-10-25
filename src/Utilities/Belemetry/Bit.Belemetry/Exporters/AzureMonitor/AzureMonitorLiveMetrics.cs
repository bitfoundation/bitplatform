using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Bit.Belemetry;

/// <summary>
/// Streams Application Insights Live Metrics (the QuickPulse protocol): it pings the live endpoint every few seconds
/// and, while someone has the Live Metrics blade open (the service answers <c>x-ms-qps-subscribed: true</c>), posts
/// one data point per second with request, dependency and exception rates plus process CPU and memory.
/// </summary>
/// <remarks>Not available in the browser: the live endpoint does not allow cross-origin calls.</remarks>
internal sealed class AzureMonitorLiveMetrics : IAsyncDisposable
{
    private const int InvariantVersion = 5;

    private readonly BelemetryAzureMonitorExporter _exporter;
    private readonly BelemetrySettings _settings;
    private readonly HttpClient _client = new() { Timeout = TimeSpan.FromSeconds(10) };
    private readonly string _streamId = Guid.NewGuid().ToString("N");
    private readonly Action<Exception?, string> _diagnostic;
    private Uri _endpoint;
    private string? _etag;
    private Task? _loop;

    private long _requests, _requestsFailed, _requestTicks;
    private long _dependencies, _dependenciesFailed, _dependencyTicks;
    private long _exceptions;
    private TimeSpan _lastCpu;
    private long _lastCpuTimestamp;

    public AzureMonitorLiveMetrics(BelemetryAzureMonitorExporter exporter, BelemetrySettings settings, Action<Exception?, string> diagnostic)
    {
        _exporter = exporter;
        _settings = settings;
        _diagnostic = diagnostic;
        _endpoint = exporter.ConnectionString.LiveEndpoint;
    }

    public void Start(BelemetryPipeline pipeline, CancellationToken stopping)
    {
        pipeline.RecordAccepted += OnRecord;
        _loop = RunAsync(stopping);
    }

    private void OnRecord(object record)
    {
        switch (record)
        {
            case BelemetrySpan span when span.Kind is BelemetrySpanKind.Server or BelemetrySpanKind.Consumer:
                Interlocked.Increment(ref _requests);
                Interlocked.Add(ref _requestTicks, span.Duration.Ticks);
                if (IsFailure(span, 500)) Interlocked.Increment(ref _requestsFailed);
                break;
            case BelemetrySpan span:
                Interlocked.Increment(ref _dependencies);
                Interlocked.Add(ref _dependencyTicks, span.Duration.Ticks);
                if (IsFailure(span, 400)) Interlocked.Increment(ref _dependenciesFailed);
                break;
            case BelemetryLogRecord log when log.HasException:
                Interlocked.Increment(ref _exceptions);
                break;
        }
    }

    private static bool IsFailure(BelemetrySpan span, int threshold)
    {
        return span.Status == BelemetryStatusCode.Error
               || span.Attributes.GetInt64(BelemetrySemanticConventions.HttpResponseStatusCode) >= threshold;
    }

    private async Task RunAsync(CancellationToken stopping)
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

            try
            {
                using var _ = BelemetrySuppression.Begin();

                if (subscribed)
                {
                    var now = Stopwatch.GetTimestamp();
                    var elapsed = Stopwatch.GetElapsedTime(lastPost, now);
                    lastPost = now;
                    subscribed = await SendAsync("post", [CreateDataPoint(elapsed)], stopping);
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
                _diagnostic(ex, "Live Metrics: request failed.");
            }
        }
    }

    private async Task<(bool Subscribed, TimeSpan? PollingHint)> PingAsync(CancellationToken cancellationToken)
    {
        using var message = CreateMessage("ping", WriteBody(w => WriteDataPoint(w, metrics: null)));
        await _exporter.PrepareAsync(message, cancellationToken);
        using var response = await _client.SendAsync(message, cancellationToken);
        ReadControlHeaders(response, out var subscribed, out var hint);
        return (subscribed, hint);
    }

    private async Task<bool> SendAsync(string operation, IReadOnlyList<(string Name, double Value)>[] points, CancellationToken cancellationToken)
    {
        var body = WriteBody(w =>
        {
            w.WriteStartArray();
            foreach (var metrics in points) WriteDataPoint(w, metrics);
            w.WriteEndArray();
        });

        using var message = CreateMessage(operation, body);
        await _exporter.PrepareAsync(message, cancellationToken);
        using var response = await _client.SendAsync(message, cancellationToken);
        ReadControlHeaders(response, out var subscribed, out _);
        return subscribed;
    }

    private HttpRequestMessage CreateMessage(string operation, byte[] body)
    {
        var url = AzureMonitorConnectionString.Combine(_endpoint, $"QuickPulseService.svc/{operation}?ikey={Uri.EscapeDataString(_exporter.ConnectionString.InstrumentationKey)}");
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

    private void ReadControlHeaders(HttpResponseMessage response, out bool subscribed, out TimeSpan? pollingHint)
    {
        subscribed = Header(response, "x-ms-qps-subscribed") is { } s && bool.TryParse(s, out var b) && b;

        pollingHint = Header(response, "x-ms-qps-service-polling-interval-hint") is { } h
                      && long.TryParse(h, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ms) && ms > 0
            ? TimeSpan.FromMilliseconds(ms)
            : null;

        if (Header(response, "x-ms-qps-service-endpoint-redirect-v2") is { } redirect && Uri.TryCreate(redirect, UriKind.Absolute, out var uri))
        {
            _endpoint = uri;
        }

        _etag = Header(response, "x-ms-qps-configuration-etag") ?? _etag;
    }

    private static string? Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }

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

    private void WriteDataPoint(Utf8JsonWriter w, IReadOnlyList<(string Name, double Value)>? metrics)
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
            w.WriteEndArray();
            w.WriteStartArray("Documents");
            w.WriteEndArray();
            w.WriteStartArray("TopCpuProcesses");
            w.WriteEndArray();
            w.WriteStartArray("CollectionConfigurationErrors");
            w.WriteEndArray();
        }

        w.WriteEndObject();
    }

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
