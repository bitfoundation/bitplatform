namespace Bit.BlazorUI;

/// <summary>Fluent helpers for enabling the built-in Markdown flavors.</summary>
public static class BitMarkdownPipelineBuilderExtensions
{
    /// <summary>Adds GitHub-style pipe tables.</summary>
    public static BitMarkdownPipelineBuilder UsePipeTables(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownPipeTableExtension());

    /// <summary>Adds <c>~~strikethrough~~</c>.</summary>
    public static BitMarkdownPipelineBuilder UseStrikethrough(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownStrikethroughExtension());

    /// <summary>Adds GitHub task lists (<c>- [ ]</c> / <c>- [x]</c>).</summary>
    public static BitMarkdownPipelineBuilder UseTaskLists(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownTaskListExtension());

    /// <summary>Adds autolink literals (bare URLs and emails become links).</summary>
    public static BitMarkdownPipelineBuilder UseAutoLinks(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownAutoLinkExtension());

    /// <summary>Adds <c>:shortcode:</c> emoji replacement.</summary>
    public static BitMarkdownPipelineBuilder UseEmojis(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownEmojiExtension());

    /// <summary>Adds <c>:shortcode:</c> emoji replacement with per-pipeline emoji overrides.</summary>
    public static BitMarkdownPipelineBuilder UseEmojis(this BitMarkdownPipelineBuilder b, IReadOnlyDictionary<string, string> overrides)
        => b.Use(new BitMarkdownEmojiExtension(overrides));

    /// <summary>Adds automatic heading id slugs.</summary>
    public static BitMarkdownPipelineBuilder UseAutoIdentifiers(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownAutoIdentifierExtension());

    /// <summary>Adds GitHub-style footnotes (<c>[^1]</c> plus a <c>[^1]: ...</c> definition).</summary>
    public static BitMarkdownPipelineBuilder UseFootnotes(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownFootnoteExtension());

    /// <summary>Adds GitHub alerts (<c>&gt; [!NOTE]</c>, <c>&gt; [!TIP]</c>, ...).</summary>
    public static BitMarkdownPipelineBuilder UseAlerts(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownAlertExtension());

    /// <summary>Renders every single newline as a line break, like a chat or comment box.</summary>
    public static BitMarkdownPipelineBuilder UseSoftLineAsHardLine(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownSoftLineAsHardLineExtension());

    /// <summary>Adds the full GitHub Flavored Markdown bundle: tables, strikethrough, task lists, autolinks, footnotes and alerts.</summary>
    public static BitMarkdownPipelineBuilder UseGitHubFlavored(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownGitHubFlavoredExtension());

    /// <summary>Adds the GitHub flavors plus emoji and auto-identifiers.</summary>
    public static BitMarkdownPipelineBuilder UseAdvanced(this BitMarkdownPipelineBuilder b)
        => b.UseGitHubFlavored().UseEmojis().UseAutoIdentifiers();
}
