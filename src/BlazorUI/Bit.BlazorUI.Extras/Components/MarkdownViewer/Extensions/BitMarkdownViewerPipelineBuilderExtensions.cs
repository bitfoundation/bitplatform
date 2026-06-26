namespace Bit.BlazorUI;

/// <summary>Fluent helpers for enabling the built-in Markdown flavors.</summary>
public static class BitMarkdownViewerPipelineBuilderExtensions
{
    /// <summary>Adds GitHub-style pipe tables.</summary>
    public static BitMarkdownViewerPipelineBuilder UsePipeTables(this BitMarkdownViewerPipelineBuilder b)
        => b.Use(new BitMarkdownViewerPipeTableExtension());

    /// <summary>Adds <c>~~strikethrough~~</c>.</summary>
    public static BitMarkdownViewerPipelineBuilder UseStrikethrough(this BitMarkdownViewerPipelineBuilder b)
        => b.Use(new BitMarkdownViewerStrikethroughExtension());

    /// <summary>Adds GitHub task lists (<c>- [ ]</c> / <c>- [x]</c>).</summary>
    public static BitMarkdownViewerPipelineBuilder UseTaskLists(this BitMarkdownViewerPipelineBuilder b)
        => b.Use(new BitMarkdownViewerTaskListExtension());

    /// <summary>Adds autolink literals (bare URLs and emails become links).</summary>
    public static BitMarkdownViewerPipelineBuilder UseAutoLinks(this BitMarkdownViewerPipelineBuilder b)
        => b.Use(new BitMarkdownViewerAutoLinkExtension());

    /// <summary>Adds <c>:shortcode:</c> emoji replacement.</summary>
    public static BitMarkdownViewerPipelineBuilder UseEmojis(this BitMarkdownViewerPipelineBuilder b)
        => b.Use(new BitMarkdownViewerEmojiExtension());

    /// <summary>Adds <c>:shortcode:</c> emoji replacement with per-pipeline emoji overrides.</summary>
    public static BitMarkdownViewerPipelineBuilder UseEmojis(this BitMarkdownViewerPipelineBuilder b, IReadOnlyDictionary<string, string> overrides)
        => b.Use(new BitMarkdownViewerEmojiExtension(overrides));

    /// <summary>Adds automatic heading id slugs.</summary>
    public static BitMarkdownViewerPipelineBuilder UseAutoIdentifiers(this BitMarkdownViewerPipelineBuilder b)
        => b.Use(new BitMarkdownViewerAutoIdentifierExtension());

    /// <summary>Adds the full GitHub Flavored Markdown bundle.</summary>
    public static BitMarkdownViewerPipelineBuilder UseGitHubFlavored(this BitMarkdownViewerPipelineBuilder b)
        => b.Use(new BitMarkdownViewerGitHubFlavoredExtension());

    /// <summary>Adds GFM plus emoji and auto-identifiers.</summary>
    public static BitMarkdownViewerPipelineBuilder UseAdvanced(this BitMarkdownViewerPipelineBuilder b)
        => b.UseGitHubFlavored().UseEmojis().UseAutoIdentifiers();
}
