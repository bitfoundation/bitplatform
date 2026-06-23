using System.Text;

namespace Bit.BlazorUI;

/// <summary>Handles backslash escapes and backslash hard line breaks.</summary>
public sealed class EscapeInlineParser : InlineParser
{
    public override char[] TriggerChars => new[] { '\\' };

    public override bool TryParse(InlineProcessor state)
    {
        string s = state.Text;
        int i = state.Pos;
        if (i + 1 < s.Length && s[i + 1] == '\n')
        {
            state.AppendNode(new LineBreakNode { Hard = true });
            state.Pos = i + 2;
            return true;
        }
        if (i + 1 < s.Length && InlineHelpers.IsAsciiPunctuation(s[i + 1]))
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

/// <summary>Handles inline code spans delimited by runs of backticks.</summary>
public sealed class CodeSpanInlineParser : InlineParser
{
    public override char[] TriggerChars => new[] { '`' };

    public override bool TryParse(InlineProcessor state)
    {
        string s = state.Text;
        int start = state.Pos;
        int run = InlineHelpers.CountRun(s, start, '`');
        int close = FindClosing(s, start + run, run);
        if (close < 0) return false;

        string content = s.Substring(start + run, close - (start + run));
        state.AppendNode(new CodeSpanNode { Content = Normalize(content) });
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
                int run = InlineHelpers.CountRun(s, i, '`');
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

/// <summary>Handles CommonMark angle-bracket autolinks: <c>&lt;https://...&gt;</c>.</summary>
public sealed class AutolinkInlineParser : InlineParser
{
    public override char[] TriggerChars => new[] { '<' };

    public override bool TryParse(InlineProcessor state)
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

    private static void Emit(InlineProcessor state, string href, string label, int close)
    {
        var link = new LinkNode { Url = UrlSanitizer.Sanitize(href, isImage: false) };
        link.Children.Add(new TextNode(label));
        state.AppendNode(link);
        state.Pos = close + 1;
    }
}

/// <summary>Handles inline links <c>[text](url "title")</c> and images <c>![alt](url)</c>.</summary>
public sealed class LinkInlineParser : InlineParser
{
    public override char[] TriggerChars => new[] { '[', '!' };

    public override bool TryParse(InlineProcessor state)
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
            state.AppendNode(new ImageNode
            {
                Url = UrlSanitizer.Sanitize(url, isImage: true),
                Title = title,
                Alt = InlineHelpers.PlainText(state.ParseInlines(label))
            });
        }
        else
        {
            var link = new LinkNode
            {
                Url = UrlSanitizer.Sanitize(url, isImage: false),
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

/// <summary>Turns newlines into soft breaks, or hard breaks after two trailing spaces.</summary>
public sealed class LineBreakInlineParser : InlineParser
{
    public override char[] TriggerChars => new[] { '\n' };

    public override bool TryParse(InlineProcessor state)
    {
        int removed = state.TrimPendingTrailingSpaces();
        state.AppendNode(new LineBreakNode { Hard = removed >= 2 });
        state.Pos++;
        return true;
    }
}

/// <summary>
/// Core emphasis processor for <c>*</c> and <c>_</c>, producing
/// <see cref="EmphasisNode"/> / <see cref="StrongNode"/>.
/// </summary>
public sealed class EmphasisDelimiterProcessor : DelimiterProcessor
{
    public override char[] Characters => new[] { '*', '_' };

    public override (bool canOpen, bool canClose) GetFlanking(
        char c, bool leftFlanking, bool rightFlanking, char prev, char next)
    {
        bool prevPunct = prev != '\0' && InlineHelpers.IsPunctuation(prev);
        bool nextPunct = next != '\0' && InlineHelpers.IsPunctuation(next);

        if (c == '_')
        {
            return (leftFlanking && (!rightFlanking || prevPunct),
                    rightFlanking && (!leftFlanking || nextPunct));
        }
        return (leftFlanking, rightFlanking);
    }

    public override int TryCreate(char c, int openLength, int closeLength,
        List<MarkdownNode> children, out MarkdownNode? node)
    {
        int used = openLength >= 2 && closeLength >= 2 ? 2 : 1;
        if (used == 2)
        {
            var strong = new StrongNode();
            strong.Children.AddRange(children);
            node = strong;
        }
        else
        {
            var em = new EmphasisNode();
            em.Children.AddRange(children);
            node = em;
        }
        return used;
    }
}
