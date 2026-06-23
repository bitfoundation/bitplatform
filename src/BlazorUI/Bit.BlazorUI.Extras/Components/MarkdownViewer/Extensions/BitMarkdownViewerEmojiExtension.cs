namespace Bit.BlazorUI;

/// <summary>Enables <c>:shortcode:</c> emoji replacement.</summary>
public sealed class BitMarkdownViewerEmojiExtension : IBitMarkdownViewerExtension
{
    public void Setup(BitMarkdownViewerPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownViewerEmojiAstProcessor());
}
