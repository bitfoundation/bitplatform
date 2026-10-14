using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;

namespace Bit.Belemetry;

/// <summary>
/// Delivers <see cref="BelemetryHttpRequest"/>s: compression, retries with backoff (honoring <c>Retry-After</c>),
/// partial-success handling, and handing undeliverable requests to the persistent store.
/// </summary>
internal sealed class BelemetryHttpSender : IDisposable
{
    // OTLP spec retryable codes, plus 408 and Azure's 439 (daily quota) / 500.
    private static readonly HashSet<int> _retryable = [408, 429, 439, 500, 502, 503, 504];
    private static readonly TimeSpan _maxRetryAfter = TimeSpan.FromSeconds(30);

    private readonly HttpClient _client;
    private readonly bool _ownsClient;
    private readonly Action<Exception?, string> _diagnostic;

    public BelemetryHttpSender(HttpMessageHandler? handler, Action<Exception?, string> diagnostic)
    {
        _diagnostic = diagnostic;
        _ownsClient = true;
        // A dedicated client: the app's HttpClient may carry a BaseAddress, auth handlers or its own telemetry, none of
        // which belongs on export traffic. Per-request timeouts are applied with a CancellationTokenSource instead.
        _client = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
    }

    /// <summary>The waits between in-process attempts when the server gives no <c>Retry-After</c> (settable for tests).</summary>
    internal TimeSpan[] Backoff { get; set; } = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];

    /// <summary>Delivers <paramref name="request"/>, retrying transient failures in-process a couple of times.</summary>
    public async Task<BelemetrySendOutcome> SendAsync(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, bool retry, CancellationToken cancellationToken)
    {
        var attempts = retry ? Backoff.Length + 1 : 1;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            TimeSpan? retryAfter = null;

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(exporter.Timeout > TimeSpan.Zero ? exporter.Timeout : TimeSpan.FromSeconds(10));

                using var message = await CreateMessageAsync(exporter, request, timeout.Token);
                using var response = await _client.SendAsync(message, HttpCompletionOption.ResponseContentRead, timeout.Token);
                var status = (int)response.StatusCode;

                if (response.IsSuccessStatusCode)
                {
                    var body = status == 200 || status == 206 ? await ReadBodyAsync(response, timeout.Token) : null;
                    if (exporter.GetRetryForPartialSuccess(request, status, body) is { } partial)
                    {
                        _diagnostic(null, $"{exporter.Name}: partial success ({status}); retrying the rejected items.");
                        request = partial;
                        continue;
                    }
                    return BelemetrySendOutcome.Delivered;
                }

                if (_retryable.Contains(status) is false)
                {
                    _diagnostic(null, $"{exporter.Name}: {request.Url} rejected the request with {status}; it is dropped.");
                    return BelemetrySendOutcome.Rejected;
                }

                retryAfter = GetRetryAfter(response);
                _diagnostic(null, $"{exporter.Name}: {request.Url} answered {status}; will retry.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return BelemetrySendOutcome.Failed;
            }
            catch (Exception ex)
            {
                // Network failure, DNS, CORS (in the browser), or timeout: all transient from our point of view.
                _diagnostic(ex, $"{exporter.Name}: sending to {request.Url} failed.");
            }

            if (attempt + 1 < attempts)
            {
                var delay = retryAfter is { } ra ? (ra > _maxRetryAfter ? _maxRetryAfter : ra) : Backoff[attempt];
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return BelemetrySendOutcome.Failed;
                }
            }
        }

        return BelemetrySendOutcome.Failed;
    }

    private static async Task<HttpRequestMessage> CreateMessageAsync(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, CancellationToken cancellationToken)
    {
        var body = request.Body;
        var encoding = request.ContentEncoding;

        if (string.Equals(encoding, "gzip", StringComparison.OrdinalIgnoreCase) && OperatingSystem.IsBrowser())
        {
            // System.IO.Compression is not available in browser WebAssembly; send it uncompressed instead.
            encoding = null;
        }
        else if (string.Equals(encoding, "gzip", StringComparison.OrdinalIgnoreCase))
        {
            body = Gzip(body);
        }

        var content = new ByteArrayContent(body);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
        if (encoding is not null) content.Headers.ContentEncoding.Add(encoding);

        var message = new HttpRequestMessage(HttpMethod.Post, request.Url) { Content = content };
        foreach (var (name, value) in request.Headers)
        {
            if (message.Headers.TryAddWithoutValidation(name, value) is false) content.Headers.TryAddWithoutValidation(name, value);
        }

        await exporter.PrepareAsync(message, cancellationToken);
        return message;
    }

    private static byte[] Gzip(byte[] body)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(body);
        }
        return output.ToArray();
    }

    private static async Task<string?> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            if (response.Content.Headers.ContentLength is 0) return null;
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            // Protobuf partial-success responses are not decoded: they only inform, they do not ask for a retry.
            if (mediaType is not null && mediaType.Contains("protobuf", StringComparison.OrdinalIgnoreCase)) return null;
            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        if (header?.Delta is { } delta) return delta;
        if (header?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }
        return null;
    }

    public void Dispose()
    {
        if (_ownsClient) _client.Dispose();
    }

    internal static bool IsRetryable(HttpStatusCode status) => _retryable.Contains((int)status);
}
