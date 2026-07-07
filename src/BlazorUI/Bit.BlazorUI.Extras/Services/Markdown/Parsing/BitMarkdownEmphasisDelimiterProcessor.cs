namespace Bit.BlazorUI;

/// <summary>
/// Core emphasis processor for <c>*</c> and <c>_</c>, producing
/// <see cref="BitMarkdownEmphasisNode"/> / <see cref="BitMarkdownStrongNode"/>.
/// </summary>
public sealed class BitMarkdownEmphasisDelimiterProcessor : BitMarkdownDelimiterProcessor
{
    public override char[] Characters => new[] { '*', '_' };

    // The rule of three is an emphasis-specific constraint.
    public override bool AppliesRuleOfThree => true;

    public override (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next)
    {
        bool prevPunct = prev != '\0' && BitMarkdownInlineHelpers.IsPunctuation(prev);
        bool nextPunct = next != '\0' && BitMarkdownInlineHelpers.IsPunctuation(next);

        if (c == '_')
        {
            return (leftFlanking && (!rightFlanking || prevPunct),
                    rightFlanking && (!leftFlanking || nextPunct));
        }
        return (leftFlanking, rightFlanking);
    }

    public override int TryCreate(char c, int openLength, int closeLength,
        List<BitMarkdownNode> children, out BitMarkdownNode? node)
    {
        int used = openLength >= 2 && closeLength >= 2 ? 2 : 1;
        if (used == 2)
        {
            var strong = new BitMarkdownStrongNode();
            strong.Children.AddRange(children);
            node = strong;
        }
        else
        {
            var em = new BitMarkdownEmphasisNode();
            em.Children.AddRange(children);
            node = em;
        }
        return used;
    }
}
