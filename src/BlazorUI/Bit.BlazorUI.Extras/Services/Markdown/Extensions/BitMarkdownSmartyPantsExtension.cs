namespace Bit.BlazorUI;

/// <summary>
/// Enables typographic replacement (SmartyPants): <c>"quotes"</c> and <c>'quotes'</c> curl,
/// <c>--</c> and <c>---</c> become en and em dashes, <c>...</c> becomes an ellipsis, and
/// <c>&lt;&lt;</c> / <c>&gt;&gt;</c> become guillemets. Code and URLs are left alone.
/// </summary>
public sealed class BitMarkdownSmartyPantsExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
        => builder.AstProcessors.Add(new BitMarkdownSmartyPantsAstProcessor());
}
