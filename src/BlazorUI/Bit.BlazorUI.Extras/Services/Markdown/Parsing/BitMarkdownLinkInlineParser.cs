using System.Text;

namespace Bit.BlazorUI;

/// <summary>Handles inline links <c>[text](url "title")</c> and images <c>![alt](url)</c>.</summary>
public sealed class BitMarkdownLinkInlineParser : BitMarkdownInlineParser
{
    public override char[] TriggerChars => new[] { '[', '!' };

    public override bool TryParse(BitMarkdownInlineProcessor state)
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
            state.AppendNode(new BitMarkdownImageNode
            {
                Url = BitMarkdownUrlSanitizer.Sanitize(url, isImage: true),
                Title = title,
                Alt = BitMarkdownInlineHelpers.PlainText(state.ParseInlines(label))
            });
        }
        else
        {
            var link = new BitMarkdownLinkNode
            {
                Url = BitMarkdownUrlSanitizer.Sanitize(url, isImage: false),
                Title = title
            };
            // A link may not contain another link; unwrap any nested links so the
            // inner link's content survives as plain inline text instead.
            link.Children.AddRange(RemoveNestedLinks(state.ParseInlines(label)));
            state.AppendNode(link);
        }
        state.Pos = q + 1;
        return true;
    }

    // Recursively replaces any nested link node with its (also unwrapped) children,
    // ensuring a link's label can never contain another link.
    private static List<BitMarkdownNode> RemoveNestedLinks(IEnumerable<BitMarkdownNode> nodes)
    {
        var result = new List<BitMarkdownNode>();
        foreach (var node in nodes)
        {
            if (node is BitMarkdownLinkNode nested)
            {
                result.AddRange(RemoveNestedLinks(nested.Children));
                continue;
            }
            foreach (var list in node.ChildLists)
            {
                var cleaned = RemoveNestedLinks(list);
                list.Clear();
                foreach (var child in cleaned) list.Add(child);
            }
            result.Add(node);
        }
        return result;
    }

    private static int FindLabelEnd(string s, int openBracket)
    {
        int depth = 0;
        int i = openBracket;
        while (i < s.Length)
        {
            char c = s[i];
            if (c == '\\') { i += 2; continue; }
            if (c == '`')
            {
                // Skip an inline code span so that ']' enclosed by backticks does
                // not prematurely terminate the link label.
                i = SkipCodeSpan(s, i);
                continue;
            }
            if (c == '[') depth++;
            else if (c == ']')
            {
                depth--;
                if (depth == 0) return i;
            }
            i++;
        }
        return -1;
    }

    // Given the index of a backtick, returns the index just past a matching
    // closing code-span run. If no closing run of equal length exists, the
    // backticks are treated as literal text and the index just past the opening
    // run is returned.
    private static int SkipCodeSpan(string s, int i)
    {
        int start = i;
        int openLen = 0;
        while (i < s.Length && s[i] == '`') { i++; openLen++; }

        int j = i;
        while (j < s.Length)
        {
            if (s[j] == '`')
            {
                int closeLen = 0;
                while (j < s.Length && s[j] == '`') { j++; closeLen++; }
                if (closeLen == openLen) return j;
            }
            else j++;
        }
        return start + openLen;
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
