namespace Bit.BlazorUI;

/// <summary>Enables GitHub autolink literals (bare URLs and emails become links).</summary>
public sealed class BitMarkdownViewerAutoLinkExtension : IBitMarkdownViewerExtension
{
    public void Setup(BitMarkdownViewerPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownViewerAutoLinkAstProcessor());
}
