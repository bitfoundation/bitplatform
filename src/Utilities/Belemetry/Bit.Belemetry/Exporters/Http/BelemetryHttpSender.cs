using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Bit.Belemetry;

/// <summary>
/// Delivers <see cref="BelemetryHttpRequest"/>s: compression, retries with backoff (honoring <c>Retry-After</c>),
/// partial-success handling, gRPC framing and status, and handing undeliverable requests to the persistent store.
/// </summary>
internal sealed class BelemetryHttpSender : IDisposable
{
    // What the Azure Monitor ingestion endpoint asks to retry, its 439 (daily quota) and 500 included.
    private static readonly HashSet<int> _retryable = [408, 429, 439, 500, 502, 503, 504];

    // The OTLP specification's list, which is shorter: "All other 4xx or 5xx response status codes MUST NOT be retried"
    // - a collector answering 500 or 408 has said its last word on that payload.
    private static readonly HashSet<int> _retryableOtlp = [429, 502, 503, 504];

    // The gRPC status codes the OTLP specification lists as retryable: CANCELLED, DEADLINE_EXCEEDED, RESOURCE_EXHAUSTED,
    // ABORTED, OUT_OF_RANGE, UNAVAILABLE and DATA_LOSS. Anything else is the server's final word.
    private static readonly HashSet<int> _retryableGrpc = [1, 4, 8, 10, 11, 14, 15];
    private static readonly TimeSpan _maxRetryAfter = TimeSpan.FromSeconds(30);

    internal static readonly string UserAgent = "Bit.Belemetry/" + BelemetrySettings.SdkVersion;

    private readonly HttpClient _client;
    private readonly bool _ownsClient;
    private readonly Action<Exception?, string> _diagnostic;
    private readonly Action<Exception?, string> _warning;
    private HttpClient? _grpcClient;

    /// <param name="handler">The handler to send through; the platform default when null.</param>
    /// <param name="diagnostic">Logs a diagnostic (Debug, or Information with <see cref="BelemetryOptions.Debug"/>).</param>
    /// <param name="warning">Logs a warning: data the backend rejected. Defaults to <paramref name="diagnostic"/>.</param>
    public BelemetryHttpSender(HttpMessageHandler? handler, Action<Exception?, string> diagnostic, Action<Exception?, string>? warning = null)
    {
        _diagnostic = diagnostic;
        _warning = warning ?? diagnostic;
        _ownsClient = true;
        // A dedicated client: the app's HttpClient may carry a BaseAddress, auth handlers or its own telemetry, none of
        // which belongs on export traffic. Per-request timeouts are applied with a CancellationTokenSource instead.
        _client = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        // A provided handler (tests, an app's own) serves gRPC too; otherwise gRPC gets its own client, see Client().
        if (handler is not null) _grpcClient = _client;
    }

    /// <summary>
    /// The client for a request. gRPC needs HTTP/2, with prior knowledge over plain http: the platform's default
    /// handler on Android and iOS (the native one) cannot do that and would fail every attempt, so gRPC goes through
    /// a SocketsHttpHandler of its own, created on first use. The browser never gets here with gRPC.
    /// </summary>
    private HttpClient Client(bool grpc)
    {
        if (grpc is false) return _client;
        if (_grpcClient is null)
        {
            var client = OperatingSystem.IsBrowser() ? new HttpClient() : new HttpClient(new SocketsHttpHandler(), disposeHandler: true);
            client.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
            _grpcClient = Interlocked.CompareExchange(ref _grpcClient, client, null) ?? client;
            if (_grpcClient != client) client.Dispose();
        }
        return _grpcClient;
    }

