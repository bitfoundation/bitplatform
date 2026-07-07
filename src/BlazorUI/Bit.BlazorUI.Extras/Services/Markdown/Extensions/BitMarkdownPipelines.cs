namespace Bit.BlazorUI;

/// <summary>Ready-made, cached pipelines for common configurations.</summary>
public static class BitMarkdownViewerPipelines
{
    private static readonly Lazy<BitMarkdownViewerPipeline> _gitHub
        = new(() => new BitMarkdownViewerPipelineBuilder().UseGitHubFlavored().Build());
    private static readonly Lazy<BitMarkdownViewerPipeline> _advanced
        = new(() => new BitMarkdownViewerPipelineBuilder().UseAdvanced().Build());

    /// <summary>Basic CommonMark core only (no flavors).</summary>
    public static BitMarkdownViewerPipeline Basic => BitMarkdownViewerPipeline.Basic;

    /// <summary>GitHub Flavored Markdown (tables, strikethrough, task lists, autolinks).</summary>
    public static BitMarkdownViewerPipeline GitHub => _gitHub.Value;

    /// <summary>GFM plus emoji and auto-identifiers.</summary>
    public static BitMarkdownViewerPipeline Advanced => _advanced.Value;
}
