namespace Bit.BlazorUI;

/// <summary>Enables GitHub autolink literals (bare URLs and emails become links).</summary>
public sealed class BitMarkdownAutoLinkExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownAutoLinkAstProcessor());
}
