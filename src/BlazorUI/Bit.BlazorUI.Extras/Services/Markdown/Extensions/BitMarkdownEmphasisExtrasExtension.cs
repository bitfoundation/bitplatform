namespace Bit.BlazorUI;

/// <summary>
/// Enables the emphasis flavors beyond CommonMark's <c>*</c>/<c>_</c> and GFM's <c>~~</c>:
/// <c>~subscript~</c>, <c>^superscript^</c>, <c>++inserted++</c> and <c>==highlighted==</c>,
/// rendered as <c>&lt;sub&gt;</c>, <c>&lt;sup&gt;</c>, <c>&lt;ins&gt;</c> and <c>&lt;mark&gt;</c>.
/// </summary>
/// <remarks>
/// Subscript shares the <c>~</c> character with strikethrough, and a delimiter character may only
/// belong to one processor. This extension therefore replaces whatever claims <c>~</c> with the
/// processor that reads both lengths - one <c>~</c> as subscript, two as strikethrough - so the
/// two flavors can be enabled in either order, and enabling this one implies strikethrough.
/// </remarks>
public sealed class BitMarkdownEmphasisExtrasExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        builder.DelimiterProcessors.RemoveAll(p => Array.IndexOf(p.Characters, '~') >= 0);
        builder.DelimiterProcessors.Add(new BitMarkdownStrikethroughDelimiterProcessor(subscript: true));
        builder.DelimiterProcessors.Add(new BitMarkdownEmphasisExtrasDelimiterProcessor());

        // Registering the strikethrough renderer here as well keeps "~~" rendering whether or not
        // BitMarkdownStrikethroughExtension is also in the pipeline. A renderer is chosen by what
        // it accepts, so a second registration of the same one is harmless.
        builder.Renderers.Add(new BitMarkdownStrikethroughRenderer());
        builder.Renderers.Add(new BitMarkdownEmphasisExtrasRenderer());
    }
}
