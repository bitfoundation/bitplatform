namespace Bit.BlazorUI;

/// <summary>Delimiter processor for <c>~~</c> strikethrough runs.</summary>
public sealed class BitMarkdownViewerStrikethroughDelimiterProcessor : BitMarkdownViewerDelimiterProcessor
{
    public override char[] Characters => new[] { '~' };
    public override int MinRunLength => 2;

    public override (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next)
        => (leftFlanking, rightFlanking);

    public override int TryCreate(char c, int openLength, int closeLength,
        List<BitMarkdownViewerMarkdownNode> children, out BitMarkdownViewerMarkdownNode? node)
    {
        // GFM strikethrough requires runs of two on both sides.
        if (openLength < 2 || closeLength < 2)
        {
            node = null;
            return 0;
        }
        var strike = new BitMarkdownViewerStrikethroughNode();
        strike.Children.AddRange(children);
        node = strike;
        return 2;
    }
}
