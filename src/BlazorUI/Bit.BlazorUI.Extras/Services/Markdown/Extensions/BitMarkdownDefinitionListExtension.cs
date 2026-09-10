namespace Bit.BlazorUI;

/// <summary>
/// Enables definition lists: a term on its own line followed by <c>: its definition</c> renders as
/// a real <c>&lt;dl&gt;</c>, which is what a glossary, an API reference or a set of options is.
/// </summary>
public sealed class BitMarkdownDefinitionListExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new BitMarkdownDefinitionListBlockParser());
        builder.Renderers.Add(new BitMarkdownDefinitionListRenderer());
    }
}