    /// <summary>The waits between in-process attempts when the server gives no <c>Retry-After</c> (settable for tests).</summary>
    internal TimeSpan[] Backoff { get; set; } = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];

    /// <summary>Delivers <paramref name="request"/>, retrying transient failures in-process a couple of times.</summary>
    public async Task<BelemetrySendOutcome> SendAsync(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, bool retry, CancellationToken cancellationToken) =>
        (await SendWithRemainderAsync(exporter, request, retry, cancellationToken)).Outcome;

    /// <summary>
    /// Delivers <paramref name="request"/> and returns, with the outcome, what is still undelivered: the request itself,
    /// or after a partial success only its rejected items - which is what must be stored for a later retry, since the
    /// accepted ones would otherwise be sent twice.
    /// </summary>
    public async Task<(BelemetrySendOutcome Outcome, BelemetryHttpRequest Remaining)> SendWithRemainderAsync(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, bool retry, CancellationToken cancellationToken)
    {
        var outcome = await SendCoreAsync(exporter, request, retry, cancellationToken, remaining => request = remaining);
        return (outcome, request);
    }

    private async Task<BelemetrySendOutcome> SendCoreAsync(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, bool retry, CancellationToken cancellationToken, Action<BelemetryHttpRequest> narrowed)
    {
        var attempts = retry ? Backoff.Length + 1 : 1;
        var grpc = IsGrpc(request);

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            TimeSpan? retryAfter = null;

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(exporter.Timeout > TimeSpan.Zero ? exporter.Timeout : TimeSpan.FromSeconds(10));

                using var message = await CreateMessageAsync(exporter, request, timeout.Token);
                using var response = await Client(grpc).SendAsync(message, HttpCompletionOption.ResponseContentRead, timeout.Token);
                var status = (int)response.StatusCode;

                if (grpc && status == 200)
                {
                    // gRPC answers 200 whatever happened; the verdict is the grpc-status trailer (or header, for a
                    // trailers-only response). A missing status is a broken reply, worth one more try.
                    var grpcStatus = GetGrpcStatus(response);
                    if (grpcStatus == 0)
                    {
                        ReportPartialSuccess(exporter, request, null, Unframe(await ReadBodyAsync(response, timeout.Token)));
                        return BelemetrySendOutcome.Delivered;
                    }

                    if (grpcStatus is null || _retryableGrpc.Contains(grpcStatus.Value))
                    {
                        _diagnostic(null, $"{exporter.Name}: {request.Url} answered grpc-status {grpcStatus?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(none)"}; will retry.");
                    }
                    else
                    {
                        _warning(null, $"{exporter.Name}: {request.Url} rejected the request with grpc-status {grpcStatus} ({GetGrpcMessage(response)}); it is dropped.");
                        return BelemetrySendOutcome.Rejected;
                    }
                }
                else if (response.IsSuccessStatusCode)
                {
                    var body = status == 200 || status == 206 ? await ReadBodyAsync(response, timeout.Token) : null;
                    var mediaType = response.Content.Headers.ContentType?.MediaType;
                    var text = body is null || IsProtobuf(mediaType) ? null : Encoding.UTF8.GetString(body);
                    if (exporter.GetRetryForPartialSuccess(request, status, text) is { } partial)
                    {
                        _diagnostic(null, $"{exporter.Name}: partial success ({status}); retrying the rejected items.");

                        // Retried right away, even on what would have been the last attempt: the accepted items are in,
                        // and what is left is a smaller request that has not failed yet. Only while it shrinks, so a
                        // server that keeps rejecting the same items cannot keep it going.
                        if (attempt + 1 >= attempts && partial.Body.Length < request.Body.Length) attempts++;
                        request = partial;
                        narrowed(partial);
                        continue;
                    }
                    ReportPartialSuccess(exporter, request, mediaType, body);
                    return BelemetrySendOutcome.Delivered;
                }
                else if (exporter.IsRetryable(status) is false)
                {
                    // Data loss with a cause the app's owner can fix (a key, an endpoint, a quota): a warning.
                    _warning(null, $"{exporter.Name}: {request.Url} rejected the request with {status}; it is dropped.");
                    return BelemetrySendOutcome.Rejected;
                }
                else
                {
                    retryAfter = GetRetryAfter(response);
                    _diagnostic(null, $"{exporter.Name}: {request.Url} answered {status}; will retry.");
                }
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
                var delay = retryAfter is { } ra ? (ra > _maxRetryAfter ? _maxRetryAfter : ra)
                          : Backoff.Length == 0 ? TimeSpan.Zero : Backoff[Math.Min(attempt, Backoff.Length - 1)];
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
        var grpc = IsGrpc(request);

        if (string.Equals(encoding, "gzip", StringComparison.OrdinalIgnoreCase) && OperatingSystem.IsBrowser())
        {
            // System.IO.Compression is not available in browser WebAssembly; send it uncompressed instead.
            encoding = null;
        }
        else if (string.Equals(encoding, "gzip", StringComparison.OrdinalIgnoreCase))
        {
            body = Gzip(body);
        }

        // A gRPC message is length-prefixed: one byte saying whether it is compressed, then its big-endian length.
        // The stored request holds the bare protobuf, so a replay frames it again here.
        if (grpc) body = Frame(body, compressed: encoding is not null);

        var content = new ByteArrayContent(body);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
        if (encoding is not null && grpc is false) content.Headers.ContentEncoding.Add(encoding);

        var message = new HttpRequestMessage(HttpMethod.Post, request.Url) { Content = content };

        if (grpc)
        {
            // Unary gRPC is a plain HTTP/2 POST; the exact version keeps HttpClient from negotiating down to HTTP/1.1
            // (and lets it use HTTP/2 without TLS, which is what a local collector or the Aspire dashboard may be).
            message.Version = HttpVersion.Version20;
            message.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            message.Headers.TryAddWithoutValidation("te", "trailers");
            if (encoding is not null) message.Headers.TryAddWithoutValidation("grpc-encoding", encoding);
        }

        // The exporter's current headers, not ones stored with the request: persisted requests carry none (no secrets
        // on disk), and a replayed request must use today's API key, not the one it was created with.
        var headers = request.Headers is { Count: > 0 } ? request.WithHeaders(exporter.Headers).Headers! : exporter.Headers;
        foreach (var (name, value) in headers)
        {
            if (message.Headers.TryAddWithoutValidation(name, value) is false) content.Headers.TryAddWithoutValidation(name, value);
        }

        // Identifies the exporter to the backend, as the OTLP exporter specification asks - except in the browser, where
        // a page cannot set it, and trying would only add a header the collector's CORS policy has to allow.
        if (OperatingSystem.IsBrowser() is false && message.Headers.UserAgent.Count == 0)
        {
            message.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        }

        await exporter.PrepareAsync(message, cancellationToken);
        return message;
    }

    internal static bool IsGrpc(BelemetryHttpRequest request) =>
        string.Equals(request.ContentType, BelemetryOtlpExporter.GrpcContentType, StringComparison.OrdinalIgnoreCase);

    internal static byte[] Frame(byte[] body, bool compressed)
    {
        var framed = new byte[body.Length + 5];
        framed[0] = compressed ? (byte)1 : (byte)0;
        BinaryPrimitives.WriteUInt32BigEndian(framed.AsSpan(1, 4), (uint)body.Length);
        body.CopyTo(framed, 5);
        return framed;
    }

    private static int? GetGrpcStatus(HttpResponseMessage response)
    {
        // Trailers first: a normal reply carries the status there; a trailers-only reply puts it in the headers.
        if (TryGetHeader(response.TrailingHeaders, "grpc-status", out var trailer) && int.TryParse(trailer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromTrailer)) return fromTrailer;
        if (TryGetHeader(response.Headers, "grpc-status", out var header) && int.TryParse(header, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromHeader)) return fromHeader;
        return null;
    }

    private static string GetGrpcMessage(HttpResponseMessage response)
    {
        if (TryGetHeader(response.TrailingHeaders, "grpc-message", out var trailer)) return Uri.UnescapeDataString(trailer);
        if (TryGetHeader(response.Headers, "grpc-message", out var header)) return Uri.UnescapeDataString(header);
        return string.Empty;
    }

    private static bool TryGetHeader(HttpHeaders headers, string name, out string value)
    {
        if (headers.TryGetValues(name, out var values) && values.FirstOrDefault() is { } first)
        {
            value = first;
            return true;
        }

        value = string.Empty;
        return false;
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

    // The OTLP specification's recommended limit for a response: anything bigger is not a partial-success message.
    private const int MaxResponseBytes = 4 * 1024 * 1024;

    private static async Task<byte[]?> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            if (response.Content.Headers.ContentLength is 0 or > MaxResponseBytes) return null;
            var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return body.Length is 0 or > MaxResponseBytes ? null : body;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsProtobuf(string? mediaType) => mediaType is not null && mediaType.Contains("protobuf", StringComparison.OrdinalIgnoreCase);

    /// <summary>The message of an uncompressed gRPC frame; null for anything else.</summary>
    internal static byte[]? Unframe(byte[]? body)
    {
        if (body is null || body.Length < 5 || body[0] != 0) return null;
        var length = BinaryPrimitives.ReadUInt32BigEndian(body.AsSpan(1, 4));
        return length <= body.Length - 5 ? body.AsSpan(5, (int)length).ToArray() : null;
    }

    /// <summary>
    /// A partial success must not be retried (the server already decided), but it must not pass in silence either:
    /// the records it rejected are gone. A warning, as it is data loss the app's owner can act on.
    /// </summary>
    private void ReportPartialSuccess(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, string? mediaType, byte[]? body)
    {
        try
        {
            if (body is null || exporter.DescribePartialSuccess(mediaType, body) is not { } rejection) return;
            _warning(null, $"{exporter.Name}: {request.Url} accepted the request but {rejection}.");
        }
        catch
        {
            // Reporting must never turn a delivered request into a failed one, which would resend it.
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
        if (_grpcClient is { } grpcClient && grpcClient != _client) grpcClient.Dispose();
    }

    /// <summary>Whether the Azure Monitor ingestion endpoint asks for a retry with <paramref name="status"/>.</summary>
    internal static bool IsRetryable(HttpStatusCode status) => _retryable.Contains((int)status);

    /// <summary>Whether the OTLP specification allows retrying an OTLP/HTTP request answered with <paramref name="status"/>.</summary>
    internal static bool IsOtlpRetryable(int status) => _retryableOtlp.Contains(status);
}
