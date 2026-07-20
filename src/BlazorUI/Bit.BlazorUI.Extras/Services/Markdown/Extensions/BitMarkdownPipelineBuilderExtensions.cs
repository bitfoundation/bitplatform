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

    /// <summary>Adds the full GitHub Flavored Markdown bundle.</summary>
    public static BitMarkdownPipelineBuilder UseGitHubFlavored(this BitMarkdownPipelineBuilder b)
        => b.Use(new BitMarkdownGitHubFlavoredExtension());

    /// <summary>Adds GFM plus emoji and auto-identifiers.</summary>
    public static BitMarkdownPipelineBuilder UseAdvanced(this BitMarkdownPipelineBuilder b)
        => b.UseGitHubFlavored().UseEmojis().UseAutoIdentifiers();
}
