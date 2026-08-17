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
    /// <summary>The page as Markdown, or a null <c>Markdown</c> and the reason it could not be rendered.</summary>
    public static async Task<(string? Markdown, string? Error)> TryRenderMarkdownAsync(
        HtmlRenderer htmlRenderer, NavigationManager navigationManager, string baseUri, DocLink page)
    {
        EnsureLocation(navigationManager, baseUri, page.Url);

        try
        {
            var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var component = await htmlRenderer.RenderComponentAsync(page.PageType);

                return component.ToHtmlString();
            });

            return (html.ToMarkdown(), null);
        }
        catch (Exception exception)
        {
            return (null, exception.Message);
        }
    }

    /// <summary>
    /// Tells the request's NavigationManager where it is, before a page asks.
    /// <para>
    /// Outside a component endpoint nothing initializes it, and every page on this site reads it
    /// indirectly: PageHeader emits a canonical URL from it and DemoSection builds its anchors from
    /// it. Uninitialized, the first of those throws and takes the whole page with it. Initializing
    /// it with the URL the page really lives at also means the anchors and canonicals in the
    /// Markdown are the site's own, rather than invented.
    /// </para>
    /// </summary>
    private static void EnsureLocation(NavigationManager navigationManager, string baseUri, string url)
    {
        if (navigationManager is not IHostEnvironmentNavigationManager host) return;

        try
        {
            host.Initialize(baseUri, $"{baseUri}{url}");
        }
        catch (InvalidOperationException)
        {
            // Already initialized - this scope has rendered a page before, and the location it was
            // given is close enough: it differs only in which docs page the URL names.
        }
    }

    /// <summary>What to answer with when the page did not render - and where its content is anyway.</summary>
    public static string Unavailable(DocLink page, string? error) =>
        $"The '{page.Title}' documentation page could not be rendered on the server{(error is null ? null : $": {error}")}. " +
        $"It is readable at /{page.Url} on the live documentation site. For the same material as text, call " +
        $"InspectButilApi(name: \"{page.Url}\") for what the API needs, and GetButilApiDetails for its members.";
}
