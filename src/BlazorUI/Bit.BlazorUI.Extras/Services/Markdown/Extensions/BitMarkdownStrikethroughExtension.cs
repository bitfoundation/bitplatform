namespace Bit.BlazorUI;

/// <summary>Enables <c>~~strikethrough~~</c> (GFM).</summary>
public sealed class BitMarkdownStrikethroughExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.DelimiterProcessors.Add(new BitMarkdownStrikethroughDelimiterProcessor());
        builder.Renderers.Add(new BitMarkdownStrikethroughRenderer());
    }
}
