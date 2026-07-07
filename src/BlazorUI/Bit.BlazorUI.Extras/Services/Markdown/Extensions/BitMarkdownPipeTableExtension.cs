namespace Bit.BlazorUI;

/// <summary>Enables GitHub-style pipe tables.</summary>
public sealed class BitMarkdownViewerPipeTableExtension : IBitMarkdownViewerExtension
{
    public void Setup(BitMarkdownViewerPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new BitMarkdownViewerPipeTableBlockParser());
        builder.Renderers.Add(new BitMarkdownViewerTableRenderer());
    }
}
