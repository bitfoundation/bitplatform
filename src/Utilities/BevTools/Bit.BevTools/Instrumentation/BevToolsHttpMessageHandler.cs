using System.Diagnostics;
using Microsoft.Extensions.Http;

namespace Bit.BevTools;

/// <summary>
/// Records HTTP requests (method, URL, status, duration, sizes, headers with redaction; never bodies) into the
/// session that issued them. Can also be added manually to a hand-built <c>HttpClient</c>.
/// </summary>
public sealed class BevToolsHttpMessageHandler : DelegatingHandler
{
    private static readonly HashSet<string> AlwaysRedactedHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie", "X-Api-Key", "Api-Key", "X-Auth-Token", "X-CSRF-Token", "X-XSRF-Token",
    };

    private readonly BevToolsOptions _options;
    private readonly BevToolsSession? _explicitSession;
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _next;

    public BevToolsHttpMessageHandler(BevToolsOptions options)
    {
        _options = options;
        _next = base.SendAsync;
    }

    /// <summary>Creates a handler bound to a specific session (use in WebAssembly for hand-built clients).</summary>
    public BevToolsHttpMessageHandler(BevToolsSession session) : this(session.Options)
    {
        _explicitSession = session;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _explicitSession ?? ResolveSession(request);
        return session is null || !session.IsEnabled
            ? base.SendAsync(request, cancellationToken)
            : RecordAsync(session, _options, request, _next, cancellationToken);
    }

    /// <summary>The session an outer handler noted on the request, or the one the current context belongs to.</summary>
    internal static BevToolsSession? ResolveSession(HttpRequestMessage request) =>
        (request.Options.TryGetValue(BevToolsHttpSessionHandler.SessionKey, out var captured) && !captured.IsDisposed ? captured : null)
        ?? BevToolsSessionResolver.Resolve();

    internal static async Task<HttpResponseMessage> RecordAsync(BevToolsSession session, BevToolsOptions options, HttpRequestMessage request, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next, CancellationToken cancellationToken)
    {
        var trigger = BevToolsActivityObserver.CurrentTriggerEvent(session);
        if (!session.IsCapturing)
        {
            // Paused: the network log and the timeline stay frozen, but a failed request still reaches the error
            // list, which pausing promises to keep collecting. Nothing is read from the request unless it failed.
            HttpResponseMessage pausedResponse;
            try
            {
                pausedResponse = await next(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RecordException(session, ex, trigger?.Id, cancellationToken.IsCancellationRequested);
                throw;
            }

            var status = (int)pausedResponse.StatusCode;
            if (status >= 400)
            {
                RecordStatusError(session, request.Method.Method, session.Redactor.RedactUrl(request.RequestUri?.ToString() ?? ""), status, pausedResponse.ReasonPhrase, trigger?.Id);
            }

            return pausedResponse;
        }

        var url = session.Redactor.RedactUrl(request.RequestUri?.ToString() ?? "");
        // Noted by the outer handler of a factory pipeline; else (a hand-built client) read now, while the code that sent
        // the request is still on the stack - the awaits below would lose it.
        var caller = !options.CaptureHttpCallers ? default
            : request.Options.TryGetValue(BevToolsHttpSessionHandler.CallerKey, out var noted) ? noted
            : BevToolsCallSites.Find(skipFrames: 0);
        var record = new BevToolsHttpRecord
        {
            Id = session.Http.NextId(),
            StartedAt = DateTimeOffset.UtcNow,
            Method = request.Method.Method,
            Url = url,
            RequestBytes = request.Content?.Headers.ContentLength,
            RequestHeaders = options.CaptureHttpHeaders ? CollectHeaders(session, request.Headers, request.Content?.Headers) : null,
            TriggerEventId = trigger?.Id,
            TriggerDescription = trigger?.Title,
            CallerComponent = caller.Component,
            CallerVia = caller.Via,
            CallerMissing = caller.Missing,
        };
        record.TimelineEventId = session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Http,
            Category = "http",
            Title = record.Method + " " + StripOrigin(url),
            Detail = "pending",
            ParentEventId = trigger?.Id,
            Data = record.Caller is { } sender
                ? new Dictionary<string, object?> { ["requestId"] = record.Id, ["url"] = url, ["caller"] = sender }
                : new Dictionary<string, object?> { ["requestId"] = record.Id, ["url"] = url },
        });
        session.Http.Add(record);

        var bodies = BevToolsHttpBodies.Allowed(session);
        if (bodies)
        {
            (record.RequestBody, record.RequestBodyNote) = await BevToolsHttpBodies.ReadRequestAsync(session, request.Content).ConfigureAwait(false);
            record.GraphQlOperation = BevToolsHttpBodies.GraphQlOperation(record.RequestBody);
        }

        var start = Stopwatch.GetTimestamp();
        try
        {
            var response = await next(request, cancellationToken).ConfigureAwait(false);
            record.DurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            ReadTraceParent(request, record);
            record.Protocol = BevToolsHttpBodies.Protocol(request, response);
            if (bodies)
            {
                (record.ResponseBody, record.ResponseBodyNote) = await BevToolsHttpBodies.ReadResponseAsync(session, response, cancellationToken).ConfigureAwait(false);
            }

            record.StatusCode = (int)response.StatusCode;
            record.ReasonPhrase = response.ReasonPhrase;
            record.ResponseBytes = response.Content?.Headers.ContentLength;
            record.ContentType = response.Content?.Headers.ContentType?.ToString();
            record.ResponseHeaders = options.CaptureHttpHeaders ? CollectHeaders(session, response.Headers, response.Content?.Headers) : null;
            var failed = record.StatusCode >= 400;
            if (failed)
            {
                session.Http.MarkFailed(record);
            }

            var severity = failed ? BevToolsSeverity.Error : record.DurationMs >= options.Diagnostics.SlowHttpMs ? BevToolsSeverity.Warning : BevToolsSeverity.Info;
            session.Timeline.Complete(record.TimelineEventId, record.DurationMs.Value, $"{record.StatusCode} {record.ReasonPhrase}" + (record.ResponseBytes is { } b ? $", {FormatBytes(b)}" : ""), severity);
            if (failed)
            {
                RecordStatusError(session, record.Method, url, record.StatusCode.Value, record.ReasonPhrase, trigger?.Id);
            }

            session.Http.Touch();
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            record.DurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            ReadTraceParent(request, record);
            record.IsCancelled = true;
            // BevTools is the innermost handler, so this is one attempt: a resilience pipeline's attempt timeout or a
            // hedged attempt cancels it the same way the caller does.
            record.Error = "Cancelled: the request's token was cancelled (by the caller, HttpClient.Timeout, or a handler in the pipeline: a resilience attempt timeout, hedging)";
            session.Timeline.Complete(record.TimelineEventId, record.DurationMs.Value, $"cancelled after {record.DurationMs.Value:0} ms (by the caller, HttpClient.Timeout or a handler in the pipeline)", BevToolsSeverity.Warning);
            session.Http.Touch();
            throw;
        }
        catch (Exception ex)
        {
            record.DurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            ReadTraceParent(request, record);
            // A connect timeout is a TaskCanceledException whose inner TimeoutException says what happened.
            record.Error = BevToolsRedactor.RedactText(ex.GetType().Name + ": " + ex.Message + (ex.InnerException is { } inner && inner.Message != ex.Message ? " (" + inner.GetType().Name + ": " + inner.Message + ")" : ""));
            session.Http.MarkFailed(record);
            session.Timeline.Complete(record.TimelineEventId, record.DurationMs.Value, record.Error, BevToolsSeverity.Error);
            RecordException(session, ex, trigger?.Id, tokenCancelled: false);
            session.Http.Touch();
            throw;
        }
    }

    /// <summary>
    /// The trace the request was sent in: <c>traceparent</c> ("00-{trace id}-{parent id}-{flags}") as it left, which the
    /// client's diagnostics handler writes into the request below BevTools. Only a well-formed header counts.
    /// </summary>
    internal static void ReadTraceParent(HttpRequestMessage request, BevToolsHttpRecord record)
    {
        try
        {
            if (!request.Headers.TryGetValues("traceparent", out var values) || values.FirstOrDefault() is not { Length: 55 } header
                || header[2] != '-' || header[35] != '-' || header[52] != '-')
            {
                return;
            }

            var traceId = header.Substring(3, 32);
            var spanId = header.Substring(36, 16);
            // W3C: lowercase hex throughout, and neither id all zeros.
            if (traceId.All(char.IsAsciiHexDigitLower) && spanId.All(char.IsAsciiHexDigitLower) && traceId.Any(c => c != '0') && spanId.Any(c => c != '0')
                && char.IsAsciiHexDigitLower(header[53]) && char.IsAsciiHexDigitLower(header[54]))
            {
                record.TraceId = traceId;
                record.SpanId = spanId;
            }
        }
        catch
        {
            // Headers a handler disposed with the request: no trace id rather than a failure.
        }
    }

    private static void RecordStatusError(BevToolsSession session, string method, string redactedUrl, int status, string? reasonPhrase, long? triggerId) =>
        session.Errors.Record(null, "http", $"{method} {StripOrigin(redactedUrl)} returned {status} {reasonPhrase}", precedingEventId: triggerId, exceptionType: "HttpStatus" + status);

    /// <summary>
    /// A request cancelled through its own token is the caller's decision, not an error. Any other cancellation - a
    /// connect timeout, a handler giving up - is a failure like any exception.
    /// </summary>
    private static void RecordException(BevToolsSession session, Exception exception, long? triggerId, bool tokenCancelled)
    {
        if (exception is not OperationCanceledException || !tokenCancelled)
        {
            session.Errors.Record(exception, "http", precedingEventId: triggerId);
        }
    }

    private static List<KeyValuePair<string, string>> CollectHeaders(BevToolsSession session, System.Net.Http.Headers.HttpHeaders headers, System.Net.Http.Headers.HttpHeaders? contentHeaders)
    {
        var list = new List<KeyValuePair<string, string>>();
        Add(headers);
        if (contentHeaders is not null)
        {
            Add(contentHeaders);
        }

        return list;

        void Add(System.Net.Http.Headers.HttpHeaders source)
        {
            foreach (var header in source)
            {
                // Under ValueAccess None the names alone say what was sent (an Authorization header, a custom tenant
                // header), and no value is kept; otherwise a value is kept unless its name or its shape is a secret.
                var value = AlwaysRedactedHeaders.Contains(header.Key) || session.Redactor.IsSensitiveName(header.Key)
                    ? BevToolsRedactor.RedactedValue
                    : session.ValueAccess == BevToolsValueAccess.None
                        ? BevToolsRedactor.HiddenValue
                        : BevToolsRedactor.RedactText(string.Join(", ", header.Value));
                list.Add(new KeyValuePair<string, string>(header.Key, value));
            }
        }
    }

    internal static string StripOrigin(string url)
    {
        // Manual strip instead of Uri.PathAndQuery so redaction markers are not percent-encoded.
        var schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
        {
            return url;
        }

        var pathStart = url.IndexOf('/', schemeEnd + 3);
        if (pathStart >= 0)
        {
            return url[pathStart..];
        }

        // No path: "https://host?q=1" is "/?q=1", not "/".
        var rest = url.IndexOfAny(['?', '#'], schemeEnd + 3);
        return rest < 0 ? "/" : "/" + url[rest..];
    }

    internal static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => bytes + " B",
        < 1024 * 1024 => (bytes / 1024.0).ToString("0.#") + " KB",
        _ => (bytes / 1024.0 / 1024.0).ToString("0.##") + " MB",
    };
}
