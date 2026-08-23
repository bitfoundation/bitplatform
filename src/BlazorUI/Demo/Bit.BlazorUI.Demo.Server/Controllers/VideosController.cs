using System.Net;
using Microsoft.Net.Http.Headers;

namespace Bit.BlazorUI.Demo.Server.Controllers;

/// <summary>
/// Same-origin passthrough for the demo videos hosted on videos.bitplatform.dev.
/// <para>
/// The demo site is served cross-origin isolated with Cross-Origin-Embedder-Policy: require-corp
/// (see Middlewares.cs), under which a cross-origin subresource is blocked unless its response
/// carries a Cross-Origin-Resource-Policy header or it is requested in CORS mode. The video host
/// sends neither CORP nor Access-Control-Allow-Origin, so neither route works and the &lt;video&gt;
/// element is left with a media error. Serving it from this origin sidesteps the check entirely -
/// COEP only constrains cross-origin resources.
/// </para>
/// <para>
/// Unlike the CesiumJS passthrough this one has to speak byte ranges: a media element seeks by
/// asking for one, and WebKit refuses to play a video at all from a source that does not answer
/// range requests. So the client's Range (and its conditional headers) go upstream verbatim and
/// the upstream's status - 206 and its Content-Range included - comes back verbatim, rather than
/// the response being flattened into a plain 200 by <see cref="ControllerBase.File(Stream, string)"/>.
/// </para>
/// </summary>
[ApiController]
[Route("api/videos")]
public partial class VideosController : AppControllerBase
{
    /// <summary>
    /// The only upstream this controller will ever fetch from. Every composed URL is checked
    /// against this prefix after resolution, so a path containing '..' (or an absolute URL) can
    /// never reach another host - this endpoint takes a caller-supplied path and would otherwise
    /// be a server-side request forgery hole.
    /// </summary>
    private const string UpstreamBaseUrl = "https://videos.bitplatform.dev/";

    /// <summary>Guards against a pathological path being composed into a request URL.</summary>
    private const int MaxPathLength = 512;

    /// <summary>
    /// How long the upstream is given to accept the request and answer with its headers. As in
    /// <see cref="CesiumController"/> it deliberately does not cover the body, which is streamed
    /// at the pace of the browser downloading it - all the more so here, where the browser holds
    /// a media stream open for as long as it is playing.
    /// </summary>
    private static readonly TimeSpan UpstreamHeadersTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Request headers forwarded upstream: the range the media element is asking for, and the
    /// validators that let the upstream answer a re-request with a 304 instead of the bytes.
    /// </summary>
    private static readonly string[] ForwardedRequestHeaders =
    [
        HeaderNames.Range, HeaderNames.IfRange, HeaderNames.IfNoneMatch, HeaderNames.IfModifiedSince
    ];

    /// <summary>
    /// Response headers forwarded back: what a 206 means (Content-Range), that ranges may be asked
    /// for at all (Accept-Ranges), and the validators the browser needs to revalidate later.
    /// </summary>
    private static readonly string[] ForwardedResponseHeaders =
    [
        HeaderNames.ContentRange, HeaderNames.AcceptRanges, HeaderNames.ETag, HeaderNames.LastModified
    ];

    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    [HttpGet("{**path}")]
    public async Task<IActionResult> Get(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > MaxPathLength) return NotFound();

        if (Uri.TryCreate(new Uri(UpstreamBaseUrl, UriKind.Absolute), path, out var upstreamUri) is false) return NotFound();

        // Resolution has already collapsed any '..' segments, so comparing the *result* against the
        // base is what actually confines the request - validating the raw path would not.
        if (upstreamUri.AbsoluteUri.StartsWith(UpstreamBaseUrl, StringComparison.Ordinal) is false) return NotFound();

        using var upstreamRequest = new HttpRequestMessage(HttpMethod.Get, upstreamUri);
        foreach (var header in ForwardedRequestHeaders)
        {
            if (Request.Headers.TryGetValue(header, out var values))
            {
                upstreamRequest.Headers.TryAddWithoutValidation(header, (IEnumerable<string?>)values);
            }
        }

        var httpClient = httpClientFactory.CreateClient(nameof(VideosController));

        // The token a send is made with also governs the reads of the streamed body below, so the
        // deadline is armed for the header phase only and disarmed as soon as the headers are in -
        // a video being watched must not have its stream cut off at the 30 s mark. It stays linked
        // to RequestAborted throughout, so a viewer who navigates away still tears the upstream
        // request down. Disposed with the request rather than here: the body outlives this method.
        var headersTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        HttpContext.Response.RegisterForDispose(headersTimeout);
        headersTimeout.CancelAfter(UpstreamHeadersTimeout);

        // ResponseHeadersRead so the body streams through instead of being buffered in memory: the
        // demo videos are tens of megabytes.
        var upstreamResponse = await httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, headersTimeout.Token);

        headersTimeout.CancelAfter(Timeout.InfiniteTimeSpan);

        if (upstreamResponse.IsSuccessStatusCode is false && upstreamResponse.StatusCode != HttpStatusCode.NotModified)
        {
            var statusCode = (int)upstreamResponse.StatusCode;
            upstreamResponse.Dispose();
            return StatusCode(statusCode);
        }

        // The body below is the upstream stream itself, so the response message has to outlive this
        // method; hand it to the request's dispose list rather than a using block.
        HttpContext.Response.RegisterForDispose(upstreamResponse);

        Response.StatusCode = (int)upstreamResponse.StatusCode;

        foreach (var header in ForwardedResponseHeaders)
        {
            var values = upstreamResponse.Headers.TryGetValues(header, out var fromMessage)
                ? fromMessage
                : (upstreamResponse.Content.Headers.TryGetValues(header, out var fromContent) ? fromContent : null);

            if (values is not null)
            {
                Response.Headers[header] = values.ToArray();
            }
        }

        // The demo videos are immutable, so it is safe to let the browser and the CDN hold on to
        // them. The COOP/COEP headers this response also carries are UA-independent (see
        // Middlewares.cs), so a shared cache entry is safe to hand to any browser.
        Response.GetTypedHeaders().CacheControl = new() { Public = true, MaxAge = TimeSpan.FromDays(30) };

        if (upstreamResponse.StatusCode == HttpStatusCode.NotModified) return new EmptyResult();

        Response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "video/mp4";
        Response.ContentLength = upstreamResponse.Content.Headers.ContentLength;

        var stream = await upstreamResponse.Content.ReadAsStreamAsync(cancellationToken);

        await stream.CopyToAsync(Response.Body, cancellationToken);

        return new EmptyResult();
    }
}
