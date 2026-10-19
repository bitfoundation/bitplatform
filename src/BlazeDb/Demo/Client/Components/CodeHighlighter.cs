using System.Text;
using System.Text.Encodings.Web;

namespace Bit.BlazeDb.Demo.Client.Components;

/// <summary>
/// A small, self-contained tokenizer used to colour the code samples on this site.
/// It exists so the demo does not need to pull in a JavaScript highlighter.
/// </summary>
public static class CodeHighlighter
{
    private static readonly HashSet<string> Keywords =
    [
        "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char",
        "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double",
        "dynamic", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float",
        "for", "foreach", "get", "global", "goto", "if", "implicit", "in", "init", "int", "interface",
        "internal", "is", "lock", "long", "nameof", "new", "nint", "not", "null", "object", "operator",
        "or", "out", "override", "params", "partial", "private", "protected", "public", "readonly",
        "record", "ref", "required", "return", "sbyte", "sealed", "set", "short", "sizeof", "stackalloc",
        "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
        "ulong", "unchecked", "unsafe", "ushort", "using", "value", "var", "virtual", "void", "volatile",
        "when", "where", "while", "with", "yield",
    ];

    public static string ToHtml(string code, string language)
    {
        var sb = new StringBuilder(code.Length + 256);
        var normalized = code.Replace("\r\n", "\n").TrimEnd();

        switch (language?.ToLowerInvariant())
        {
            case "csharp":
            case "cs":
            case "c#":
            case "razor":
                Csharp(normalized, sb);
                break;
            case "xml":
            case "html":
                Markup(normalized, sb);
                break;
            case "json":
                Json(normalized, sb);
                break;
            default:
                Escape(sb, normalized);
                break;
        }

        return sb.ToString();
    }

    // ------------------------------------------------------------------ C#

