namespace Bit.BlazorUI;

/// <summary>Delimiter processor for <c>~~</c> strikethrough runs.</summary>
public sealed class BitMarkdownStrikethroughDelimiterProcessor : BitMarkdownDelimiterProcessor
{
    public override char[] Characters => new[] { '~' };
    public override int MinRunLength => 2;

    public override (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next)
        => (leftFlanking, rightFlanking);

    public override int TryCreate(char c, int openLength, int closeLength,
        List<BitMarkdownNode> children, out BitMarkdownNode? node)
    {
        // GFM strikethrough requires runs of two on both sides.
        if (openLength < MinRunLength || closeLength < MinRunLength)
        {
            node = null;
            return 0;
        }
        var strike = new BitMarkdownStrikethroughNode();
        strike.Children.AddRange(children);
        node = strike;
        return MinRunLength;
    }
}
