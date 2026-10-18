using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Bit.Bsqlite.Demo.Client;

/// <summary>
/// Token colouring for the snippets on the demo pages. Deliberately tiny and dependency-free:
/// the sample must not pull a highlighter down over the wire just to show ten lines of C#.
/// </summary>
internal static partial class CodeHighlighter
{
    public static MarkupString Highlight(string code, string language)
    {
        var normalized = code.Replace("\r\n", "\n").TrimEnd();
        var pattern = PatternFor(language);
        if (pattern is null)
        {
            return new MarkupString(WebUtility.HtmlEncode(normalized));
        }

        var builder = new StringBuilder(normalized.Length + 256);
        var cursor = 0;

        foreach (Match match in pattern.Matches(normalized))
        {
            if (match.Index > cursor)
            {
                builder.Append(WebUtility.HtmlEncode(normalized[cursor..match.Index]));
            }

            var css = CssClassFor(match);
            if (css is null)
            {
                builder.Append(WebUtility.HtmlEncode(match.Value));
            }
            else
            {
                builder.Append("<span class=\"").Append(css).Append("\">")
                    .Append(WebUtility.HtmlEncode(match.Value))
                    .Append("</span>");
            }

            cursor = match.Index + match.Length;
        }

        if (cursor < normalized.Length)
        {
            builder.Append(WebUtility.HtmlEncode(normalized[cursor..]));
        }

        return new MarkupString(builder.ToString());
    }

    private static string? CssClassFor(Match match)
    {
        if (match.Groups["com"].Success) return "tok-com";
        if (match.Groups["str"].Success) return "tok-str";
        if (match.Groups["key"].Success) return "tok-key";
        if (match.Groups["num"].Success) return "tok-num";
        if (match.Groups["typ"].Success) return "tok-typ";
        return null;
    }

    private static Regex? PatternFor(string language) => language.ToLowerInvariant() switch
    {
        "csharp" or "c#" or "cs" or "razor" => CSharpPattern(),
        "sql" => SqlPattern(),
        "xml" or "csproj" or "html" => XmlPattern(),
        "shell" or "bash" or "console" => ShellPattern(),
        _ => null,
    };

    // Comments, strings (verbatim, interpolated, char), keywords, numbers, then PascalCase names.
    [GeneratedRegex(
        """
        (?<com>//[^\n]*|/\*.*?\*/)
        |(?<str>@?\$?"(?:[^"\\]|\\.|"")*"|'(?:[^'\\]|\\.)')
        |(?<key>\b(?:abstract|as|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|get|goto|if|implicit|in|init|int|interface|internal|is|lock|long|namespace|new|nameof|not|null|object|operator|out|override|params|private|protected|public|readonly|record|ref|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|when|where|while|with|yield)\b)
        |(?<num>\b\d[\d_]*(?:\.\d+)?[mfdulUL]*\b)
        |(?<typ>\b[A-Z][A-Za-z0-9_]*\b)
        """,
        RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex CSharpPattern();

    [GeneratedRegex(
        """
        (?<com>--[^\n]*)
        |(?<str>'(?:[^']|'')*')
        |(?<typ>"(?:[^"]|"")*")
        |(?<key>\b(?:ADD|ALL|ALTER|AND|AS|ASC|ATTACH|BEGIN|BETWEEN|BY|CASE|CAST|COLLATE|COMMIT|CREATE|CROSS|DATABASE|DEFAULT|DELETE|DESC|DETACH|DISTINCT|DROP|ELSE|END|EXISTS|FROM|FULL|GROUP|HAVING|IN|INDEX|INNER|INSERT|INTO|IS|JOIN|LEFT|LIKE|LIMIT|NOT|NULL|OFFSET|ON|OR|ORDER|OUTER|PRAGMA|REGEXP|RELEASE|RETURNING|RIGHT|ROLLBACK|SAVEPOINT|SELECT|SET|TABLE|THEN|TRANSACTION|UNION|UPDATE|USING|VALUES|VIEW|WHEN|WHERE|WITH)\b)
        |(?<num>\b\d+(?:\.\d+)?\b)
        """,
        RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex SqlPattern();

    [GeneratedRegex(
        """
        (?<com><!--.*?-->)
        |(?<str>"[^"]*")
        |(?<key></?[A-Za-z_][\w.:-]*|/?>)
        """,
        RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex XmlPattern();

    [GeneratedRegex(
        """
        (?<com>\#[^\n]*)
        |(?<str>"[^"\n]*"|'[^'\n]*')
        |(?<key>^\s*(?:dotnet|npm|pnpm|git|cd)\b)
        """,
        RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace)]
    private static partial Regex ShellPattern();
}
