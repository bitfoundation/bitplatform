namespace Bit.BlazorUI;

/// <summary>Enables <c>~~strikethrough~~</c> (GFM).</summary>
public sealed class BitMarkdownViewerStrikethroughExtension : IBitMarkdownViewerExtension
{
    public void Setup(BitMarkdownViewerPipelineBuilder builder)
    {
        builder.DelimiterProcessors.Add(new BitMarkdownViewerStrikethroughDelimiterProcessor());
        builder.Renderers.Add(new BitMarkdownViewerStrikethroughRenderer());
    }
}
