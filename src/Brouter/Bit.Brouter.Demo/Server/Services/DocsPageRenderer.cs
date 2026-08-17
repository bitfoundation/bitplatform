using Bit.Brouter.Demo.Client;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Brouter.Demo.Server.Services;

/// <summary>
/// Renders a documentation page component and flattens it to Markdown, for the tool and the
/// resource that both hand out the docs.
/// <para>
/// The component is the very one the site serves, so what an MCP client reads is what a human
/// reads. It renders on its own, though - outside the app's &lt;Brouter&gt; and its layout - so a
/// page that reaches for the router or for JS interop while initializing throws instead of
/// producing HTML. That has to come back as a page-shaped answer explaining itself, never as a
/// failed tool call that tells the agent nothing.
/// </para>
/// </summary>
public static class DocsPageRenderer
{
    /// <summary>The page as Markdown, or a null <c>Markdown</c> and the reason it could not be rendered.</summary>
    public static async Task<(string? Markdown, string? Error)> TryRenderMarkdownAsync(HtmlRenderer htmlRenderer, DocsPageInfo page)
    {
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

    /// <summary>What to answer with when the page did not render - and where its content is anyway.</summary>
    public static string Unavailable(DocsPageInfo page, string? error) =>
        $"The '{page.Title}' documentation page could not be rendered on the server{(error is null ? null : $": {error}")}. " +
        $"It is readable at {page.Url} on the live documentation site. For the same material as text, " +
        $"call SearchBrouter(query: \"{page.Keywords.Split(' ').FirstOrDefault()}\") or GetBrouterGuideSections.";
}
