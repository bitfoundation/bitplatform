using Bit.BlazorUI.Markdown.Extensions;

namespace Bit.BlazorUI;

/// <summary>Fluent helpers for enabling the built-in Markdown flavors.</summary>
public static class BitMarkdownPipelineBuilderExtensions
{
    /// <summary>Adds GitHub-style pipe tables.</summary>
    public static BitMarkdownPipelineBuilder UsePipeTables(this BitMarkdownPipelineBuilder b)
        => b.Use(new PipeTableExtension());

    /// <summary>Adds <c>~~strikethrough~~</c>.</summary>
    public static BitMarkdownPipelineBuilder UseStrikethrough(this BitMarkdownPipelineBuilder b)
        => b.Use(new StrikethroughExtension());

    /// <summary>Adds GitHub task lists (<c>- [ ]</c> / <c>- [x]</c>).</summary>
    public static BitMarkdownPipelineBuilder UseTaskLists(this BitMarkdownPipelineBuilder b)
        => b.Use(new TaskListExtension());

    /// <summary>Adds autolink literals (bare URLs and emails become links).</summary>
    public static BitMarkdownPipelineBuilder UseAutoLinks(this BitMarkdownPipelineBuilder b)
        => b.Use(new AutoLinkExtension());

    /// <summary>Adds <c>:shortcode:</c> emoji replacement.</summary>
    public static BitMarkdownPipelineBuilder UseEmojis(this BitMarkdownPipelineBuilder b)
        => b.Use(new EmojiExtension());

    /// <summary>Adds automatic heading id slugs.</summary>
    public static BitMarkdownPipelineBuilder UseAutoIdentifiers(this BitMarkdownPipelineBuilder b)
        => b.Use(new AutoIdentifierExtension());

    /// <summary>Adds the full GitHub Flavored Markdown bundle.</summary>
    public static BitMarkdownPipelineBuilder UseGitHubFlavored(this BitMarkdownPipelineBuilder b)
        => b.Use(new GitHubFlavoredExtension());

    /// <summary>Adds GFM plus emoji and auto-identifiers.</summary>
    public static BitMarkdownPipelineBuilder UseAdvanced(this BitMarkdownPipelineBuilder b)
        => b.UseGitHubFlavored().UseEmojis().UseAutoIdentifiers();
}

/// <summary>Ready-made, cached pipelines for common configurations.</summary>
public static class BitMarkdownPipelines
{
    private static BitMarkdownPipeline? _gitHub;
    private static BitMarkdownPipeline? _advanced;

    /// <summary>Basic CommonMark core only (no flavors).</summary>
    public static BitMarkdownPipeline Basic => BitMarkdownPipeline.Basic;

    /// <summary>GitHub Flavored Markdown (tables, strikethrough, task lists, autolinks).</summary>
    public static BitMarkdownPipeline GitHub
        => _gitHub ??= new BitMarkdownPipelineBuilder().UseGitHubFlavored().Build();

    /// <summary>GFM plus emoji and auto-identifiers.</summary>
    public static BitMarkdownPipeline Advanced
        => _advanced ??= new BitMarkdownPipelineBuilder().UseAdvanced().Build();
}
