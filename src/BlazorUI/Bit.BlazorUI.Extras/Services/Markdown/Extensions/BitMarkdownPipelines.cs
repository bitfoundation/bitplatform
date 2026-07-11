namespace Bit.BlazorUI;

/// <summary>Ready-made, cached pipelines for common configurations.</summary>
public static class BitMarkdownPipelines
{
    private static readonly Lazy<BitMarkdownPipeline> _basic
        = new(() => new BitMarkdownPipelineBuilder().Build());
    private static readonly Lazy<BitMarkdownPipeline> _gitHub
        = new(() => new BitMarkdownPipelineBuilder().UseGitHubFlavored().Build());
    private static readonly Lazy<BitMarkdownPipeline> _advanced
        = new(() => new BitMarkdownPipelineBuilder().UseAdvanced().Build());

    /// <summary>Basic CommonMark core only (no flavors).</summary>
    public static BitMarkdownPipeline Basic => _basic.Value;

    /// <summary>GitHub Flavored Markdown (tables, strikethrough, task lists, autolinks).</summary>
    public static BitMarkdownPipeline GitHub => _gitHub.Value;

    /// <summary>GFM plus emoji and auto-identifiers.</summary>
    public static BitMarkdownPipeline Advanced => _advanced.Value;
}
