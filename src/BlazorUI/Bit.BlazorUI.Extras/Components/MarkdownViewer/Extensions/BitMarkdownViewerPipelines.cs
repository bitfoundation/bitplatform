namespace Bit.BlazorUI;

/// <summary>Ready-made, cached pipelines for common configurations.</summary>
public static class BitMarkdownViewerPipelines
{
    private static BitMarkdownViewerPipeline? _gitHub;
    private static BitMarkdownViewerPipeline? _advanced;

    /// <summary>Basic CommonMark core only (no flavors).</summary>
    public static BitMarkdownViewerPipeline Basic => BitMarkdownViewerPipeline.Basic;

    /// <summary>GitHub Flavored Markdown (tables, strikethrough, task lists, autolinks).</summary>
    public static BitMarkdownViewerPipeline GitHub
        => _gitHub ??= new BitMarkdownViewerPipelineBuilder().UseGitHubFlavored().Build();

    /// <summary>GFM plus emoji and auto-identifiers.</summary>
    public static BitMarkdownViewerPipeline Advanced
        => _advanced ??= new BitMarkdownViewerPipelineBuilder().UseAdvanced().Build();
}
