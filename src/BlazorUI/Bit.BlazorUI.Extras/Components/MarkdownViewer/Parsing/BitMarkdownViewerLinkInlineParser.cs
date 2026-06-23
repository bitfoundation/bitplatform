using System.Text;

namespace Bit.BlazorUI;

/// <summary>Handles inline links <c>[text](url "title")</c> and images <c>![alt](url)</c>.</summary>
public sealed class BitMarkdownViewerLinkInlineParser : BitMarkdownViewerInlineParser
{
    public override char[] TriggerChars => new[] { '[', '!' };

    public override bool TryParse(BitMarkdownViewerInlineProcessor state)
    {
        string s = state.Text;
        int i = state.Pos;
        bool isImage = s[i] == '!';
        int bracket = isImage ? i + 1 : i;
        if (bracket >= s.Length || s[bracket] != '[') return false;

        int labelEnd = FindLabelEnd(s, bracket);
        if (labelEnd < 0) return false;

        int p = labelEnd + 1;
        if (p >= s.Length || s[p] != '(') return false;

        string label = s.Substring(bracket + 1, labelEnd - bracket - 1);

        int q = p + 1;
        if (!ParseDestination(s, ref q, out string url, out string? title)) return false;
        if (q >= s.Length || s[q] != ')') return false;

        if (isImage)
        {
            state.AppendNode(new BitMarkdownViewerImageNode
            {
                Url = BitMarkdownViewerUrlSanitizer.Sanitize(url, isImage: true),
                Title = title,
                Alt = BitMarkdownViewerInlineHelpers.PlainText(state.ParseInlines(label))
            });
        }
        else
        {
            var link = new BitMarkdownViewerLinkNode
            {
                Url = BitMarkdownViewerUrlSanitizer.Sanitize(url, isImage: false),
                Title = title
            };
            link.Children.AddRange(state.ParseInlines(label));
            state.AppendNode(link);
        }
        state.Pos = q + 1;
        return true;
    }

    private static int FindLabelEnd(string s, int openBracket)
    {
        int depth = 0;
        for (int i = openBracket; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\\') { i++; continue; }
            if (c == '[') depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    private static bool ParseDestination(string s, ref int i, out string url, out string? title)
    {
        url = string.Empty;
        title = null;
        int n = s.Length;
        while (i < n && (s[i] is ' ' or '\t' or '\n')) i++;

        var sb = new StringBuilder();
        if (i < n && s[i] == '<')
        {
            i++;
            while (i < n && s[i] != '>' && s[i] != '\n') sb.Append(s[i++]);
            if (i >= n || s[i] != '>') return false;
            i++;
        }
        else
        {
            int depth = 0;
            while (i < n)
            {
                char c = s[i];
                if (c == '\\' && i + 1 < n) { sb.Append(s[i + 1]); i += 2; continue; }
                if (c is ' ' or '\t' or '\n') break;
                if (c == '(') depth++;
                else if (c == ')')
                {
                    if (depth == 0) break;
                    depth--;
                }
                sb.Append(c);
                i++;
            }
        }
        url = sb.ToString();

        while (i < n && (s[i] is ' ' or '\t' or '\n')) i++;
        if (i < n && (s[i] is '"' or '\'' or '('))
        {
            char closeCh = s[i] == '(' ? ')' : s[i];
            i++;
            var tb = new StringBuilder();
            while (i < n && s[i] != closeCh)
            {
                if (s[i] == '\\' && i + 1 < n) { tb.Append(s[i + 1]); i += 2; continue; }
                tb.Append(s[i++]);
            }
            if (i >= n) return false;
            i++;
            title = tb.ToString();
            while (i < n && (s[i] is ' ' or '\t' or '\n')) i++;
        }

        return true;
    }
}
