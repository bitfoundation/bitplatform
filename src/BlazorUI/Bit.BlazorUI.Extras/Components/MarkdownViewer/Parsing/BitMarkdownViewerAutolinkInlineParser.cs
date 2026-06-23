namespace Bit.BlazorUI;

/// <summary>Handles CommonMark angle-bracket autolinks: <c>&lt;https://...&gt;</c>.</summary>
public sealed class BitMarkdownViewerAutolinkInlineParser : BitMarkdownViewerInlineParser
{
    public override char[] TriggerChars => new[] { '<' };

    public override bool TryParse(BitMarkdownViewerInlineProcessor state)
    {
        string s = state.Text;
        int start = state.Pos;
        int close = s.IndexOf('>', start + 1);
        if (close < 0) return false;

        string inner = s.Substring(start + 1, close - start - 1);
        if (inner.Length == 0 || inner.Contains(' ') || inner.Contains('<')) return false;

        int colon = inner.IndexOf(':');
        if (colon > 0)
        {
            string scheme = inner[..colon];
            if (char.IsLetter(scheme[0])
                && scheme.All(ch => char.IsLetterOrDigit(ch) || ch is '+' or '.' or '-'))
            {
                Emit(state, inner, inner, close);
                return true;
            }
        }

        if (inner.Contains('@') && !inner.Contains(':'))
        {
            Emit(state, "mailto:" + inner, inner, close);
            return true;
        }

        return false;
    }

    private static void Emit(BitMarkdownViewerInlineProcessor state, string href, string label, int close)
    {
        var link = new BitMarkdownViewerLinkNode { Url = BitMarkdownViewerUrlSanitizer.Sanitize(href, isImage: false) };
        link.Children.Add(new BitMarkdownViewerTextNode(label));
        state.AppendNode(link);
        state.Pos = close + 1;
    }
}
