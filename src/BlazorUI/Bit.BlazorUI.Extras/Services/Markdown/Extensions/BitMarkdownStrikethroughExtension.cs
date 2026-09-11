namespace Bit.BlazorUI;

/// <summary>Enables <c>~~strikethrough~~</c> (GFM).</summary>
public sealed class BitMarkdownStrikethroughExtension : IBitMarkdownExtension
{
    public void Setup(BitMarkdownPipelineBuilder builder)
    {
        // The emphasis-extras flavor may already own '~' (it reads a single one as subscript), and
        // a delimiter character may only belong to one processor. Its processor renders "~~" the
        // same way, so leaving it in place is what makes the two flavors order-independent.
        if (builder.DelimiterProcessors.Exists(p => Array.IndexOf(p.Characters, '~') >= 0) is false)
        {
            builder.DelimiterProcessors.Add(new BitMarkdownStrikethroughDelimiterProcessor());
        }

        builder.Renderers.Add(new BitMarkdownStrikethroughRenderer());
    }
}
