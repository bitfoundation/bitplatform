using System.Text.Json;
using Bit.Belemetry.Demo.Client.Services;
using Bit.Belemetry.Demo.Server.Services;

namespace Bit.Belemetry.Demo.Server.Endpoints;

/// <summary>
/// A minimal, in-process OTLP/HTTP receiver - the destination the demo exports to, so readers can watch
/// their own telemetry arrive on the Live telemetry page.
/// </summary>
/// <remarks>
/// <para>
/// <c>POST /otlp/v1/traces</c>, <c>/otlp/v1/logs</c> and <c>/otlp/v1/metrics</c> accept OTLP/JSON
/// (<c>application/json</c>, optionally gzip-encoded) and answer like a collector: <c>200</c> with an empty
/// <c>Export*ServiceResponse</c>. Protobuf is answered with <c>415</c> - the demo exports JSON
/// (<c>Protocol = HttpJson</c>), which keeps every payload readable in the browser's network tab.
/// </para>
/// <para>
/// <c>GET /api/telemetry?sessionId=...&amp;after=...</c> hands a session its own records back.
/// </para>
/// </remarks>
public static class OtlpReceiverEndpoints
{
    /// <summary>The largest request body accepted; the browser script batches at most 100 records.</summary>
    public const long MaxBodyBytes = 2 * 1024 * 1024;

    public static IEndpointRouteBuilder MapOtlpReceiver(this IEndpointRouteBuilder endpoints)
    {
        var otlp = endpoints.MapGroup("/otlp/v1").DisableAntiforgery();

        otlp.MapPost("/traces", (HttpRequest request, TelemetryStore store) => ReceiveAsync(request, store, OtlpJsonDecoder.DecodeTraces, "ExportTraceServiceResponse"));
        otlp.MapPost("/logs", (HttpRequest request, TelemetryStore store) => ReceiveAsync(request, store, OtlpJsonDecoder.DecodeLogs, "ExportLogsServiceResponse"));
        otlp.MapPost("/metrics", (HttpRequest request, TelemetryStore store) => ReceiveAsync(request, store, OtlpJsonDecoder.DecodeMetrics, "ExportMetricsServiceResponse"));

        endpoints.MapGet("/api/telemetry", (string? sessionId, long? after, bool? shared, TelemetryStore store) =>
        {
            if (string.IsNullOrWhiteSpace(sessionId) || TelemetryStore.IsValidSessionId(sessionId) is false)
            {
                return Results.BadRequest(new { error = "Pass the browser session id: BitBelemetry.getContext().sessionId." });
            }

            return Results.Ok(store.Get(sessionId, after ?? 0, shared ?? false));
        });

        // A same-origin endpoint for the browser page's fetch and XHR buttons. It answers with the
        // traceparent it received, so the page can show that the browser span and this request's
        // server span share one trace id. ?delay=ms (at most 3 s) makes it slow enough for the page to
        // abort the request before the answer arrives.
        endpoints.MapGet("/api/demo/echo", async (HttpRequest request, ILoggerFactory loggers, int? delay, CancellationToken cancellationToken) =>
        {
            var traceparent = request.Headers.TryGetValue("traceparent", out var value) ? value.ToString() : null;
            loggers.CreateLogger("Bit.Belemetry.Demo.Server.Echo").LogInformation("Echo called with traceparent {Traceparent}", traceparent ?? "(none)");

            if (delay is > 0)
            {
                try { await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(delay.Value, 3000)), cancellationToken); }
                catch (OperationCanceledException) { return Results.Empty; }
            }

            return Results.Ok(new { traceparent, serverTraceId = System.Diagnostics.Activity.Current?.TraceId.ToString(), time = DateTimeOffset.UtcNow });
        });

        endpoints.MapGet("/api/demo/fail", () => Results.Problem("A deliberate 500 from the demo server, to show a failed dependency span.", statusCode: 500));

        return endpoints;
    }

    private static async Task<IResult> ReceiveAsync(HttpRequest request, TelemetryStore store, Func<JsonElement, List<TelemetryRecord>> decode, string responseType)
    {
        var contentType = request.ContentType ?? string.Empty;

        if (contentType.StartsWith("application/x-protobuf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Text("This demo receiver accepts OTLP/JSON only - set Protocol = BelemetryOtlpProtocol.HttpJson.",
                                statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        if (contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) is false)
        {
            return Results.Text("Expected Content-Type: application/json.", statusCode: StatusCodes.Status415UnsupportedMediaType);
        }

        if (request.ContentLength > MaxBodyBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        try
        {
            var body = request.Body;
            if (string.Equals(request.Headers.ContentEncoding, "gzip", StringComparison.OrdinalIgnoreCase))
            {
                body = new System.IO.Compression.GZipStream(body, System.IO.Compression.CompressionMode.Decompress);
            }

            await using var bounded = new BoundedStream(body, MaxBodyBytes);
            using var document = await JsonDocument.ParseAsync(bounded, cancellationToken: request.HttpContext.RequestAborted);

            store.Add(decode(document.RootElement));
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "The body is not valid OTLP/JSON." });
        }
        catch (InvalidDataException)
        {
            return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        // An empty Export*ServiceResponse: a full success, as a collector answers.
        return Results.Json(new Dictionary<string, object>(), statusCode: StatusCodes.Status200OK, contentType: "application/json");
    }

    /// <summary>Stops reading past a limit, so a gzip bomb cannot inflate past <see cref="MaxBodyBytes"/>.</summary>
    private sealed class BoundedStream(Stream inner, long limit) : Stream
    {
        private long _read;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => _read; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count) => Count(inner.Read(buffer, offset, count));

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => Count(await inner.ReadAsync(buffer, cancellationToken));

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        private int Count(int read)
        {
            _read += read;
            if (_read > limit) throw new InvalidDataException("Body too large.");
            return read;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
