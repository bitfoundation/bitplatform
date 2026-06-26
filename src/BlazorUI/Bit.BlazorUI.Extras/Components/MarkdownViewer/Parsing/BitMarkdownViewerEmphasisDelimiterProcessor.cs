namespace Bit.BlazorUI;

/// <summary>
/// Core emphasis processor for <c>*</c> and <c>_</c>, producing
/// <see cref="BitMarkdownViewerEmphasisNode"/> / <see cref="BitMarkdownViewerStrongNode"/>.
/// </summary>
public sealed class BitMarkdownViewerEmphasisDelimiterProcessor : BitMarkdownViewerDelimiterProcessor
{
    public override char[] Characters => new[] { '*', '_' };

    // The rule of three is an emphasis-specific constraint.
    public override bool AppliesRuleOfThree => true;

    public override (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next)
    {
        bool prevPunct = prev != '\0' && BitMarkdownViewerInlineHelpers.IsPunctuation(prev);
        bool nextPunct = next != '\0' && BitMarkdownViewerInlineHelpers.IsPunctuation(next);

        if (c == '_')
        {
            return (leftFlanking && (!rightFlanking || prevPunct),
                    rightFlanking && (!leftFlanking || nextPunct));
        }
        return (leftFlanking, rightFlanking);
    }

    public override int TryCreate(char c, int openLength, int closeLength,
        List<BitMarkdownViewerMarkdownNode> children, out BitMarkdownViewerMarkdownNode? node)
    {
        int used = openLength >= 2 && closeLength >= 2 ? 2 : 1;
        if (used == 2)
        {
            var strong = new BitMarkdownViewerStrongNode();
            strong.Children.AddRange(children);
            node = strong;
        }
        else
        {
            var em = new BitMarkdownViewerEmphasisNode();
            em.Children.AddRange(children);
            node = em;
        }
        return used;
    }
}
