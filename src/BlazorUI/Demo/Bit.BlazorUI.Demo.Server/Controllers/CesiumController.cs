namespace Bit.BlazorUI.Demo.Server.Controllers;

/// <summary>
/// Same-origin passthrough for the CesiumJS build the BitMap demo loads.
/// <para>
/// The demo site is served cross-origin isolated with Cross-Origin-Embedder-Policy: require-corp
/// (see Middlewares.cs), under which a cross-origin subresource is blocked unless its response
/// carries a Cross-Origin-Resource-Policy header or it is requested in CORS mode. cesium.com sends
/// neither CORP nor Access-Control-Allow-Origin, so neither route works and the script is blocked
/// outright. Serving it from this origin sidesteps the check entirely - COEP only constrains
/// cross-origin resources.
/// </para>
/// <para>
/// The whole Build/Cesium tree has to come through here, not just Cesium.js: CesiumJS resolves its
/// own workers, .wasm decoders and assets relative to the URL its script tag was loaded from, so
/// those requests land on this route as well and would otherwise go straight back to cesium.com.
/// </para>
/// </summary>
[ApiController]
[Route("api/cesium")]
public partial class CesiumController : AppControllerBase
{
    /// <summary>
    /// The only upstream this controller will ever fetch from. Every composed URL is checked
    /// against this prefix after resolution, so a path containing '..' (or an absolute URL) can
    /// never reach another host - this endpoint takes a caller-supplied path and would otherwise
    /// be a server-side request forgery hole.
    /// <para>
    /// Derived from <see cref="BitCesiumMapProvider.DefaultBaseUrl"/> rather than spelled out, so
    /// that bumping the pinned CesiumJS release in one place cannot leave this proxy silently
    /// serving the previous build to the demo.
    /// </para>
    /// </summary>
    private const string UpstreamBaseUrl = $"{BitCesiumMapProvider.DefaultBaseUrl}/";

    /// <summary>Guards against a pathological path being composed into a request URL.</summary>
    private const int MaxPathLength = 512;

    /// <summary>
    /// How long the upstream is given to accept the request and answer with its headers. It
    /// deliberately does not cover the body: that is streamed straight to the browser and so is
    /// paced by the browser, which is why the named client itself carries no
    /// <see cref="HttpClient.Timeout"/> (see Services.cs).
    /// </summary>
    private static readonly TimeSpan UpstreamHeadersTimeout = TimeSpan.FromSeconds(30);

    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    [HttpGet("{**path}")]
    public async Task<IActionResult> Get(string? path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length > MaxPathLength) return NotFound();

        if (Uri.TryCreate(new Uri(UpstreamBaseUrl, UriKind.Absolute), path, out var upstreamUri) is false) return NotFound();

        // Resolution has already collapsed any '..' segments, so comparing the *result* against the
        // base is what actually confines the request - validating the raw path would not.
        if (upstreamUri.AbsoluteUri.StartsWith(UpstreamBaseUrl, StringComparison.Ordinal) is false) return NotFound();

        var httpClient = httpClientFactory.CreateClient(nameof(CesiumController));

        // The token this send is made with also governs the reads of the streamed body below, so
        // the deadline is armed for the header phase only and disarmed as soon as the headers are
        // in - a slow but healthy client must not have its download cut off at the 30 s mark. It
        // stays linked to RequestAborted throughout, so a client that goes away still tears the
        // upstream request down. Disposed with the request rather than here: the body outlives
        // this method.
        var headersTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        HttpContext.Response.RegisterForDispose(headersTimeout);
        headersTimeout.CancelAfter(UpstreamHeadersTimeout);

        // ResponseHeadersRead so the body streams through instead of being buffered in memory:
        // Cesium.js alone is several megabytes.
        var upstreamResponse = await httpClient.GetAsync(upstreamUri, HttpCompletionOption.ResponseHeadersRead, headersTimeout.Token);

        headersTimeout.CancelAfter(Timeout.InfiniteTimeSpan);

        if (upstreamResponse.IsSuccessStatusCode is false)
        {
            var statusCode = (int)upstreamResponse.StatusCode;
            upstreamResponse.Dispose();
            return StatusCode(statusCode);
        }

        // The body below is the upstream stream itself, so the response message has to outlive this
        // method; hand it to the request's dispose list rather than a using block.
        HttpContext.Response.RegisterForDispose(upstreamResponse);

        // The content type has to be forwarded verbatim: the tree mixes JavaScript, CSS, JSON, images,
        // glTF and application/wasm, and the browser refuses a worker or a WebAssembly module served
        // under the wrong type.
        var contentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

        // A pinned CesiumJS release is immutable, so it is safe to let the browser and the CDN hold on
        // to it. The COOP/COEP headers this response also carries are UA-independent (see
        // Middlewares.cs), so a shared cache entry is safe to hand to any browser.
        Response.GetTypedHeaders().CacheControl = new() { Public = true, MaxAge = TimeSpan.FromDays(30) };

        var stream = await upstreamResponse.Content.ReadAsStreamAsync(cancellationToken);

        return File(stream, contentType);
    }
}
