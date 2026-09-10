namespace Bit.BlazorUI;

/// <summary>
/// Handles inline mathematics: <c>$a^2 + b^2$</c>. The rules are the ones every processor
/// converged on, and they exist to keep prices out of equations: the opening <c>$</c> must be
/// followed by a non-space, the closing one preceded by a non-space, and a digit may not follow
/// the closing delimiter - so "it costs $5 and $10" is text, not math.
/// </summary>
public sealed class BitMarkdownMathInlineParser : BitMarkdownInlineParser
{
    public override char[] TriggerChars => new[] { '$' };

    public override bool TryParse(BitMarkdownInlineProcessor state)
    {
        string s = state.Text;
        int start = state.Pos;

        int open = BitMarkdownInlineHelpers.CountRun(s, start, '$');
        // A run of three or more is not a delimiter; "$$" opens a display run inside a paragraph.
        if (open > 2) return false;

        int contentStart = start + open;
        if (contentStart >= s.Length) return false;
        if (open == 1 && (s[contentStart] == ' ' || s[contentStart] == '\n')) return false;

        int i = contentStart;
        while (i < s.Length)
        {
            if (s[i] == '\\') { i += 2; continue; }
            if (s[i] != '$') { i++; continue; }

            int close = BitMarkdownInlineHelpers.CountRun(s, i, '$');
            if (close < open) { i += close; continue; }

            if (i == contentStart) return false;
            if (open == 1 && s[i - 1] == ' ') { i += close; continue; }
            // "$100 and $200" would otherwise read as math: a digit right after the closing
            // delimiter means these were prices all along.
            if (open == 1 && i + close < s.Length && char.IsAsciiDigit(s[i + close])) return false;

            state.AppendNode(new BitMarkdownMathNode
            {
                Content = s[contentStart..i],
                Display = open == 2
            });
            state.Pos = i + open;
            return true;
        }

        return false;
    }
}
