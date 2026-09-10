namespace Bit.BlazorUI;

/// <summary>
/// Enables figures: an image alone in a paragraph, written with a title
/// (<c>![alt](/url "the caption")</c>), renders as a <c>&lt;figure&gt;</c> with that title as its
/// <c>&lt;figcaption&gt;</c>.
/// </summary>
public sealed class BitMarkdownFigureExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.AstProcessors.Add(new BitMarkdownFigureAstProcessor());
        builder.Renderers.Add(new BitMarkdownFigureRenderer());
    }
}
