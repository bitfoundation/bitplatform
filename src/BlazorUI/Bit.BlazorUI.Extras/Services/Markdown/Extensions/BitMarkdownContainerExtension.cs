namespace Bit.BlazorUI;

/// <summary>
/// Enables custom containers: <c>:::name optional title</c> ... <c>:::</c> renders as a
/// <c>div</c> classed after the name, which is how documentation sites write admonitions and
/// layout blocks.
/// </summary>
public sealed class BitMarkdownContainerExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new BitMarkdownContainerBlockParser());
        builder.Renderers.Add(new BitMarkdownContainerRenderer());
    }
}
