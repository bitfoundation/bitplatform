using System.Collections.Concurrent;
using Bit.Butil.Demo.Client.Docs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.Butil.Demo.Server.Services;

/// <summary>
/// Renders a documentation page component and flattens it to Markdown, for the tool and the
/// resource that both hand out the docs.
/// <para>
/// The component is the very one the site serves, so what an MCP client reads is what a human
/// reads - there is no second copy of the documentation that could go stale. It renders on its own
/// though, outside the app's router and its layout, so anything the page reads from its
/// surroundings has to be arranged first (see <see cref="EnsureLocation"/>), and a page that still
/// throws has to come back as a page-shaped answer explaining itself rather than as a failed tool
/// call that tells the agent nothing.
/// </para>
/// <para>
/// The live demos on the page do not run: nothing calls into JavaScript here, and Butil's own
/// prerender behaviour - safe defaults instead of exceptions - is what makes rendering them
/// harmless. The prose, the code samples and the API tables are the part worth reading anyway.
/// </para>
/// </summary>
public static class DocsPageRenderer
{
    /// <summary>
    /// The docs pages are rich enough that a couple of them would otherwise dominate a client's
    /// context window; the ones on this site land far below the cap.
    /// </summary>
    public const int MaxDocumentLength = 40_000;

    // The rendered Markdown of every docs page served so far, keyed by origin and slug.
    private static readonly ConcurrentDictionary<string, string> _rendered = new(StringComparer.Ordinal);

    // Bounded on purpose: half of that key is the Host header, which nothing here can vouch for. A
    // deployment answers on one or two origins, so real traffic never approaches this cap and only
    // forged Hosts could - past which pages still render, they just stop being kept.
    private const int MaxCachedPages = 512;

    /// <summary>
    /// The origin the current request arrived on - what a docs page's canonical URL and anchors are
    /// built from while it renders, and what the MCP page prints as this server's own endpoints.
    /// Behind a reverse proxy the forwarded-headers middleware has already rewritten Scheme and
    /// Host, so this is the public origin rather than the container's.
    /// <para>
    /// Read off the accessor rather than off <c>ControllerBase.Request</c>: over MCP the tools are
    /// invoked on an instance the tool host built from DI, which never had a ControllerContext
    /// assigned, so <c>Request</c> would be a null reference on exactly the transport that matters.
    /// </para>
    /// </summary>
    public static string BaseUri(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext is { Request: var request }
            ? $"{request.Scheme}://{request.Host}/"
            : "https://localhost/";
    }

    /// <summary>
    /// The same as <see cref="TryRenderMarkdownAsync"/>, rendering each page once per origin.
    /// <para>
    /// Rendering a page and flattening it costs far more than serving it, and a page's Markdown only
    /// depends on the origin it was rendered for - the MCP page prints this server's endpoint URLs
    /// into its own body - so the first caller from an origin pays for it and everyone after reads
    /// the same Markdown.
    /// </para>
    /// </summary>
    public static async Task<(string? Markdown, string? Error)> RenderCachedMarkdownAsync(
        HtmlRenderer htmlRenderer, NavigationManager navigationManager, ILogger logger, string baseUri, DocLink page,
        CancellationToken cancellationToken = default)
    {
        // '\n' cannot occur in either part, so no pair of them can collide on one key.
        var key = $"{baseUri}\n{page.Url}";

        if (_rendered.TryGetValue(key, out var cached)) return (cached, null);

        var (markdown, error) = await TryRenderMarkdownAsync(htmlRenderer, navigationManager, logger, baseUri, page, cancellationToken);

        // Not cached: a page that failed to render is a bug to be fixed, not a stale answer to keep.
        if (markdown is null) return (null, error);

        if (_rendered.Count < MaxCachedPages) _rendered[key] = markdown;

        return (markdown, null);
    }

    /// <summary>Cuts a document down to what an MCP client can be handed in one answer.</summary>
    public static string Truncate(string text)
    {
        return text.Length <= MaxDocumentLength
            ? text
            : $"{text[..MaxDocumentLength]}\n\n[truncated - the full text is longer than {MaxDocumentLength} characters]";
    }

