namespace Bit.BlazorUI;

/// <summary>
/// Enables mathematics: <c>$inline$</c> and <c>$$display$$</c> are kept verbatim - safe from
/// Markdown's emphasis and escape rules, which otherwise eat the underscores and backslashes TeX
/// is made of - and marked for a client-side typesetter such as KaTeX or MathJax.
/// </summary>
public sealed class BitMarkdownMathExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.BlockParsers.Add(new BitMarkdownMathBlockParser());
        builder.InlineParsers.Add(new BitMarkdownMathInlineParser());
        builder.Renderers.Add(new BitMarkdownMathRenderer());
    }
}
