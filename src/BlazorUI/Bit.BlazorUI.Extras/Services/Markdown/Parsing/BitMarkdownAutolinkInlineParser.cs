namespace Bit.BlazorUI;

/// <summary>Handles CommonMark angle-bracket autolinks: <c>&lt;https://...&gt;</c>.</summary>
public sealed partial class BitMarkdownAutolinkInlineParser : BitMarkdownInlineParser
{
    public override char[] TriggerChars => new[] { '<' };

    // CommonMark email autolink grammar. Only a token matching this is turned into a
    // mailto link; anything else inside <...> stays plain text.
    [System.Text.RegularExpressions.GeneratedRegex(
        @"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        System.Text.RegularExpressions.RegexOptions.None,
        matchTimeoutMilliseconds: 1000)]
    private static partial System.Text.RegularExpressions.Regex EmailAutolink();

    public override bool TryParse(BitMarkdownInlineProcessor state)
    {
        string s = state.Text;
        int start = state.Pos;
        int close = s.IndexOf('>', start + 1);
        if (close < 0) return false;

        string inner = s.Substring(start + 1, close - start - 1);
        if (inner.Length == 0 || inner.Any(char.IsWhiteSpace) || inner.Contains('<')) return false;

        int colon = inner.IndexOf(':');
        if (colon > 1)
        {
            string scheme = inner[..colon];
            if (char.IsLetter(scheme[0])
                && scheme.All(ch => char.IsLetterOrDigit(ch) || ch is '+' or '.' or '-'))
            {
                Emit(state, inner, inner, close);
                return true;
            }
        }

        bool isEmail;
        try
        {
            isEmail = EmailAutolink().IsMatch(inner);
        }
        catch (System.Text.RegularExpressions.RegexMatchTimeoutException)
        {
            // Pathological input: treat as a non-match rather than hang.
            return false;
        }
        if (isEmail)
        {
            Emit(state, "mailto:" + inner, inner, close);
            return true;
        }

        return false;
    }

    private static void Emit(BitMarkdownInlineProcessor state, string href, string label, int close)
    {
        var link = new BitMarkdownLinkNode { Url = BitMarkdownUrlSanitizer.Sanitize(href, isImage: false) };
        link.Children.Add(new BitMarkdownTextNode(label));
        state.AppendNode(link);
        state.Pos = close + 1;
    }
}