    private static void Csharp(string s, StringBuilder sb)
    {
        var i = 0;
        var lineStart = true;

        while (i < s.Length)
        {
            var c = s[i];

            if (c == '\n')
            {
                sb.Append('\n');
                i++;
                lineStart = true;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                sb.Append(c);
                i++;
                continue;
            }

            // Comments
            if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                var end = s.IndexOf('\n', i);
                if (end < 0)
                {
                    end = s.Length;
                }
                Span(sb, "tok-c", s[i..end]);
                i = end;
                lineStart = false;
                continue;
            }

            if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
            {
                var end = s.IndexOf("*/", i + 2, StringComparison.Ordinal);
                end = end < 0 ? s.Length : end + 2;
                Span(sb, "tok-c", s[i..end]);
                i = end;
                lineStart = false;
                continue;
            }

            // Razor directives / transitions
            if (c == '@' && i + 1 < s.Length && (char.IsLetter(s[i + 1]) || s[i + 1] == '*'))
            {
                var end = i + 1;
                while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_' || s[end] == '.'))
                {
                    end++;
                }
                Span(sb, lineStart ? "tok-k" : "tok-f", s[i..end]);
                i = end;
                lineStart = false;
                continue;
            }

            // Strings, including verbatim and interpolated forms
            if (c == '"' || ((c == '@' || c == '$') && TryStringStart(s, i, out _)))
            {
                i = ReadString(s, i, sb);
                lineStart = false;
                continue;
            }

            if (c == '\'')
            {
                var end = i + 1;
                while (end < s.Length && s[end] != '\'')
                {
                    end += s[end] == '\\' ? 2 : 1;
                }
                end = Math.Min(end + 1, s.Length);
                Span(sb, "tok-s", s[i..end]);
                i = end;
                lineStart = false;
                continue;
            }

            // Attribute usage - a bracket that opens a line
            if (c == '[' && lineStart)
            {
                Span(sb, "tok-p", "[");
                i++;
                var end = i;
                while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_' || s[end] == '.'))
                {
                    end++;
                }
                if (end > i)
                {
                    Span(sb, "tok-a", s[i..end]);
                    i = end;
                }
                lineStart = false;
                continue;
            }

            // Numbers
            if (char.IsDigit(c))
            {
                var end = i;
                while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_' ||
                       (s[end] == '.' && end + 1 < s.Length && char.IsDigit(s[end + 1]))))
                {
                    end++;
                }
                Span(sb, "tok-n", s[i..end]);
                i = end;
                lineStart = false;
                continue;
            }

            // Identifiers
            if (char.IsLetter(c) || c == '_')
            {
                var end = i;
                while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_'))
                {
                    end++;
                }

                var word = s[i..end];
                var next = SkipSpaces(s, end);
                var cls = Keywords.Contains(word) ? "tok-k"
                    : next < s.Length && s[next] == '(' ? "tok-f"
                    : char.IsUpper(word[0]) ? "tok-t"
                    : null;

                if (cls is null)
                {
                    Escape(sb, word);
                }
                else
                {
                    Span(sb, cls, word);
                }

                i = end;
                lineStart = false;
                continue;
            }

            Span(sb, "tok-p", c.ToString());
            i++;
            lineStart = false;
        }
    }

    private static bool TryStringStart(string s, int i, out int quote)
    {
        quote = -1;
        var j = i;
        if (j < s.Length && (s[j] == '$' || s[j] == '@'))
        {
            j++;
        }
        if (j < s.Length && (s[j] == '$' || s[j] == '@'))
        {
            j++;
        }
        if (j < s.Length && s[j] == '"')
        {
            quote = j;
            return true;
        }
        return false;
    }

    private static int ReadString(string s, int i, StringBuilder sb)
    {
        var verbatim = false;
        var j = i;

        while (j < s.Length && (s[j] == '$' || s[j] == '@'))
        {
            verbatim |= s[j] == '@';
            j++;
        }

        if (j >= s.Length || s[j] != '"')
        {
            Span(sb, "tok-s", s[i..Math.Min(j + 1, s.Length)]);
            return Math.Min(j + 1, s.Length);
        }

        j++; // opening quote

        while (j < s.Length)
        {
            if (verbatim)
            {
                if (s[j] == '"')
                {
                    if (j + 1 < s.Length && s[j + 1] == '"')
                    {
                        j += 2;
                        continue;
                    }
                    j++;
                    break;
                }
                j++;
            }
            else
            {
                if (s[j] == '\\' && j + 1 < s.Length)
                {
                    j += 2;
                    continue;
                }
                if (s[j] == '"')
                {
                    j++;
                    break;
                }
                if (s[j] == '\n')
                {
                    break;
                }
                j++;
            }
        }

        Span(sb, "tok-s", s[i..j]);
        return j;
    }

    // -------------------------------------------------------------- Markup

    private static void Markup(string s, StringBuilder sb)
    {
        var i = 0;

        while (i < s.Length)
        {
            if (s[i] == '<')
            {
                if (s.AsSpan(i).StartsWith("<!--"))
                {
                    var close = s.IndexOf("-->", i, StringComparison.Ordinal);
                    close = close < 0 ? s.Length : close + 3;
                    Span(sb, "tok-c", s[i..close]);
                    i = close;
                    continue;
                }

                var end = s.IndexOf('>', i);
                end = end < 0 ? s.Length : end + 1;
                MarkupTag(s[i..end], sb);
                i = end;
                continue;
            }

            var next = s.IndexOf('<', i);
            next = next < 0 ? s.Length : next;
            Escape(sb, s[i..next]);
            i = next;
        }
    }

    private static void MarkupTag(string tag, StringBuilder sb)
    {
        var i = 0;

        while (i < tag.Length)
        {
            var c = tag[i];

            if (c is '<' or '>' or '/' or '=' or '?' or '!')
            {
                Span(sb, "tok-p", c.ToString());
                i++;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                sb.Append(c);
                i++;
                continue;
            }

            if (c == '"' || c == '\'')
            {
                var end = tag.IndexOf(c, i + 1);
                end = end < 0 ? tag.Length : end + 1;
                Span(sb, "tok-s", tag[i..end]);
                i = end;
                continue;
            }

            var stop = i;
            while (stop < tag.Length && !char.IsWhiteSpace(tag[stop]) && tag[stop] is not ('>' or '=' or '/' or '"'))
            {
                stop++;
            }

            var isTagName = i <= 1 || tag[i - 1] == '/';
            Span(sb, isTagName ? "tok-t" : "tok-a", tag[i..stop]);
            i = stop;
        }
    }

    // ---------------------------------------------------------------- JSON

    private static void Json(string s, StringBuilder sb)
    {
        var i = 0;

        while (i < s.Length)
        {
            var c = s[i];

            if (c == '"')
            {
                var end = i + 1;
                while (end < s.Length && s[end] != '"')
                {
                    end += s[end] == '\\' ? 2 : 1;
                }
                end = Math.Min(end + 1, s.Length);

                var after = SkipSpaces(s, end);
                Span(sb, after < s.Length && s[after] == ':' ? "tok-a" : "tok-s", s[i..end]);
                i = end;
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                var end = i + 1;
                while (end < s.Length && (char.IsDigit(s[end]) || s[end] is '.' or 'e' or 'E' or '+' or '-'))
                {
                    end++;
                }
                Span(sb, "tok-n", s[i..end]);
                i = end;
                continue;
            }

            if (char.IsLetter(c))
            {
                var end = i;
                while (end < s.Length && char.IsLetter(s[end]))
                {
                    end++;
                }
                Span(sb, "tok-k", s[i..end]);
                i = end;
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                sb.Append(c);
                i++;
                continue;
            }

            Span(sb, "tok-p", c.ToString());
            i++;
        }
    }

    // -------------------------------------------------------------- Helpers

    private static int SkipSpaces(string s, int i)
    {
        while (i < s.Length && (s[i] == ' ' || s[i] == '\t'))
        {
            i++;
        }
        return i;
    }

    private static void Span(StringBuilder sb, string cls, string text)
    {
        sb.Append("<span class=\"").Append(cls).Append("\">");
        Escape(sb, text);
        sb.Append("</span>");
    }

    private static void Escape(StringBuilder sb, string text) =>
        sb.Append(HtmlEncoder.Default.Encode(text));
}
