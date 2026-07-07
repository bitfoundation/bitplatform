namespace Bit.BlazorUI;

/// <summary>Handles inline code spans delimited by runs of backticks.</summary>
public sealed class BitMarkdownCodeSpanInlineParser : BitMarkdownInlineParser
{
    public override char[] TriggerChars => new[] { '`' };

    public override bool TryParse(BitMarkdownInlineProcessor state)
    {
        string s = state.Text;
        int start = state.Pos;
        int run = BitMarkdownInlineHelpers.CountRun(s, start, '`');
        int close = FindClosing(s, start + run, run);
        if (close < 0) return false;

        string content = s.Substring(start + run, close - (start + run));
        state.AppendNode(new BitMarkdownCodeSpanNode { Content = Normalize(content) });
        state.Pos = close + run;
        return true;
    }

    private static int FindClosing(string s, int from, int runLen)
    {
        int i = from;
        while (i < s.Length)
        {
            if (s[i] == '`')
            {
                int run = BitMarkdownInlineHelpers.CountRun(s, i, '`');
                if (run == runLen) return i;
                i += run;
            }
            else i++;
        }
        return -1;
    }

    private static string Normalize(string content)
    {
        content = content.Replace("\r\n", " ").Replace('\n', ' ');
        if (content.Length > 2 && content[0] == ' ' && content[^1] == ' '
            && content.Any(ch => ch != ' '))
        {
            content = content[1..^1];
        }
        return content;
    }
}
