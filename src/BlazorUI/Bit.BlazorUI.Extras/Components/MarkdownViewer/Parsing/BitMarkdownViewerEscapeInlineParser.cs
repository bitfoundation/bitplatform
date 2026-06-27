namespace Bit.BlazorUI;

/// <summary>Handles backslash escapes and backslash hard line breaks.</summary>
public sealed class BitMarkdownViewerEscapeInlineParser : BitMarkdownViewerInlineParser
{
    public override char[] TriggerChars => new[] { '\\' };

    public override bool TryParse(BitMarkdownViewerInlineProcessor state)
    {
        string s = state.Text;
        int i = state.Pos;
        if (i + 1 < s.Length && s[i + 1] == '\n')
        {
            state.AppendNode(new BitMarkdownViewerLineBreakNode { Hard = true });
            state.Pos = i + 2;
            return true;
        }
        if (i + 1 < s.Length && BitMarkdownViewerInlineHelpers.IsAsciiPunctuation(s[i + 1]))
        {
            state.AppendChar(s[i + 1]);
            state.Pos = i + 2;
            return true;
        }
        state.AppendChar('\\');
        state.Pos = i + 1;
        return true;
    }
}