    /// <summary>The page as Markdown, or a null <c>Markdown</c> and the reason it could not be rendered.</summary>
    public static async Task<(string? Markdown, string? Error)> TryRenderMarkdownAsync(
        HtmlRenderer htmlRenderer, NavigationManager navigationManager, ILogger logger, string baseUri, DocLink page,
        CancellationToken cancellationToken = default)
    {
        if (EnsureLocation(navigationManager, baseUri, page.Url) is false)
        {
            return (null, $"this request already rendered a different page, and its NavigationManager " +
                          $"cannot be pointed at /{page.Url} a second time - the canonical URL and the " +
                          $"anchors would be the other page's. Ask for one page per request.");
        }

        try
        {
            // Checked here and once more after the render: HtmlRenderer has no overload that takes a
            // token, so the render itself cannot be interrupted - but a client that gave up while a
            // page was queued behind the dispatcher should not then be charged for flattening it.
            cancellationToken.ThrowIfCancellationRequested();

            var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var component = await htmlRenderer.RenderComponentAsync(page.PageType);

                return component.ToHtmlString();
            });

            cancellationToken.ThrowIfCancellationRequested();

            return (html.ToMarkdown(), null);
        }
        catch (OperationCanceledException)
        {
            // The client cancelled or disconnected. Rethrown rather than reported: this is not a
            // page that failed to render, and answering with prose about it would put an
            // explanation of the cancellation into the transcript of a request that no longer exists.
            throw;
        }
        catch (Exception exception)
        {
            // Logged with the slug, answered with a fixed sentence: the exception's own message can
            // carry paths, connection strings and internals of this server, and the caller is an
            // arbitrary MCP client. What went wrong belongs in the server's log, where whoever can
            // fix it will look; the client only needs to know that this page is not coming.
            logger.LogError(exception, "Rendering the Bit.Butil documentation page /{Slug} failed.", page.Url);

            return (null, "the page threw while rendering - the server's log has the detail");
        }
    }

    /// <summary>
    /// Tells the request's NavigationManager where it is, before a page asks - and reports whether
    /// it now says what this page needs it to say.
    /// <para>
    /// Outside a component endpoint nothing initializes it, and every page on this site reads it
    /// indirectly: PageHeader emits a canonical URL from it and DemoSection builds its anchors from
    /// it. Uninitialized, the first of those throws and takes the whole page with it. Initializing
    /// it with the URL the page really lives at also means the anchors and canonicals in the
    /// Markdown are the site's own, rather than invented.
    /// </para>
    /// <para>
    /// It can only be initialized once per scope, so a scope that has already rendered another page
    /// cannot render this one honestly: every anchor and canonical would name the first page. That
    /// is worse than not answering, because the answer is then cached under this page's own key.
    /// </para>
    /// </summary>
    private static bool EnsureLocation(NavigationManager navigationManager, string baseUri, string url)
    {
        if (navigationManager is not IHostEnvironmentNavigationManager host) return true;

        var uri = $"{baseUri}{url}";

        try
        {
            host.Initialize(baseUri, uri);

            return true;
        }
        catch (InvalidOperationException)
        {
            // Already initialized: usable only if it happens to point at this very page.
            return string.Equals(navigationManager.Uri, uri, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>What to answer with when the page did not render - and where its content is anyway.</summary>
    public static string Unavailable(DocLink page, string? error)
    {
        // A narrative page - "Getting started", "Render modes" - documents no API, so its slug is
        // not a name PlanButilFeature can be asked about. Sending an agent there produces a second
        // miss on top of the first one.
        var instead = page.Support == ApiSupport.Guide
            ? "call GetButilGuideSection for the same ground in the library's own reference guide, or GetButilApiDetails with no type name for its public API."
            : $"call PlanButilFeature(apis: \"{page.Url}\") for what the API needs, and GetButilApiDetails for its members.";

        return $"The '{page.Title}' documentation page could not be rendered on the server{(error is null ? null : $": {error}")}. " +
               $"It is readable at /{page.Url} on the live documentation site. For the same material as text, {instead}";
    }
}
