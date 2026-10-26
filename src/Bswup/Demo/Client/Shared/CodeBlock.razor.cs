using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Bit.Bswup.Demo.Client.Shared;

/// <summary>
/// A syntax-highlighted code sample.
/// <para>
/// The highlighter is deliberately shallow. It is not a parser and does not try to be: it
/// recognises comments, string and character literals, numbers, keywords and - in markup - tags
/// and attributes, and leaves everything else alone. That is the whole design. A sample on a
/// documentation page is read as prose, so the job is to tell a comment apart from code at a
/// glance, not to reproduce an IDE; and a shallow scanner that is wrong about an edge case
/// merely renders that run in the body colour, whereas a deep one that is wrong can swallow
/// half the sample.
/// </para>
/// <para>
/// Two invariants hold no matter what the input is, and every change here has to keep them: the
/// output contains the input's characters in the input's order, and every one of them is
/// HTML-encoded exactly once. The scanners below only ever ADD span boundaries around slices of
/// the original string - no rewriting, no reordering, no dropping - and the gaps between matches
/// are emitted verbatim.
/// </para>
/// </summary>
public partial class CodeBlock
{
    [Parameter] public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Shown in the header and used to pick the scanner. An unrecognised value (or "text")
    /// renders the sample unhighlighted, which is the intended fallback rather than a failure.
    /// </summary>
    [Parameter] public string Language { get; set; } = "code";

    /// <summary>
    /// Optional file name, shown next to the language. Naming the file a sample belongs in is
    /// most of the instruction on a page about configuration.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    private MarkupString _html;

    protected override void OnParametersSet()
    {
        _html = (MarkupString)Highlight(Normalize(Code), Language);

        base.OnParametersSet();
    }

    /// <summary>
    /// Line endings collapsed to LF, and the blank lines the surrounding markup contributes
    /// trimmed off both ends.
    /// <para>
    /// The carriage returns have to go before anything is wrapped in a span. An HTML parser
    /// normalises CRLF to a single newline as it reads the byte stream, so it only recognises a
    /// PAIR when the two characters are adjacent - and a token that runs to the end of its line
    /// (a <c>//</c> comment is the one that does) puts a <c>&lt;/span&gt;</c> between them. The
    /// stranded CR then normalises to a newline of its own, and the sample renders with a blank
    /// line after every comment.
    /// </para>
    /// </summary>
    private static string Normalize(string code)
        => code.Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n');

    // The site's samples are compile-time constants and every page re-renders on each
    // client-side navigation, so the same handful of strings would otherwise be re-scanned for
    // the lifetime of the tab. Bounded by the number of distinct samples on the site.
    private static readonly ConcurrentDictionary<(string Code, string Language), string> _cache = new();

    private static string Highlight(string code, string language)
        => _cache.GetOrAdd((code, language), key => Scan(key.Code, key.Language));

    private static string Scan(string code, string language) => language.ToLowerInvariant() switch
    {
        "csharp" or "cs" or "c#" => Tokenize(code, CSharpPattern()),
        "javascript" or "js" or "typescript" or "ts" => Tokenize(code, JavaScriptPattern()),
        "json" => Tokenize(code, JsonPattern()),
        "html" or "xml" or "razor" or "cshtml" or "svg" => TokenizeMarkup(code),
        "shell" or "bash" or "sh" or "powershell" or "ps1" or "cmd" or "console" => Tokenize(code, ShellPattern()),
        _ => WebUtility.HtmlEncode(code),
    };

    /// <summary>
    /// Walks <paramref name="pattern"/> across the sample, wrapping each match in a span named
    /// after the group that matched and emitting the text between matches untouched.
    /// </summary>
    private static string Tokenize(string code, Regex pattern)
    {
        var builder = new StringBuilder(code.Length + 128);
        var cursor = 0;

        foreach (Match match in pattern.Matches(code))
        {
            // A zero-length match would leave the cursor where it is and emit an empty span; the
            // patterns are written so it cannot happen, and this makes that cheap to be sure of.
            if (match.Length == 0) continue;

            if (match.Index > cursor) builder.Append(WebUtility.HtmlEncode(code[cursor..match.Index]));

            Append(builder, match.Value, ClassOf(match));

            cursor = match.Index + match.Length;
        }

        if (cursor < code.Length) builder.Append(WebUtility.HtmlEncode(code[cursor..]));

        return builder.ToString();
    }

    /// <summary>
    /// Markup is scanned in two passes - tags and comments first, then the inside of each tag -
    /// because an attribute name is only an attribute name INSIDE a tag. A single flat pattern
    /// would colour any word in the sample's text content that happened to be followed by an
    /// equals sign.
    /// </summary>
    private static string TokenizeMarkup(string code)
    {
        var builder = new StringBuilder(code.Length + 128);
        var cursor = 0;

        foreach (Match match in MarkupPattern().Matches(code))
        {
            if (match.Length == 0) continue;

            if (match.Index > cursor) builder.Append(WebUtility.HtmlEncode(code[cursor..match.Index]));

            if (match.Groups["cmt"].Success) Append(builder, match.Value, "tok-cmt");
            else builder.Append(Tokenize(match.Value, TagPattern()));

            cursor = match.Index + match.Length;
        }

        if (cursor < code.Length) builder.Append(WebUtility.HtmlEncode(code[cursor..]));

        return builder.ToString();
    }

