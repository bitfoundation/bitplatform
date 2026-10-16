using System.Diagnostics;
using Microsoft.Extensions.Http;

namespace Bit.BevTools;

/// <summary>Adds <see cref="BevToolsHttpMessageHandler"/> to every <c>IHttpClientFactory</c> pipeline.</summary>
internal sealed class BevToolsHttpFilter(BitBevToolsOptions options, BevToolsRegistry registry) : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) => builder =>
    {
        next(builder);
        if (registry.IsEnabled && options.TrackHttp)
        {
            // Innermost, i.e. closest to the primary handler: every attempt a resilience/retry handler makes is a real
            // network request and is recorded as one. Sitting outermost would hide retries behind a single record,
            // which is exactly the pattern "why is this request repeating?" needs to show.
            builder.AdditionalHandlers.Add(new BevToolsHttpMessageHandler(options));
        }
    };
}

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

    private readonly BitBevToolsOptions _options;
    private readonly BevToolsSession? _explicitSession;

    public BevToolsHttpMessageHandler(BitBevToolsOptions options)
    {
        _options = options;
    }

    /// <summary>Creates a handler bound to a specific session (use in WebAssembly for hand-built clients).</summary>
    public BevToolsHttpMessageHandler(BevToolsSession session) : this(session.Options)
    {
        _explicitSession = session;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _explicitSession ?? SessionResolver.Resolve();
        if (session is null || !session.IsEnabled)
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        var trigger = ActivityObserver.CurrentTriggerEvent(session);
        if (!session.IsCapturing)
        {
            // Paused: the network log and the timeline stay frozen, but a failed request still reaches the error
            // list, which pausing promises to keep collecting. Nothing is read from the request unless it failed.
            HttpResponseMessage pausedResponse;
            try
            {
                pausedResponse = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                RecordException(session, ex, trigger?.Id);
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
        var record = new BevHttpRecord
        {
            Id = session.Http.NextId(),
            StartedAt = DateTimeOffset.UtcNow,
            Method = request.Method.Method,
            Url = url,
            RequestBytes = request.Content?.Headers.ContentLength,
            RequestHeaders = _options.CaptureHttpHeaders ? CollectHeaders(session, request.Headers, request.Content?.Headers) : null,
            TriggerEventId = trigger?.Id,
            TriggerDescription = trigger?.Title,
        };
        record.TimelineEventId = session.Timeline.Record(new BevToolsEvent
        {
            Kind = BevToolsEventKind.Http,
            Category = "http",
            Title = record.Method + " " + StripOrigin(url),
            Detail = "pending",
            ParentEventId = trigger?.Id,
            Data = new Dictionary<string, object?> { ["requestId"] = record.Id, ["url"] = url },
        });
        session.Http.Add(record);

        var start = Stopwatch.GetTimestamp();
        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            record.DurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            record.StatusCode = (int)response.StatusCode;
            record.ReasonPhrase = response.ReasonPhrase;
            record.ResponseBytes = response.Content?.Headers.ContentLength;
            record.ContentType = response.Content?.Headers.ContentType?.ToString();
            record.ResponseHeaders = _options.CaptureHttpHeaders ? CollectHeaders(session, response.Headers, response.Content?.Headers) : null;
            var failed = record.StatusCode >= 400;
            if (failed)
            {
                session.Http.MarkFailed(record);
            }

            var severity = failed ? BevToolsSeverity.Error : record.DurationMs >= _options.Diagnostics.SlowHttpMs ? BevToolsSeverity.Warning : BevToolsSeverity.Info;
            session.Timeline.Complete(record.TimelineEventId, record.DurationMs.Value, $"{record.StatusCode} {record.ReasonPhrase}" + (record.ResponseBytes is { } b ? $", {FormatBytes(b)}" : ""), severity);
            if (failed)
            {
                RecordStatusError(session, record.Method, url, record.StatusCode.Value, record.ReasonPhrase, trigger?.Id);
            }

            session.Http.Touch();
            return response;
        }
        catch (Exception ex)
        {
            record.DurationMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            record.Error = ex.GetType().Name + ": " + ex.Message;
            session.Http.MarkFailed(record);
            session.Timeline.Complete(record.TimelineEventId, record.DurationMs.Value, record.Error, BevToolsSeverity.Error);
            RecordException(session, ex, trigger?.Id);
            session.Http.Touch();
            throw;
        }
    }

    private static void RecordStatusError(BevToolsSession session, string method, string redactedUrl, int status, string? reasonPhrase, long? triggerId) =>
        session.Errors.Record(null, "http", $"{method} {StripOrigin(redactedUrl)} returned {status} {reasonPhrase}", precedingEventId: triggerId, exceptionType: "HttpStatus" + status);

    /// <summary>A cancelled request is the caller's decision, not an error.</summary>
    private static void RecordException(BevToolsSession session, Exception exception, long? triggerId)
    {
        if (exception is not OperationCanceledException)
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
                var value = AlwaysRedactedHeaders.Contains(header.Key) || session.Redactor.IsSensitiveName(header.Key)
                    ? BevRedactor.RedactedValue
                    : string.Join(", ", header.Value);
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
        return pathStart < 0 ? "/" : url[pathStart..];
    }

    internal static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => bytes + " B",
        < 1024 * 1024 => (bytes / 1024.0).ToString("0.#") + " KB",
        _ => (bytes / 1024.0 / 1024.0).ToString("0.##") + " MB",
    };
}
