using System.Text;
using Microsoft.AspNetCore.Components;

namespace Bit.Blex.Demo.Client.Components;

/// <summary>
/// A small, dependency-free syntax highlighter for the code samples on this site.
/// </summary>
/// <remarks>
/// The docs site deliberately ships no third-party JS: highlighting a handful of C#, Razor, XML
/// and JSON snippets does not justify a CDN dependency (and a CDN would break offline and
/// strict-CSP hosting). This is a pragmatic scanner, not a parser -- it recognises comments,
/// strings, numbers, keywords, attributes and markup, which is everything the samples need.
/// </remarks>
public static class CodeHighlighter
{
    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch",
        "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
        "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed",
        "float", "for", "foreach", "get", "global", "goto", "if", "implicit", "in", "init", "int",
        "interface", "internal", "is", "lock", "long", "nameof", "namespace", "new", "not",
        "null", "object", "operator", "out", "override", "params", "partial", "private",
        "protected", "public", "readonly", "record", "ref", "required", "return", "sbyte",
        "sealed", "set", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch",
        "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "var", "virtual", "void", "volatile", "when", "where", "while", "with", "yield",
    };

    private static readonly HashSet<string> RazorDirectives = new(StringComparer.Ordinal)
    {
        "page", "using", "inject", "inherits", "implements", "layout", "namespace", "typeparam",
        "attribute", "code", "bind", "ref", "key", "onclick", "oninput", "onchange", "rendermode",
        "preservewhitespace", "functions",
    };

    /// <summary>Highlights <paramref name="code"/> for the given language.</summary>
    /// <param name="code">The raw snippet.</param>
    /// <param name="language">
    /// <c>csharp</c>, <c>razor</c>, <c>xml</c>, <c>json</c>, <c>bash</c> or <c>text</c>.
    /// Anything unrecognised is rendered as escaped plain text.
    /// </param>
    public static MarkupString Highlight(string code, string language)
    {
        var sb = new StringBuilder(code.Length * 2);
        switch (language?.ToLowerInvariant())
        {
            case "csharp" or "cs" or "c#":
                ScanCSharp(code, 0, code.Length, sb);
                break;
            case "razor" or "html" or "cshtml":
                ScanRazor(code, sb);
                break;
            case "xml" or "csproj":
                ScanMarkup(code, 0, code.Length, sb, razorAware: false);
                break;
            case "json":
                ScanJson(code, sb);
                break;
            case "bash" or "sh" or "shell" or "console":
                ScanShell(code, sb);
                break;
            default:
                Append(sb, code);
                break;
        }

        return new MarkupString(sb.ToString());
    }

    // ---------------------------------------------------------------- C#

    private static void ScanCSharp(string s, int start, int end, StringBuilder sb)
    {
        var i = start;
        var atLineStart = true;

        while (i < end)
        {
            var c = s[i];

            // Comments
            if (c == '/' && i + 1 < end && s[i + 1] == '/')
            {
                var j = i;
                while (j < end && s[j] != '\n')
                    j++;
                Emit(sb, "c-com", s, i, j);
                i = j;
                continue;
            }

            if (c == '/' && i + 1 < end && s[i + 1] == '*')
            {
                var j = i + 2;
                while (j + 1 < end && !(s[j] == '*' && s[j + 1] == '/'))
                    j++;
                j = Math.Min(end, j + 2);
                Emit(sb, "c-com", s, i, j);
                i = j;
                continue;
            }

            // Strings (raw, verbatim, interpolated and plain all collapse to one token)
            if (c == '"' || (c is '@' or '$' && i + 1 < end && (s[i + 1] == '"' || (i + 2 < end && s[i + 1] is '@' or '$' && s[i + 2] == '"'))))
            {
                var j = ScanCSharpString(s, i, end);
                Emit(sb, "c-str", s, i, j);
                i = j;
                continue;
            }

            if (c == '\'')
            {
                var j = i + 1;
                while (j < end && s[j] != '\'')
                    j += s[j] == '\\' ? 2 : 1;
                j = Math.Min(end, j + 1);
                Emit(sb, "c-str", s, i, j);
                i = j;
                continue;
            }

            // Attributes: a '[' that opens a line is [BlexStore], [BlexState], ... rather than an indexer.
            if (c == '[' && atLineStart)
            {
                var j = i + 1;
                var depth = 1;
                while (j < end && depth > 0)
                {
                    if (s[j] == '[')
                        depth++;
                    else if (s[j] == ']')
                        depth--;
                    j++;
                }

                // The outer span colours the brackets and punctuation; nested spans from the
                // recursive scan colour the attribute's own name and arguments.
                sb.Append("<span class=\"c-attr\">[");
                ScanCSharp(s, i + 1, Math.Max(i + 1, j - 1), sb);
                sb.Append("]</span>");
                i = j;
                atLineStart = false;
                continue;
            }

            // Numbers
            if (char.IsDigit(c) && (i == start || !IsIdentPart(s[i - 1])))
            {
                var j = i;
                while (j < end && (char.IsLetterOrDigit(s[j]) || s[j] == '.' || s[j] == '_'))
                    j++;
                Emit(sb, "c-num", s, i, j);
                i = j;
                atLineStart = false;
                continue;
            }

            // Identifiers
            if (IsIdentStart(c))
            {
                var j = i;
                while (j < end && IsIdentPart(s[j]))
                    j++;

                var word = s[i..j];
                var k = j;
                while (k < end && (s[k] == ' ' || s[k] == '\t'))
                    k++;

                if (CSharpKeywords.Contains(word))
                    Emit(sb, "c-kw", s, i, j);
                else if (k < end && s[k] == '(')
                    Emit(sb, "c-fn", s, i, j);
                else if (char.IsUpper(word[0]))
                    Emit(sb, "c-type", s, i, j);
                else
                    Append(sb, word);

                i = j;
                atLineStart = false;
                continue;
            }

            if (c == '\n')
                atLineStart = true;
            else if (!char.IsWhiteSpace(c))
                atLineStart = false;

            Append(sb, c);
            i++;
        }
    }

    private static int ScanCSharpString(string s, int i, int end)
    {
        var verbatim = false;
        while (i < end && (s[i] == '@' || s[i] == '$'))
        {
            verbatim |= s[i] == '@';
            i++;
        }

        // Raw string literal: """ ... """
        if (i + 2 < end && s[i] == '"' && s[i + 1] == '"' && s[i + 2] == '"')
        {
            var quotes = 0;
            while (i < end && s[i] == '"')
            {
                quotes++;
                i++;
            }

            var run = 0;
            while (i < end && run < quotes)
            {
                run = s[i] == '"' ? run + 1 : 0;
                i++;
            }

            return i;
        }

        i++; // opening quote
        while (i < end)
        {
            if (s[i] == '\\' && !verbatim)
            {
                i += 2;
                continue;
            }

            if (s[i] == '"')
            {
                // In a verbatim string "" is an escaped quote.
                if (verbatim && i + 1 < end && s[i + 1] == '"')
                {
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            if (!verbatim && s[i] == '\n')
                return i;

            i++;
        }

        return end;
    }

    // ---------------------------------------------------------------- Razor

    private static void ScanRazor(string s, StringBuilder sb)
    {
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];

            // @* razor comment *@
            if (c == '@' && i + 1 < s.Length && s[i + 1] == '*')
            {
                var j = s.IndexOf("*@", i + 2, StringComparison.Ordinal);
                j = j < 0 ? s.Length : j + 2;
                Emit(sb, "c-com", s, i, j);
                i = j;
                continue;
            }

            if (c == '@' && i + 1 < s.Length && s[i + 1] == '@')
            {
                Append(sb, "@@");
                i += 2;
                continue;
            }

            if (c == '@' && i + 1 < s.Length)
            {
                var next = s[i + 1];

                // @{ ... } and @code { ... } -- brace-matched C# regions
                if (next == '{')
                {
                    var close = MatchBrace(s, i + 1);
                    sb.Append("<span class=\"c-razor\">@</span>");
                    Append(sb, '{');
                    ScanCSharp(s, i + 2, close, sb);
                    if (close < s.Length)
                        Append(sb, '}');
                    i = close + 1;
                    continue;
                }

                if (next == '(')
                {
                    var close = MatchParen(s, i + 1);
                    sb.Append("<span class=\"c-razor\">@</span>");
                    Append(sb, '(');
                    ScanCSharp(s, i + 2, close, sb);
                    if (close < s.Length)
                        Append(sb, ')');
                    i = close + 1;
                    continue;
                }

                if (IsIdentStart(next))
                {
                    var j = i + 1;
                    while (j < s.Length && IsIdentPart(s[j]))
                        j++;

                    var word = s[(i + 1)..j];
                    if (RazorDirectives.Contains(word))
                    {
                        sb.Append("<span class=\"c-razor\">");
                        Append(sb, s[i..j]);
                        sb.Append("</span>");

                        // @code { ... } carries a C# body.
                        if (word is "code" or "functions")
                        {
                            var open = j;
                            while (open < s.Length && s[open] != '{')
                                open++;
                            if (open < s.Length)
                            {
                                Append(sb, s[j..(open + 1)]);
                                var close = MatchBrace(s, open);
                                ScanCSharp(s, open + 1, close, sb);
                                if (close < s.Length)
                                    Append(sb, '}');
                                i = close + 1;
                                continue;
                            }
                        }
                        else if (word is "using" or "page" or "inject" or "inherits" or "implements" or "layout" or "attribute" or "namespace" or "typeparam")
                        {
                            // Rest of the directive line is C#-ish / a route literal.
                            var eol = s.IndexOf('\n', j);
                            eol = eol < 0 ? s.Length : eol;
                            ScanCSharp(s, j, eol, sb);
                            i = eol;
                            continue;
                        }

                        i = j;
                        continue;
                    }

                    // @Expression, possibly with a member chain / call.
                    var k = j;
                    while (k < s.Length && (s[k] == '.' || s[k] == '(' || IsIdentPart(s[k])))
                    {
                        if (s[k] == '(')
                        {
                            k = MatchParen(s, k) + 1;
                            break;
                        }

                        k++;
                    }

                    sb.Append("<span class=\"c-razor\">@</span>");
                    ScanCSharp(s, i + 1, Math.Min(k, s.Length), sb);
                    i = Math.Min(k, s.Length);
                    continue;
                }
            }

            // Markup tag
            if (c == '<' && i + 1 < s.Length && (IsIdentStart(s[i + 1]) || s[i + 1] == '/' || s[i + 1] == '!'))
            {
                var j = ScanTag(s, i, sb);
                i = j;
                continue;
            }

            Append(sb, c);
            i++;
        }
    }

    /// <summary>Renders one markup tag, highlighting the name, attribute names and values.</summary>
    private static int ScanTag(string s, int i, StringBuilder sb)
    {
        if (i + 1 >= s.Length)
        {
            Append(sb, s[i]);
            return i + 1;
        }

        // Comment
        if (i + 3 < s.Length && s[i + 1] == '!' && s[i + 2] == '-' && s[i + 3] == '-')
        {
            var e = s.IndexOf("-->", i, StringComparison.Ordinal);
            e = e < 0 ? s.Length : e + 3;
            Emit(sb, "c-com", s, i, e);
            return e;
        }

        var j = i + 1;
        if (j < s.Length && s[j] == '/')
            j++;
        while (j < s.Length && (IsIdentPart(s[j]) || s[j] == '-' || s[j] == '.' || s[j] == ':' || s[j] == '!'))
            j++;

        sb.Append("<span class=\"c-punc\">");
        Append(sb, s[i..(s[i + 1] == '/' ? i + 2 : i + 1)]);
        sb.Append("</span><span class=\"c-tag\">");
        Append(sb, s[(s[i + 1] == '/' ? i + 2 : i + 1)..j]);
        sb.Append("</span>");

        // Attributes until '>'
        while (j < s.Length && s[j] != '>')
        {
            if (char.IsWhiteSpace(s[j]))
            {
                Append(sb, s[j]);
                j++;
                continue;
            }

            // Razor constructs inside a tag (@onclick="...", @bind, @ref)
            if (s[j] == '@' || IsIdentStart(s[j]))
            {
                var k = j;
                if (s[k] == '@')
                    k++;
                while (k < s.Length && (IsIdentPart(s[k]) || s[k] == '-' || s[k] == ':' || s[k] == '.'))
                    k++;
                Emit(sb, s[j] == '@' ? "c-razor" : "c-attrname", s, j, k);
                j = k;
                continue;
            }

            if (s[j] == '=')
            {
                Append(sb, '=');
                j++;
                continue;
            }

            if (s[j] == '"' || s[j] == '\'')
            {
                var quote = s[j];
                var k = j + 1;
                while (k < s.Length && s[k] != quote)
                    k++;
                k = Math.Min(s.Length, k + 1);

                // An attribute value containing @ is a Razor expression; highlight it as code.
                var inner = s[j..k];
                if (inner.Contains('@'))
                {
                    sb.Append("<span class=\"c-str\">");
                    Append(sb, quote);
                    sb.Append("</span>");
                    ScanRazor(s[(j + 1)..(k - 1)], sb);
                    sb.Append("<span class=\"c-str\">");
                    Append(sb, quote);
                    sb.Append("</span>");
                }
                else
                {
                    Emit(sb, "c-str", s, j, k);
                }

                j = k;
                continue;
            }

            if (s[j] == '/')
            {
                Append(sb, '/');
                j++;
                continue;
            }

            Append(sb, s[j]);
            j++;
        }

        if (j < s.Length)
        {
            sb.Append("<span class=\"c-punc\">&gt;</span>");
            j++;
        }

        return j;
    }

    // ---------------------------------------------------------------- XML / JSON / shell

    private static void ScanMarkup(string s, int start, int end, StringBuilder sb, bool razorAware)
    {
        _ = razorAware;
        var i = start;
        while (i < end)
        {
            if (s[i] == '<')
            {
                i = ScanTag(s, i, sb);
                continue;
            }

            Append(sb, s[i]);
            i++;
        }
    }

    private static void ScanJson(string s, StringBuilder sb)
    {
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (c == '"')
            {
                var j = i + 1;
                while (j < s.Length && s[j] != '"')
                    j += s[j] == '\\' ? 2 : 1;
                j = Math.Min(s.Length, j + 1);

                var k = j;
                while (k < s.Length && char.IsWhiteSpace(s[k]))
                    k++;
                Emit(sb, k < s.Length && s[k] == ':' ? "c-attrname" : "c-str", s, i, j);
                i = j;
                continue;
            }

            if (char.IsDigit(c) || (c == '-' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
            {
                var j = i + 1;
                while (j < s.Length && (char.IsDigit(s[j]) || s[j] is '.' or 'e' or 'E' or '+' or '-'))
                    j++;
                Emit(sb, "c-num", s, i, j);
                i = j;
                continue;
            }

            if (IsIdentStart(c))
            {
                var j = i;
                while (j < s.Length && IsIdentPart(s[j]))
                    j++;
                Emit(sb, "c-kw", s, i, j);
                i = j;
                continue;
            }

            Append(sb, c);
            i++;
        }
    }

    private static void ScanShell(string s, StringBuilder sb)
    {
        foreach (var rawLine in s.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            var hash = line.IndexOf('#');
            if (hash >= 0 && line[..hash].Trim().Length == 0)
            {
                Emit(sb, "c-com", line, 0, line.Length);
            }
            else
            {
                var space = line.IndexOf(' ');
                if (space > 0 && line[..space].Trim().Length > 0 && !line.StartsWith(' '))
                {
                    Emit(sb, "c-fn", line, 0, space);
                    Append(sb, line[space..]);
                }
                else
                {
                    Append(sb, line);
                }
            }

            sb.Append('\n');
        }

        if (sb.Length > 0 && sb[^1] == '\n')
            sb.Length--;
    }

    // ---------------------------------------------------------------- helpers

    private static int MatchBrace(string s, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < s.Length; i++)
        {
            if (s[i] == '{')
                depth++;
            else if (s[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        return s.Length;
    }

    private static int MatchParen(string s, int openIndex)
    {
        var depth = 0;
        for (var i = openIndex; i < s.Length; i++)
        {
            if (s[i] == '(')
                depth++;
            else if (s[i] == ')')
            {
                depth--;
                if (depth == 0)
                    return i;
            }
        }

        return s.Length;
    }

    private static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';

    private static bool IsIdentPart(char c) => char.IsLetterOrDigit(c) || c == '_';

    private static void Emit(StringBuilder sb, string cls, string s, int start, int end)
    {
        if (end <= start)
            return;
        sb.Append("<span class=\"").Append(cls).Append("\">");
        Append(sb, s[start..end]);
        sb.Append("</span>");
    }

    private static void Append(StringBuilder sb, string text)
    {
        foreach (var c in text)
            Append(sb, c);
    }

    private static void Append(StringBuilder sb, char c)
    {
        switch (c)
        {
            case '<': sb.Append("&lt;"); break;
            case '>': sb.Append("&gt;"); break;
            case '&': sb.Append("&amp;"); break;
            default: sb.Append(c); break;
        }
    }
}