    private static void Append(StringBuilder builder, string text, string? cssClass)
    {
        var encoded = WebUtility.HtmlEncode(text);

        if (cssClass is null) builder.Append(encoded);
        else builder.Append("<span class=\"").Append(cssClass).Append("\">").Append(encoded).Append("</span>");
    }

    // The group names double as the token classes, so a new token type is a new named group and
    // one more entry here.
    private static readonly (string Group, string Class)[] _tokenClasses =
    [
        ("cmt", "tok-cmt"),
        ("str", "tok-str"),
        ("kw", "tok-kw"),
        ("typ", "tok-typ"),
        ("num", "tok-num"),
        ("tag", "tok-tag"),
        ("atr", "tok-atr"),
        ("pun", "tok-pun"),
    ];

    private static string? ClassOf(Match match)
    {
        foreach (var (group, cssClass) in _tokenClasses)
        {
            if (match.Groups[group].Success) return cssClass;
        }

        return null;
    }

    // ------------------------------------------------------------------ patterns
    //
    // Alternatives are ordered by which construct swallows the others: a comment can contain
    // anything, a string can contain a comment marker, and only what is left over can be a
    // keyword. Getting that order wrong is the one way these scanners lose text rather than
    // merely miscolour it.

    private const string CSharpKeywords =
        "abstract|as|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|" +
        "delegate|do|double|dynamic|else|enum|event|explicit|extern|false|file|finally|fixed|float|for|foreach|get|" +
        "global|goto|if|implicit|in|init|int|interface|internal|is|lock|long|nameof|namespace|new|null|object|" +
        "operator|out|override|params|private|protected|public|readonly|record|ref|required|return|sbyte|scoped|" +
        "sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|" +
        "unchecked|unsafe|ushort|using|value|var|virtual|void|volatile|when|where|while|with|yield";

    private const string JavaScriptKeywords =
        "async|await|break|case|catch|class|const|continue|debugger|default|delete|do|else|export|extends|false|" +
        "finally|for|from|function|if|import|importScripts|in|instanceof|let|new|null|of|return|self|static|super|" +
        "switch|this|throw|true|try|typeof|undefined|var|void|while|with|yield";

    // A C# verbatim string is listed before the ordinary one so @"...""..." is taken whole: the
    // ordinary alternative would stop at the first doubled quote and leave the rest as code.
    [GeneratedRegex(
        """(?<cmt>//[^\r\n]*|/\*.*?\*/)|(?<str>@"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\\n])*"|'(?:\\.|[^'\\\n])*')|(?<num>\b\d[\d_]*(?:\.\d+)?(?:[eE][+-]?\d+)?[fFdDmMuUlL]*\b)|(?<kw>\b(?:""" + CSharpKeywords + """)\b)|(?<typ>\b[A-Z][A-Za-z0-9_]*\b)""",
        RegexOptions.Singleline)]
    private static partial Regex CSharpPattern();

    [GeneratedRegex(
        """(?<cmt>//[^\r\n]*|/\*.*?\*/)|(?<str>"(?:\\.|[^"\\\n])*"|'(?:\\.|[^'\\\n])*'|`(?:\\.|[^`\\])*`)|(?<num>\b\d[\d_]*(?:\.\d+)?(?:[eE][+-]?\d+)?\b)|(?<kw>\b(?:""" + JavaScriptKeywords + """)\b)|(?<typ>\b[A-Z][A-Za-z0-9_]*\b)""",
        RegexOptions.Singleline)]
    private static partial Regex JavaScriptPattern();

    // A key is a string followed by a colon, and is given the attribute colour so the shape of
    // an object is readable at a glance. It has to precede the general string alternative.
    [GeneratedRegex(
        """(?<atr>"(?:\\.|[^"\\\n])*"(?=\s*:))|(?<str>"(?:\\.|[^"\\\n])*")|(?<kw>\b(?:true|false|null)\b)|(?<num>-?\b\d[\d.]*(?:[eE][+-]?\d+)?\b)""")]
    private static partial Regex JsonPattern();

    // Pass one: comments and whole tags. Everything else is text content.
    [GeneratedRegex("""(?<cmt><!--.*?-->)|(?<tag><[!/?]?[A-Za-z][^>]*>)""", RegexOptions.Singleline)]
    private static partial Regex MarkupPattern();

    // Pass two, inside one tag. Values come first so a quoted value is never mistaken for a run
    // of attribute names; a name is only a name when an equals sign follows it.
    [GeneratedRegex("""(?<str>"[^"]*"|'[^']*')|(?<atr>[A-Za-z_@:][\w:.\-@]*(?=\s*=))|(?<tag><[!/?]?[A-Za-z][\w:.\-]*|/?>)""")]
    private static partial Regex TagPattern();

    // The first word of a line is the command being run, which is the one thing worth picking out
    // of a shell sample; after that, the flags.
    [GeneratedRegex(
        """(?<cmt>#[^\r\n]*)|(?<str>"(?:\\.|[^"\\\n])*"|'[^'\n]*')|(?<kw>^[ \t]*[A-Za-z][\w.\-]*)|(?<atr>(?<![\w\-])--?[A-Za-z][\w\-]*)""",
        RegexOptions.Multiline)]
    private static partial Regex ShellPattern();
}
