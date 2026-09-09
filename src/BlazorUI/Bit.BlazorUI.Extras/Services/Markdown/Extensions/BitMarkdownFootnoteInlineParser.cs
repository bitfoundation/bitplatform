namespace Bit.BlazorUI;

/// <summary>
/// Handles footnote references (<c>[^label]</c>). Only a label the document actually defines
/// becomes a reference; anything else stays the text it was written as.
/// </summary>
public sealed class BitMarkdownFootnoteInlineParser : BitMarkdownInlineParser
{
    public override char[] TriggerChars => new[] { '[' };

    // Offered the '[' before the link parser, whose label syntax "[^1]" also matches.
    public override int Order => 10;

    public override bool TryParse(BitMarkdownInlineProcessor state)
    {
        string s = state.Text;
        int i = state.Pos;
        if (i + 1 >= s.Length || s[i] != '[' || s[i + 1] != '^') return false;

        int labelEnd = BitMarkdownLinkHelpers.FindLabelEnd(s, i);
        if (labelEnd < 0) return false;

        string label = BitMarkdownLinkHelpers.NormalizeLabel(s.Substring(i + 1, labelEnd - i - 1));
        if (label.Length <= 1 || label.Length > BitMarkdownLinkHelpers.MaxLabelLength) return false;

        // A reference is only meaningful when a "[^label]:" definition exists; the pre-scan
        // knows every one of them, including those written after this point.
        if (state.HasReferenceLabel(label) is false) return false;

        // "[^1]:" at this position is the definition itself, not a reference to it.
        if (labelEnd + 1 < s.Length && s[labelEnd + 1] == ':') return false;

        state.AppendNode(new BitMarkdownFootnoteReferenceNode
        {
            Label = label,
            RawText = s[i..(labelEnd + 1)]
        });
        state.Pos = labelEnd + 1;
        return true;
    }
}
