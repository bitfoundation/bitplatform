using System.Net;
using System.Text;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Components.Web;
using Bit.BlazorUI.Demo.Client.Core.Pages.Theming;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>
/// The theming reference, served out of the very page the site publishes.
/// <para>
/// Theming is the one part of this library with no API surface to reflect: a theme is a set of
/// <c>--bit-*</c> custom properties, an HTML attribute and a handful of services, and what an agent
/// needs is the prose that ties them together. That prose exists, on /theming, and is kept current
/// because people read it - so it is rendered here rather than paraphrased into a second copy that
/// would start drifting the day it was written.
/// </para>
/// <para>
/// It is a long page, so it is served a chapter at a time. The chapters are its own
/// <c>DocSection</c>s, which come through the Markdown as headings, so the split needs nothing
/// declared on either side.
/// </para>
/// </summary>
public static class BlazorUIThemingGuide
{
    /// <summary>The closing block every documentation page carries. Not part of the reference.</summary>
    private const string FeedbackSection = "Feedback";

    private static string? _markdown;

    /// <summary>
    /// The chapter headings and what each covers, read out of the page's own markup rather than out
    /// of the rendered page.
    /// <para>
    /// The search index is built at startup and the completion handler answers before any renderer
    /// is in scope, so a chapter list that needed a render would be a chapter list that is
    /// sometimes missing. The markup is embedded, and its <c>DocSection</c> attributes are the same
    /// headings the render produces.
    /// </para>
    /// </summary>
    public static IReadOnlyList<(string Title, string Description)> Chapters { get; } = ReadChapters();

    private static (string Title, string Description)[] ReadChapters()
    {
        var markup = BlazorUIDemoSource.Raw("ThemingPage.razor");

        if (markup is null) return [];

        var document = new HtmlDocument();
        document.LoadHtml(markup);

        return [.. document.DocumentNode.Descendants()
            .Where(n => n.Name.Equals("docsection", StringComparison.OrdinalIgnoreCase))
            .Select(n => (Title: WebUtility.HtmlDecode(n.GetAttributeValue("Title", string.Empty)),
                          Description: WebUtility.HtmlDecode(n.GetAttributeValue("Description", string.Empty))))
            .Where(c => string.IsNullOrWhiteSpace(c.Title) is false && c.Title != FeedbackSection)];
    }

    /// <summary>
    /// One chapter of the guide, or the index of them when no chapter is named. A chapter is
    /// matched against both the chapter headings and the sub-headings inside them, so the name a
    /// reader saw in the index resolves either way.
    /// </summary>
    public static async ValueTask<string> Get(HtmlRenderer renderer, ILogger logger, string? section)
    {
        var markdown = await RenderAsync(renderer, logger);

        if (markdown is null)
        {
            return $"The theming reference could not be rendered on this server. It is readable at {BlazorUIMarkdown.SiteUrl}/theming.";
        }

        var sections = Split(markdown);

        if (string.IsNullOrWhiteSpace(section)) return Index(markdown, sections);

        var match = sections.FirstOrDefault(s => Matches(s.Title, section))
                 ?? sections.FirstOrDefault(s => s.SubHeadings.Any(h => Matches(h, section)));

        if (match is null)
        {
            return $"The theming reference has no chapter called '{section}'. Its chapters are: {string.Join(", ", sections.Select(s => $"'{s.Title}'"))}.";
        }

        return BlazorUIMarkdown.Truncate(match.Body);
    }

    private sealed record Chapter(string Title, string Body, string[] SubHeadings, int Lines);

    /// <summary>The chapters, with what each one covers - the answer when no chapter was named.</summary>
    private static string Index(string markdown, Chapter[] sections)
    {
        var builder = new StringBuilder();

        // The lead of the page itself, its own heading included: the paragraph above the first
        // chapter is what says what a theme in this library actually is, and a reader who asked for
        // the index needs that before the list of chapters means anything.
        var stop = markdown.IndexOf("\n## ", StringComparison.Ordinal);
        var lead = markdown[..(stop > 0 ? stop : markdown.Length)];

        // The page opens with its eyebrow - the word "Documentation" above the title - which is
        // chrome rather than content and reads as a stray line with nothing under it, so the lead
        // starts at the page's own heading.
        var title = lead.IndexOf("# ", StringComparison.Ordinal);

        builder.AppendLine(lead[(title > 0 ? title : 0)..].TrimEnd()).AppendLine();

        builder.AppendLine("Pass a chapter name to `GetBitBlazorUIThemingGuide`. Sub-headings resolve too.").AppendLine();

        foreach (var chapter in sections)
        {
            builder.AppendLine($"- **{chapter.Title}** ({chapter.Lines} lines){(chapter.SubHeadings.Length == 0 ? null : $": {string.Join(", ", chapter.SubHeadings)}")}");
        }

        return builder.ToString();
    }

    private static Chapter[] Split(string markdown)
    {
        var lines = markdown.Split('\n');
        var chapters = new List<Chapter>();

        var start = -1;
        var fenced = false;

        for (var i = 0; i <= lines.Length; i++)
        {
            // A '##' inside a fenced sample is a comment in the sample, not a chapter of the page.
            if (i < lines.Length && lines[i].StartsWith("```", StringComparison.Ordinal)) fenced = fenced is false;

            var isHeading = i == lines.Length || (fenced is false && lines[i].StartsWith("## ", StringComparison.Ordinal));

            if (isHeading is false) continue;

            if (start >= 0) chapters.Add(ToChapter(lines[start..i]));

            start = i;
        }

        return [.. chapters.Where(c => c.Title != FeedbackSection)];
    }

    private static Chapter ToChapter(string[] lines)
    {
        return new Chapter(
            Title: lines[0][3..].Trim(),
            Body: string.Join('\n', lines).TrimEnd(),
            SubHeadings: [.. lines.Where(l => l.StartsWith("### ", StringComparison.Ordinal)).Select(l => l[4..].Trim())],
            Lines: lines.Length);
    }

    /// <summary>Heading matching that ignores case, punctuation and the spaces between words.</summary>
    private static bool Matches(string heading, string wanted)
        => string.Equals(Normalize(heading), Normalize(wanted), StringComparison.Ordinal);

    private static string Normalize(string text)
        => new([.. text.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);

    /// <summary>
    /// The page as Markdown, rendered once per process. It renders outside a router and outside its
    /// layout, so a failure is answered as prose saying where the page is rather than as a failed
    /// tool call, and a failure is never cached.
    /// </summary>
    private static async ValueTask<string?> RenderAsync(HtmlRenderer renderer, ILogger logger)
    {
        if (_markdown is not null) return _markdown;

        try
        {
            var html = await renderer.Dispatcher.InvokeAsync(async () =>
            {
                var component = await renderer.RenderComponentAsync<ThemingPage>();

                return component.ToHtmlString();
            });

            return _markdown = html.ToMarkdown();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The theming page could not be rendered for an MCP client.");

            return null;
        }
    }
}
