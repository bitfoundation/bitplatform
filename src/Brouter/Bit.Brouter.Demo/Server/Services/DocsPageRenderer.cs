using System.Collections.Concurrent;
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
    // Rendering a page and flattening it costs far more than serving it, and the pages are static:
    // the first caller pays for it and every caller after reads the same Markdown. The cache lives
    // here rather than in either caller so the tool and the resource cannot end up with two copies
    // of it - or, worse, with one of them re-rendering a page the other already has.
    private static readonly ConcurrentDictionary<string, string> _markdown = new(StringComparer.Ordinal);

    /// <summary>
    /// The page a slug names, or null when none does.
    /// <para>
    /// The overview's own slug is the empty string, which is not a thing anyone types, so the words
    /// a caller does reach for stand in for it. The tool and the resource resolve slugs through this
    /// one method, so a slug that works in one cannot fail in the other.
    /// </para>
    /// </summary>
    public static DocsPageInfo? FindPage(string? slug)
        => DocsCatalog.FindBySlug(slug is "overview" or "index" or "docs" ? string.Empty : slug);

    /// <summary>What to answer when no page has that slug: the ones that do.</summary>
    public static string NoSuchPage(string? slug)
    {
        var slugs = string.Join(", ", DocsCatalog.AllPages.Select(page => page.Slug.Length == 0 ? "overview" : page.Slug));

        return $"No documentation page has the slug '{slug}'. Available slugs: {slugs}.";
    }

    /// <summary>The page as Markdown, or a null <c>Markdown</c> and the reason it could not be rendered.</summary>
    public static async ValueTask<(string? Markdown, string? Error)> TryRenderMarkdownAsync(HtmlRenderer htmlRenderer, DocsPageInfo page)
    {
        if (_markdown.TryGetValue(page.Slug, out var cached)) return (cached, null);

        try
        {
            var html = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
            {
                var component = await htmlRenderer.RenderComponentAsync(page.PageType);

                return component.ToHtmlString();
            });

            var markdown = html.ToMarkdown();

            // Only a success is kept: a page that failed to render is a bug to be fixed, not a
            // stale answer to serve for the lifetime of the process.
            _markdown[page.Slug] = markdown;

            return (markdown, null);
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
