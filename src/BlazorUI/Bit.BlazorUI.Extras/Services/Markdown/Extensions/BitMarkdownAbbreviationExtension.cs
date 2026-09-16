namespace Bit.BlazorUI;

/// <summary>
/// Enables abbreviations: <c>*[HTML]: HyperText Markup Language</c> declares a term once, and
/// every whole-word occurrence of it in the text becomes an <c>&lt;abbr&gt;</c> carrying the
/// expansion as its tooltip and as what a screen reader can read out.
/// </summary>
public sealed class BitMarkdownAbbreviationExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new BitMarkdownAbbreviationDefinitionParser());
        builder.AstProcessors.Add(new BitMarkdownAbbreviationAstProcessor());
        builder.Renderers.Add(new BitMarkdownAbbreviationRenderer());
    }
}
