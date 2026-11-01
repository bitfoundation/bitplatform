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
        (await SendWithResultAsync(exporter, request, retry, cancellationToken)).Outcome;

    /// <summary>
    /// Delivers <paramref name="request"/>, retrying transient failures in-process a couple of times, and says what
    /// became of it: the outcome, what is still undelivered, and the records accepted and refused for good.
    /// </summary>
    public Task<BelemetrySendResult> SendWithResultAsync(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, bool retry, CancellationToken cancellationToken) =>
        SendWithResultAsync(exporter, request, retry, withdrawn: null, cancellationToken);

    /// <summary>
    /// Delivers <paramref name="request"/> as <see cref="SendWithResultAsync(IBelemetryHttpExporter, BelemetryHttpRequest, bool, CancellationToken)"/>
    /// does, and gives it up as <see cref="BelemetrySendOutcome.Failed"/>, without another attempt, once
    /// <paramref name="withdrawn"/> says it may no longer be sent (consent was revoked after it was built).
    /// </summary>
    public async Task<BelemetrySendResult> SendWithResultAsync(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, bool retry, Func<bool>? withdrawn, CancellationToken cancellationToken)
    {
        var attempts = retry ? Backoff.Length + 1 : 1;
        var extended = false;
        var grpc = IsGrpc(request);

        // What the partial successes settled before the request was narrowed to the rest.
        var delivered = 0;
        var rejected = 0;

        for (var attempt = 0; attempt < attempts; attempt++)
        {
            if (withdrawn?.Invoke() is true) break;

            TimeSpan? retryAfter = null;

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(exporter.Timeout > TimeSpan.Zero ? exporter.Timeout : TimeSpan.FromSeconds(10));

                using var message = await CreateMessageAsync(exporter, request, timeout.Token);
                // The headers only: a body is read by ReadBodyAsync, within MaxResponseBytes, instead of whole by HttpClient.
                using var response = await Client(grpc).SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                var status = (int)response.StatusCode;

                if (grpc && status == 200)
                {
                    // gRPC answers 200 whatever happened; the verdict is the grpc-status trailer (or header, for a
                    // trailers-only response) - and trailers arrive after the body. A missing status is a broken reply,
                    // worth one more try.
                    var body = await ReadBodyAsync(response, timeout.Token);
                    var grpcStatus = GetGrpcStatus(response);
                    if (grpcStatus == 0)
                    {
                        var refused = ReportPartialSuccess(exporter, request, null, Unframe(body));
                        return new(BelemetrySendOutcome.Delivered, request, delivered + Math.Max(0, request.Count - refused), rejected + refused);
                    }

                    if (grpcStatus is null || _retryableGrpc.Contains(grpcStatus.Value))
                    {
                        _diagnostic(null, $"{exporter.Name}: {request.Url} answered grpc-status {grpcStatus?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(none)"}; will retry.");
                    }
                    else
                    {
                        _warning(null, $"{exporter.Name}: {request.Url} rejected the request with grpc-status {grpcStatus} ({GetGrpcMessage(response)}); it is dropped.");
                        return new(BelemetrySendOutcome.Rejected, request, delivered, rejected + request.Count);
                    }
                }
                else if (response.IsSuccessStatusCode)
                {
                    var body = status == 200 || status == 206 ? await ReadBodyAsync(response, timeout.Token) : null;
                    var contentType = response.Content.Headers.ContentType;
                    var text = body is null || IsProtobuf(contentType?.MediaType) ? null : DecodeText(body, contentType?.CharSet);
                    var partial = exporter.GetRetryForPartialSuccess(request, status, text, out var refused);
                    refused = Math.Clamp(refused, 0, request.Count > 0 ? request.Count : int.MaxValue);
                    if (refused > 0)
                    {
                        _warning(null, $"{exporter.Name}: {request.Url} accepted the request but rejected {refused.ToString(CultureInfo.InvariantCulture)} of its items for good; they are dropped.");
                    }

                    if (partial is not null)
                    {
                        _diagnostic(null, $"{exporter.Name}: partial success ({status}); retrying the rejected items.");

                        // Retried right away, even on what would have been the last attempt: the accepted items are in,
                        // and what is left is a smaller request that has not failed yet. Only while it shrinks, so a
                        // server that keeps rejecting the same items cannot keep it going.
                        if (attempt + 1 >= attempts && partial.Body.Length < request.Body.Length && extended is false)
                        {
                            attempts++;
                            extended = true;
                        }
                        delivered += Math.Max(0, request.Count - partial.Count - refused);
                        rejected += refused;
                        request = partial;
                        continue;
                    }

                    refused += ReportPartialSuccess(exporter, request, contentType?.MediaType, body);
                    return new(BelemetrySendOutcome.Delivered, request, delivered + Math.Max(0, request.Count - refused), rejected + refused);
                }
                else if (exporter.IsRetryable(status) is false)
                {
                    // Data loss with a cause the app's owner can fix (a key, an endpoint, a quota): a warning.
                    _warning(null, $"{exporter.Name}: {request.Url} rejected the request with {status}; it is dropped.");
                    return new(BelemetrySendOutcome.Rejected, request, delivered, rejected + request.Count);
                }
                else
                {
                    retryAfter = GetRetryAfter(response);
                    _diagnostic(null, $"{exporter.Name}: {request.Url} answered {status}; will retry.");
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return new(BelemetrySendOutcome.Failed, request, delivered, rejected);
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
                    return new(BelemetrySendOutcome.Failed, request, delivered, rejected);
                }
            }
        }

        return new(BelemetrySendOutcome.Failed, request, delivered, rejected);
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

        if (OperatingSystem.IsBrowser()) MarkAsExport(message);

        await exporter.PrepareAsync(message, cancellationToken);
        return message;
    }

    // Where the WebAssembly HttpClient handler takes the options it copies into fetch's init object (Blazor's
    // SetBrowserRequestOption writes there too).
    private static readonly HttpRequestOptionsKey<IDictionary<string, object>> _fetchOptions = new("WebAssemblyFetchOptions");

    /// <summary>The fetch option that marks a .NET export in WebAssembly; the script's fetch instrumentation skips a request carrying it.</summary>
    internal const string ExportFetchOption = "bitBelemetryExport";

    /// <summary>
    /// In WebAssembly the request goes through <c>fetch</c>, past the script's fetch instrumentation, which knows the
    /// .NET exporters' URLs only from the configuration that started it - and a server's lists none, so in an Auto app
    /// the script the server started would trace every export of the WebAssembly pipeline. The runtime copies the
    /// option into fetch's init object as is: fetch ignores the key, and unlike a header it is nothing for the
    /// collector's CORS policy to allow.
    /// </summary>
    internal static void MarkAsExport(HttpRequestMessage message) =>
        message.Options.Set(_fetchOptions, new Dictionary<string, object>(StringComparer.Ordinal) { [ExportFetchOption] = true });

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
    internal const int MaxResponseBytes = 4 * 1024 * 1024;

    /// <summary>
    /// The response body, or null when there is none or it is over <see cref="MaxResponseBytes"/>. Read as it arrives
    /// and abandoned at the limit: a broken collector streaming an endless body (no Content-Length) costs that much
    /// memory at most.
    /// </summary>
    private static async Task<byte[]?> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            if (response.Content.Headers.ContentLength is 0 or > MaxResponseBytes) return null;

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var body = new MemoryStream();
            var chunk = new byte[16 * 1024];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (body.Length + read > MaxResponseBytes) return null;
                body.Write(chunk, 0, read);
            }

            return body.Length == 0 ? null : body.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The body as text, as <see cref="HttpContent.ReadAsStringAsync()"/> would give it: in the charset the response
    /// names (UTF-8 when it names none, or one this runtime lacks), and without a byte order mark, which JSON parsing
    /// fails on - a mark, when there is one, also tells the encoding.
    /// </summary>
    internal static string DecodeText(byte[] body, string? charset)
    {
        var encoding = Encoding.UTF8;
        if (string.IsNullOrWhiteSpace(charset) is false)
        {
            try
            {
                encoding = Encoding.GetEncoding(charset.Trim().Trim('"'));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
            {
                // Unknown here: UTF-8, which JSON is anyway.
            }
        }

        using var reader = new StreamReader(new MemoryStream(body), encoding, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
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
    /// the records it rejected are gone. A warning, as it is data loss the app's owner can act on. Returns how many
    /// records it rejected (0 when it does not say), which are not counted as delivered.
    /// </summary>
    private int ReportPartialSuccess(IBelemetryHttpExporter exporter, BelemetryHttpRequest request, string? mediaType, byte[]? body)
    {
        try
        {
            if (body is null || exporter.DescribePartialSuccess(mediaType, body, out var rejected) is not { } rejection) return 0;
            _warning(null, $"{exporter.Name}: {request.Url} accepted the request but {rejection}.");
            return (int)Math.Clamp(rejected, 0, request.Count > 0 ? request.Count : int.MaxValue);
        }
        catch
        {
            // Reporting must never turn a delivered request into a failed one, which would resend it.
            return 0;
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
