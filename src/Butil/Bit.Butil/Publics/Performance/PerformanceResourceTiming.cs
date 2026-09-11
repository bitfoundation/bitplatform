namespace Bit.Butil;

/// <summary>
/// One network fetch, broken into the phases it spent its time in - redirects, DNS, TCP, TLS,
/// request, response - plus the three sizes that say whether it was compressed and whether it came
/// from the cache.
/// <br />
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/PerformanceResourceTiming">https://developer.mozilla.org/en-US/docs/Web/API/PerformanceResourceTiming</see>
/// </summary>
/// <remarks>
/// Every timestamp is milliseconds since the time origin, and a phase the request skipped reports
/// <c>0</c> rather than a duration. Cross-origin responses report <c>0</c> for most of them - and
/// for the sizes - unless the server sends <c>Timing-Allow-Origin</c>, so a third-party resource
/// that looks instantaneous is usually one that is not allowed to tell you.
/// </remarks>
public class PerformanceResourceTiming : PerformanceEntry
{
    /// <summary>What started the fetch: <c>"script"</c>, <c>"link"</c>, <c>"img"</c>, <c>"fetch"</c>, <c>"xmlhttprequest"</c>, <c>"navigation"</c>...</summary>
    public string? InitiatorType { get; set; }

    /// <summary>How the response was delivered: <c>"cache"</c>, <c>"navigational-prefetch"</c>, or empty for the network.</summary>
    public string? DeliveryType { get; set; }

    /// <summary>The ALPN protocol id of the connection, e.g. <c>"h2"</c>, <c>"h3"</c>, <c>"http/1.1"</c>.</summary>
    public string? NextHopProtocol { get; set; }

    /// <summary>Whether the resource blocked rendering: <c>"blocking"</c> or <c>"non-blocking"</c>.</summary>
    public string? RenderBlockingStatus { get; set; }

    /// <summary>When the service worker started handling the fetch.</summary>
    public double WorkerStart { get; set; }

    /// <summary>When the first redirect started.</summary>
    public double RedirectStart { get; set; }

    /// <summary>When the last redirect finished.</summary>
    public double RedirectEnd { get; set; }

    /// <summary>When the browser started fetching, after any redirects and cache lookup.</summary>
    public double FetchStart { get; set; }

    /// <summary>When the DNS lookup started.</summary>
    public double DomainLookupStart { get; set; }

    /// <summary>When the DNS lookup finished.</summary>
    public double DomainLookupEnd { get; set; }

    /// <summary>When the connection started being established.</summary>
    public double ConnectStart { get; set; }

    /// <summary>When the connection was established.</summary>
    public double ConnectEnd { get; set; }

    /// <summary>When the TLS handshake started; <c>0</c> for a plain HTTP connection.</summary>
    public double SecureConnectionStart { get; set; }

    /// <summary>When the request was sent.</summary>
    public double RequestStart { get; set; }

    /// <summary>When the first byte of the response arrived.</summary>
    public double ResponseStart { get; set; }

    /// <summary>When the last byte of the response arrived.</summary>
    public double ResponseEnd { get; set; }

    /// <summary>When the first interim (1xx) response arrived, for an Early Hints response.</summary>
    public double FirstInterimResponseStart { get; set; }

    /// <summary>Bytes fetched over the wire, headers included. <c>0</c> from cache, and <c>0</c> cross-origin without <c>Timing-Allow-Origin</c>.</summary>
    public long TransferSize { get; set; }

    /// <summary>The body's size as transmitted, i.e. still compressed.</summary>
    public long EncodedBodySize { get; set; }

    /// <summary>The body's size after decompression.</summary>
    public long DecodedBodySize { get; set; }

    /// <summary>The HTTP status of the response.</summary>
    public int ResponseStatus { get; set; }
}
