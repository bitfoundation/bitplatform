using System.Text;
using System.Collections.Concurrent;
using Bit.Blex.Demo.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>One page of the documentation site, as the MCP tools address it.</summary>
/// <param name="Slug">The key a caller passes: the page's route, without its leading slash.</param>
/// <param name="Title">The page title, as the sidebar shows it.</param>
/// <param name="Summary">The one-line description from the site map.</param>
/// <param name="Section">The sidebar group the page belongs to.</param>
/// <param name="PageType">The routable component that renders the page.</param>
public sealed record BlexDocsPage(string Slug, string Title, string Summary, string Section, Type PageType)
{
    /// <summary>The page on the live documentation site.</summary>
    public string Url => $"{DocsPageRenderer.SiteUrl}/{Slug}";
}

/// <summary>
/// Renders a documentation page component and flattens it to Markdown, for the tool and the resource
/// that both hand out the docs.
/// <para>
/// The component is the very one the site serves, so what an MCP client reads is what a human reads.
/// The site map (the client's DocsCatalog) names the pages and the component that renders each one,
/// so there is no second list of pages here to fall out of step with the sidebar. The landing page is
/// not in it - it is a front door, not documentation - and neither is the site chrome: a page renders
/// on its own, outside the app's layouts and its &lt;BlexProvider&gt;, so what comes back is the page's
/// title block and its sections and nothing of the header, nav panel, pager or footer around them. A
/// page that reaches for JS interop while initializing throws instead of producing HTML. That comes back
/// as a page-shaped answer explaining itself, never as a failed tool call that tells the agent nothing.
/// </para>
/// </summary>
public static class DocsPageRenderer
{
    /// <summary>The live documentation site, which every page's URL points into.</summary>
    public const string SiteUrl = "https://blex.bitplatform.dev";

    /// <summary>The page a caller means when they ask for the root of the docs - an empty slug, "overview", "home".</summary>
    public const string RootSlug = "introduction";

    private static readonly ConcurrentDictionary<string, string> _markdown = new(StringComparer.Ordinal);

    private static readonly Lazy<BlexDocsPage[]> _pages = new(BuildPages);

    /// <summary>Every documentation page, in the order the sidebar lists them.</summary>
    public static BlexDocsPage[] Pages => _pages.Value;

    private static BlexDocsPage[] BuildPages()
        => [.. DocsCatalog.Sections.SelectMany(section => section.Pages.Select(page => new BlexDocsPage(
               page.Url.Trim('/'),
               page.Title,
               page.Description,
               section.Title,
               page.PageType)))];

    /// <summary>
    /// The page a slug names, or null when none does. The words a caller reaches for to mean the root
    /// page stand in for it, and slashes and case are ignored - the tool and the resource resolve slugs
    /// through this one method, so a slug that works in one cannot fail in the other.
    /// </summary>
    public static BlexDocsPage? FindPage(string? slug)
    {
        var trimmed = (slug ?? string.Empty).Trim().Trim('/');

        if (trimmed.Length == 0 || trimmed.ToLowerInvariant() is "overview" or "index" or "home") trimmed = RootSlug;

        return Pages.FirstOrDefault(page => string.Equals(page.Slug, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>What to answer when no page has that slug: the ones that do.</summary>
    public static string NoSuchPage(string? slug) =>
        $"No documentation page has the slug '{slug}'. Available slugs: {string.Join(", ", Pages.Select(page => page.Slug))}.";

    /// <summary>The pages, as the index GetBlexDocsPage answers with when it is called without a slug - the sidebar, in reading order.</summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Blex documentation").AppendLine();
        builder.AppendLine("Call `GetBlexDocsPage(slug: \"...\")` for one of these pages.").AppendLine();

        foreach (var section in Pages.GroupBy(page => page.Section))
        {
            builder.AppendLine($"## {section.Key}").AppendLine();

            foreach (var page in section)
            {
                builder.AppendLine($"- `{page.Slug}` - **{page.Title}**: {page.Summary}");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>The page as Markdown, or a null <c>Markdown</c> and the reason it could not be rendered.</summary>
    public static async ValueTask<(string? Markdown, string? Error)> TryRenderMarkdownAsync(HtmlRenderer htmlRenderer, BlexDocsPage page)
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

            // Only a success is kept: a page that failed to render is a bug to be fixed, not a stale
            // answer to serve for the lifetime of the process.
            _markdown[page.Slug] = markdown;

            return (markdown, null);
        }
        catch (Exception exception)
        {
            return (null, exception.Message);
        }
    }

    /// <summary>What to answer with when the page did not render - and where its content is anyway.</summary>
    public static string Unavailable(BlexDocsPage page, string? error) =>
        $"The '{page.Title}' documentation page could not be rendered on the server{(error is null ? null : $": {error}")}. " +
        $"It is readable at {page.Url} on the live documentation site. For the same material as text, " +
        $"call SearchBlex(query: \"{page.Title}\") or GetBlexGuideSection.";
}
