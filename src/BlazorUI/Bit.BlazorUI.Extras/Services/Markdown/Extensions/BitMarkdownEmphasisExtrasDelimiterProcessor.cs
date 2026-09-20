namespace Bit.BlazorUI;

/// <summary>
/// Delimiter processor for the three emphasis flavors that are not part of CommonMark or GFM but
/// are shared by every major processor: <c>^superscript^</c>, <c>++inserted++</c> and
/// <c>==highlighted==</c>. Subscript rides on <c>~</c>, which strikethrough already owns, so it
/// lives on <see cref="BitMarkdownStrikethroughDelimiterProcessor"/> instead.
/// </summary>
public sealed class BitMarkdownEmphasisExtrasDelimiterProcessor : BitMarkdownDelimiterProcessor
{
    public override char[] Characters => new[] { '^', '+', '=' };

    public override (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next)
        => (leftFlanking, rightFlanking);

    public override int TryCreate(char c, int openLength, int closeLength,
        List<BitMarkdownNode> children, out BitMarkdownNode? node)
    {
        // '^' is a single-character delimiter; '+' and '=' need a run of two, so that ordinary
        // prose ("1+2", "a=b") and the "+" list marker are never mistaken for emphasis.
        int required = c == '^' ? 1 : 2;
        if (openLength < required || closeLength < required)
        {
            node = null;
            return 0;
        }

        switch (c)
        {
            case '^':
                var sup = new BitMarkdownSuperscriptNode();
                sup.Children.AddRange(children);
                node = sup;
                break;

            case '+':
                var ins = new BitMarkdownInsertedNode();
                ins.Children.AddRange(children);
                node = ins;
                break;

            default:
                var mark = new BitMarkdownMarkedNode();
                mark.Children.AddRange(children);
                node = mark;
                break;
        }
        return required;
    }
}
