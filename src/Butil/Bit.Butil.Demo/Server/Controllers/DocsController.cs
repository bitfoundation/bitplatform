using System.Net.Mime;
using Bit.Butil.Demo.Server.Services;
using Microsoft.Net.Http.Headers;

namespace Bit.Butil.Demo.Server.Controllers;

/// <summary>
/// What the site's own pages ask the host for. Today that is one thing: the corpus its search box
/// searches - see <see cref="DocsContentIndex"/> and <c>Client/Shared/SearchBox.razor</c>.
/// <para>
/// The index is served rather than searched here on purpose. A search that runs on the server is a
/// request per keystroke, each of them racing the last, and a result list that stutters behind the
/// typing on a slow connection; the whole corpus is one download that the browser then searches at
/// memory speed, and that a returning visitor revalidates in a single 304.
/// </para>
/// </summary>
[ApiController]
[Route("api/docs")]
public class DocsController : ControllerBase
{
    /// <summary>
    /// The search corpus, gzipped for the browsers that say they can take it - it is prose, so the
    /// compressed copy is a fraction of the size, and this is the one payload on the site big enough
    /// for that to matter.
    /// </summary>
    [HttpGet("search-index")]
    public IActionResult SearchIndex()
    {
        var payload = DocsContentIndex.Wire;

        // The corpus changes when the app is deployed and never in between, so the ETag is the whole
        // caching story: no-cache asks the browser to revalidate rather than to re-download, and the
        // answer to a revalidation is 304 with no body at all.
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.ETag = payload.ETag;
        Response.Headers[HeaderNames.Vary] = HeaderNames.AcceptEncoding;

        // Both headers are lists with a syntax of their own - If-None-Match is comma-separated and its tags
        // may be weak, Accept-Encoding carries q values - so they are read parsed rather than as raw strings.
        var headers = Request.GetTypedHeaders();
        var etag = EntityTagHeaderValue.Parse(payload.ETag);

        if (headers.IfNoneMatch.Any(tag => tag.Equals(EntityTagHeaderValue.Any) ||
                                           tag.Compare(etag, useStrongComparison: false)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        // q=0 is a client saying it does not want gzip, which reads as "gzip" to a substring match.
        var acceptsGzip = headers.AcceptEncoding
            .Any(encoding => string.Equals(encoding.Value.Value, "gzip", StringComparison.OrdinalIgnoreCase) &&
                             encoding.Quality != 0);

        if (acceptsGzip is false) return File(payload.Json, MediaTypeNames.Application.Json);

        Response.Headers.ContentEncoding = "gzip";

        return File(payload.Gzip, MediaTypeNames.Application.Json);
    }
}
