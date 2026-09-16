namespace Bit.BlazorUI;

/// <summary>
/// Delimiter processor for <c>~</c>: a run of two is GFM strikethrough, and - when
/// <see cref="Subscript"/> is on, which the emphasis-extras flavor turns it on - a run of one is
/// subscript. Both live in one processor because a delimiter character may only belong to one.
/// </summary>
public sealed class BitMarkdownStrikethroughDelimiterProcessor : BitMarkdownDelimiterProcessor
{
    /// <summary>Creates a processor that only recognizes <c>~~strikethrough~~</c>.</summary>
    public BitMarkdownStrikethroughDelimiterProcessor() { }

    /// <summary>
    /// Creates a processor that also reads a single <c>~</c> as subscript when
    /// <paramref name="subscript"/> is <c>true</c>.
    /// </summary>
    public BitMarkdownStrikethroughDelimiterProcessor(bool subscript) => Subscript = subscript;

    /// <summary>True when a run of one <c>~</c> produces a <see cref="BitMarkdownSubscriptNode"/>.</summary>
    public bool Subscript { get; }

    public override char[] Characters => new[] { '~' };
    public override int MinRunLength => Subscript ? 1 : 2;

    public override (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next)
        => (leftFlanking, rightFlanking);

    public override int TryCreate(char c, int openLength, int closeLength,
        List<BitMarkdownNode> children, out BitMarkdownNode? node)
    {
        // GFM strikethrough requires runs of two on both sides.
        if (openLength >= 2 && closeLength >= 2)
        {
            var strike = new BitMarkdownStrikethroughNode();
            strike.Children.AddRange(children);
            node = strike;
            return 2;
        }

        if (Subscript && openLength >= 1 && closeLength >= 1)
        {
            var sub = new BitMarkdownSubscriptNode();
            sub.Children.AddRange(children);
            node = sub;
            return 1;
        }

        node = null;
        return 0;
    }
}
