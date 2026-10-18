using System.Text;
using System.Text.RegularExpressions;

namespace Bit.Butil.Demo.Client.Services;

/// <summary>
/// Turns a code sample into HTML with a span per token, so <see cref="Shared.CodeBlock"/> can
/// colour it. Deliberately hand-rolled: a docs site whose whole argument is how little JavaScript
/// a Blazor app needs would be a poor advertisement for itself if its own code panes were painted
/// by a JavaScript library, and the samples here are short enough that a scanner costs nothing.
/// </summary>
/// <remarks>
/// This is a lexer, not a parser: at each position the language's rules are tried in order and the
/// first one that matches consumes its text, with anything no rule claims emitted as escaped plain
/// text. That is the same approximation every editor used before language servers - enough for a
/// twelve-line sample, and it degrades into plain text rather than into nonsense when it meets
/// something it does not understand.
/// </remarks>
public static class SyntaxHighlighter
{
    /// <summary>Highlights <paramref name="code"/> and returns HTML ready for a MarkupString.</summary>
    /// <param name="language">A fence slug - "csharp", "razor", "json"... An unknown slug comes
    /// back as escaped plain text, which is exactly what the pane rendered before it had colour.</param>
    public static string Highlight(string? code, string? language)
    {
        if (string.IsNullOrEmpty(code)) return string.Empty;

        var builder = new StringBuilder(code.Length + (code.Length / 2));
        Scan(code, Normalize(language), builder);
        return builder.ToString();
    }

    /// <summary>The fence slugs the pages use, plus the usual aliases for each.</summary>
    private static string Normalize(string? language) => (language ?? "").Trim().ToLowerInvariant() switch
    {
        "" or "cs" or "c#" or "csharp" => "csharp",
        "razor" or "cshtml" or "blazor" => "razor",
        "html" or "xml" or "xaml" or "axaml" or "svg" => "markup",
        "json" or "jsonc" => "json",
        "sh" or "bash" or "shell" or "console" or "powershell" or "ps1" or "cmd" => "shell",
        "md" or "markdown" => "markdown",
        "http" or "https" => "http",
        "js" or "javascript" or "ts" or "typescript" => "javascript",
        "css" or "scss" => "css",
        var other => other,
    };

    // -------------------------------------------------------------------------------------------
    // The scanner
    // -------------------------------------------------------------------------------------------

    /// <summary>What a rule does with the text it matched.</summary>
    /// <param name="source">The whole sample. A rule whose end cannot be written as a regex - a
    /// brace-delimited block, where the brace that closes it depends on the ones in between -
    /// finds its own end in here and reports it back.</param>
    /// <returns>How many characters the rule consumed, counted from the start of the match. Almost
    /// always the match's own length; more only for the rules that scan past it.</returns>
    private delegate int Emitter(string source, Match match, StringBuilder builder);

    /// <summary>
    /// One lexical rule. <see cref="Class"/> is the common case - wrap the whole match in a single
    /// span - and <see cref="Emit"/> is for matches that have structure worth keeping, such as a
    /// tag, whose name, attribute names and attribute values are three different colours.
    /// </summary>
    private sealed class Rule
    {
        public Rule(string pattern, string? cls = null, Emitter? emit = null)
        {
            // \G pins the match to wherever the scanner currently is: a rule may only claim the
            // text in front of the cursor, never something further along that happens to match
            // first. ^ and $ inside a pattern stay line anchors, which is what the line-oriented
            // languages - HTTP, Markdown, shell - are written against.
            Regex = new Regex(@"\G(?:" + pattern + ")", RegexOptions.Multiline | RegexOptions.ExplicitCapture);
            Class = cls;
            Emit = emit;
        }

        public Regex Regex { get; }
        public string? Class { get; }
        public Emitter? Emit { get; }
    }

    private static void Scan(string code, string language, StringBuilder builder)
    {
        var rules = RulesFor(language);
        if (rules.Length == 0)
        {
            Escape(code, builder);
            return;
        }

        var position = 0;
        // Where the run of text that no rule has claimed starts. Flushed when a rule finally fires
        // rather than a character at a time, so plain prose costs one Escape call, not one per char.
        var plainFrom = 0;

        while (position < code.Length)
        {
            var matched = false;

            foreach (var rule in rules)
            {
                var match = rule.Regex.Match(code, position);
                // An empty match would spin this loop forever, so it counts as no match at all.
                if (match.Success is false || match.Index != position || match.Length == 0) continue;

                Escape(code.AsSpan(plainFrom, position - plainFrom), builder);

                var consumed = match.Length;

                if (rule.Emit is not null)
                {
                    // Never less than the match, or the text between would be emitted twice.
                    consumed = Math.Max(match.Length, rule.Emit(code, match, builder));
                }
                else
                {
                    Token(builder, rule.Class, match.Value);
                }

                position += consumed;
                plainFrom = position;
                matched = true;
                break;
            }

            if (matched is false) position++;
        }

        Escape(code.AsSpan(plainFrom, position - plainFrom), builder);
    }

    /// <summary>Writes one token, skipping the span when a rule only wanted the text consumed.</summary>
    private static void Token(StringBuilder builder, string? cls, string text)
    {
        if (text.Length == 0) return;

        if (cls is null)
        {
            Escape(text, builder);
            return;
        }

        builder.Append("<span class=\"tok-").Append(cls).Append("\">");
        Escape(text, builder);
        builder.Append("</span>");
    }

    /// <summary>Highlights a fragment with another language's rules - the C# in an interpolation
    /// hole, the JSON body under a set of HTTP headers, the C# inside an <c>@code</c> block.</summary>
    private static void Nested(StringBuilder builder, string fragment, string language) =>
        Scan(fragment, language, builder);

    private static void Escape(string text, StringBuilder builder) => Escape(text.AsSpan(), builder);

    private static void Escape(ReadOnlySpan<char> text, StringBuilder builder)
    {
        foreach (var c in text)
        {
            switch (c)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                default: builder.Append(c); break;
            }
        }
    }

    private static Rule[] RulesFor(string language) => language switch
    {
        "csharp" => CSharp,
        "razor" => Razor,
        "markup" => Markup,
        "json" => Json,
        "shell" => Shell,
        "markdown" => Markdown,
        "http" => Http,
        "javascript" => JavaScript,
        "css" => Css,
        _ => [],
    };

    // -------------------------------------------------------------------------------------------
    // C#
    // -------------------------------------------------------------------------------------------

    private const string CSharpKeywords =
        "abstract|add|as|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|" +
        "decimal|default|delegate|do|double|dynamic|else|enum|event|explicit|extern|file|finally|fixed|" +
        "float|for|foreach|get|global|goto|if|implicit|in|init|int|interface|internal|is|lock|long|nameof|" +
        "namespace|new|nint|not|nuint|object|operator|or|out|override|params|partial|private|protected|" +
        "public|readonly|record|ref|remove|required|return|sbyte|sealed|set|short|sizeof|stackalloc|static|" +
        "string|struct|switch|this|throw|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|value|var|" +
        "virtual|void|volatile|when|where|while|with|yield";

    /// <summary>
    /// C#. The two heuristics carrying it are that a PascalCase word is a type and that a word in
    /// front of an open parenthesis is a call, which between them make <c>await geolocation
    /// .GetCurrentPosition()</c> read correctly with no idea of what any of those names refer to.
    /// </summary>
    private static readonly Rule[] CSharp =
    [
        new(@"//[^\n]*", "com"),
        new(@"/\*[\s\S]*?\*/", "com"),
        new("\"\"\"[\\s\\S]*?\"\"\"", "str"),
        new("@\"(?:[^\"]|\"\")*\"", "str"),
        // $"..." keeps its holes: they are C#, and the eye reads them as C#, so they are scanned
        // as C# instead of being painted string-coloured along with the literal text around them.
        new("\\$@?\"(?:\\\\.|\\{\\{|\\}\\}|\\{[^{}\"]*\\}|[^\"\\\\])*\"", emit: Interpolated),
        new("\"(?:\\\\.|[^\"\\\\\n])*\"", "str"),
        new(@"'(?:\\.|[^'\\\n])*'", "str"),
        new(@"^[ \t]*#[a-z]+[^\n]*", "meta"),
        new(@"\b(?:true|false|null)\b", "lit"),
        new(@"\b0[xXbB][0-9a-fA-F_]+[uUlL]{0,2}\b", "num"),
        new(@"\b\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?(?:[fFdDmMuUlL]{1,2})?\b", "num"),
        new(@"\b(?:" + CSharpKeywords + @")\b", "kw"),
        new(@"\b[A-Za-z_]\w*(?=\s*(?:<[\w\s,.<>\[\]?]*>\s*)?\()", "fn"),
        new(@"\b[A-Z]\w*\b", "typ"),
        // Identifiers are consumed whole and left plain. Without this the keyword rule would bite
        // the tail off a name such as "isNew" on a later pass through the loop.
        new(@"\b[A-Za-z_]\w*\b"),
        new(@"=>|\?\?=|\?\?|[+\-*/%=<>!&|^~]+", "op"),
        new(@"[{}()\[\];,.:?]", "punc"),
    ];

    /// <summary>An interpolated string: the literal runs stay string-coloured, the holes are C#.</summary>
    private static int Interpolated(string source, Match match, StringBuilder builder)
    {
        var text = match.Value;
        var literalFrom = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '{') continue;
            // {{ is an escaped brace, not the start of a hole.
            if (i + 1 < text.Length && text[i + 1] == '{') { i++; continue; }

            var close = text.IndexOf('}', i + 1);
            if (close < 0) break;

            Token(builder, "str", text[literalFrom..i]);
            Token(builder, "punc", "{");
            Nested(builder, text[(i + 1)..close], "csharp");
            Token(builder, "punc", "}");

            literalFrom = close + 1;
            i = close;
        }

        Token(builder, "str", text[literalFrom..]);
        return text.Length;
    }

    // -------------------------------------------------------------------------------------------
    // HTML / XML
    // -------------------------------------------------------------------------------------------

    /// <summary>HTML and XML: one rule for the whole tag, taken apart again by <see cref="Tag"/>.</summary>
    private static readonly Rule[] Markup =
    [
        new(@"<!--[\s\S]*?-->", "com"),
        new(@"<!\[CDATA\[[\s\S]*?\]\]>", "meta"),
        new(@"<!(?i:doctype)[^>]*>", "meta"),
        new(@"<\?[\s\S]*?\?>", "meta"),
        new("</?[A-Za-z][\\w:.-]*(?:\"[^\"]*\"|'[^']*'|[^>\"'])*/?>", emit: Tag),
        new(@"&(?:#\d+|#x[0-9a-fA-F]+|[a-zA-Z]\w*);", "lit"),
    ];

    // The attribute run is lazy so that the slash of a self-closing tag lands in "close" rather
    // than being swallowed as the last character of the attributes. Quoted values still survive:
    // laziness only decides when to stop, and a quote can only be consumed by the alternative
    // that takes the whole literal, so a > inside one is never mistaken for the end of the tag.
    private static readonly Regex TagParts = new(
        @"\G(?<open></?)(?<name>[A-Za-z][\w:.-]*)(?<attrs>(?:""[^""]*""|'[^']*'|[^>""'])*?)(?<close>/?>)",
        RegexOptions.Singleline);

    private static readonly Regex Attribute = new(
        @"(?<ws>\s+)(?<name>[^\s=/>]+)(?:(?<eq>\s*=\s*)(?<value>""[^""]*""|'[^']*'|[^\s>]+))?",
        RegexOptions.Singleline);

    private static int Tag(string source, Match match, StringBuilder builder)
    {
        var parts = TagParts.Match(match.Value);
        if (parts.Success is false)
        {
            Token(builder, "tag", match.Value);
            return match.Length;
        }

        Token(builder, "punc", parts.Groups["open"].Value);
        Token(builder, "tag", parts.Groups["name"].Value);

        var attrs = parts.Groups["attrs"].Value;
        var consumed = 0;

        foreach (Match attr in Attribute.Matches(attrs))
        {
            // Whatever sat between the previous attribute and this one. Normally nothing, since
            // the whitespace is part of the match, but it is never dropped on the floor.
            Escape(attrs.AsSpan(consumed, attr.Index - consumed), builder);

            Escape(attr.Groups["ws"].Value, builder);
            Token(builder, "atn", attr.Groups["name"].Value);

            if (attr.Groups["eq"].Success)
            {
                Token(builder, "punc", attr.Groups["eq"].Value);
                Token(builder, "atv", attr.Groups["value"].Value);
            }

            consumed = attr.Index + attr.Length;
        }

        Escape(attrs.AsSpan(consumed), builder);
        Token(builder, "punc", parts.Groups["close"].Value);
        return match.Length;
    }

    // -------------------------------------------------------------------------------------------
    // Razor
    // -------------------------------------------------------------------------------------------

    /// <summary>
    /// Razor is markup with holes in it, so it is the markup rules with the transitions in front of
    /// them. Because a rule can only ever fire at the cursor, <c>@onclick</c> inside a tag needs no
    /// special case: the scanner reaches the <c>&lt;</c> first, the tag rule consumes the element
    /// whole and colours it as an attribute, while the same text between tags is an expression and
    /// gets handed to the C# rules.
    /// </summary>
    private static readonly Rule[] Razor =
    [
        new(@"@\*[\s\S]*?\*@", "com"),
        new("@@", "op"),
        // @code / @functions: the header is a directive, everything inside the braces is C#.
        new(@"@(?:code|functions)\s*\{", emit: (s, m, b) => Block(s, m, b, m.Value.Length - 1)),
        new(@"@\{", emit: (s, m, b) => Block(s, m, b, 1)),
        new(@"^[ \t]*@(?:page|inject|using|implements|inherits|namespace|attribute|typeparam|layout|" +
            @"rendermode|preservewhitespace|addTagHelper|removeTagHelper|model)\b[^\n]*", emit: Directive),
        // A control-flow transition. The body after it is markup and is left to the scanner, but
        // the condition in brackets is C# and is worth colouring - it is where the sample's actual
        // logic is, and leaving it grey next to a fully coloured @code block looks like an omission.
        new(@"@(?:else[ \t]+if|if|else|foreach|for|while|switch|do|try|catch|finally|lock|using|await)\b",
            emit: ControlFlow),
        // An explicit @(...) expression, one level of nested parentheses deep - which is as far as
        // the samples on this site go, and the fallback below still catches the name if it is not.
        new("@\\((?:[^()\"]|\"[^\"]*\"|\\((?:[^()\"]|\"[^\"]*\")*\\))*\\)", emit: Expression),
        new(@"@[A-Za-z_][\w.]*", emit: Expression),
        .. Markup,
    ];

    /// <summary>A <c>@if</c> / <c>@foreach</c> / ... transition, with its condition if it has one.</summary>
    private static int ControlFlow(string source, Match match, StringBuilder builder)
    {
        Token(builder, "meta", "@");
        Token(builder, "kw", match.Value[1..]);

        var open = match.Index + match.Length;
        while (open < source.Length && source[open] is ' ' or '\t') open++;

        if (open >= source.Length || source[open] != '(') return match.Length;

        var close = MatchingBracket(source, open, '(', ')');
        if (close < 0) return match.Length;

        Escape(source.AsSpan(match.Index + match.Length, open - match.Index - match.Length), builder);
        Token(builder, "punc", "(");
        Nested(builder, source[(open + 1)..close], "csharp");
        Token(builder, "punc", ")");

        return close + 1 - match.Index;
    }

    /// <summary>A <c>@code { ... }</c> or <c>@{ ... }</c> block: header, C# body, closing brace.</summary>
    /// <param name="bracePosition">Where the opening brace sits inside the rule's own match.</param>
    private static int Block(string source, Match match, StringBuilder builder, int bracePosition)
    {
        var start = match.Index + bracePosition;
        var end = MatchingBracket(source, start);

        Token(builder, "meta", match.Value[..bracePosition]);
        Token(builder, "punc", "{");

        // Unbalanced means the sample was clipped mid-block, which is a normal thing for a docs
        // snippet to be. The rest of it is still C#, so it is still coloured as C#.
        Nested(builder, end < 0 ? source[(start + 1)..] : source[(start + 1)..end], "csharp");

        if (end < 0) return source.Length - match.Index;

        Token(builder, "punc", "}");
        // The rule's own match stopped at the opening brace; the block runs on to the closing one.
        return end + 1 - match.Index;
    }

    /// <summary>The index of the bracket closing the one at <paramref name="open"/>, or -1.</summary>
    /// <remarks>Brackets inside strings, chars and comments do not count, which is the entire
    /// reason this is a scan and not a <c>LastIndexOf</c>.</remarks>
    private static int MatchingBracket(string code, int open, char opening = '{', char closing = '}')
    {
        var depth = 0;

        for (var i = open; i < code.Length; i++)
        {
            var c = code[i];

            if (c == opening)
            {
                depth++;
            }
            else if (c == closing)
            {
                if (--depth == 0) return i;
            }
            else if (c == '/' && i + 1 < code.Length && code[i + 1] == '/')
            {
                var newline = code.IndexOf('\n', i);
                i = newline < 0 ? code.Length : newline;
            }
            else if (c == '/' && i + 1 < code.Length && code[i + 1] == '*')
            {
                var close = code.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close < 0 ? code.Length : close + 1;
            }
            else if (c is '"' or '\'')
            {
                i = SkipLiteral(code, i, c);
            }
        }

        return -1;
    }

    /// <summary>The index of the quote closing the literal that opens at <paramref name="start"/>.</summary>
    private static int SkipLiteral(string code, int start, char quote)
    {
        // A raw string literal ends at a run of quotes as long as the one that opened it and has
        // no escapes inside, so it is matched on its fence rather than character by character.
        var fence = 0;
        while (start + fence < code.Length && code[start + fence] == quote) fence++;

        if (fence >= 3)
        {
            var close = code.IndexOf(new string(quote, fence), start + fence, StringComparison.Ordinal);
            return close < 0 ? code.Length : close + fence - 1;
        }

        var verbatim = start > 0 && code[start - 1] == '@';

        for (var i = start + 1; i < code.Length; i++)
        {
            if (code[i] == '\\' && verbatim is false) { i++; continue; }
            if (code[i] != quote) continue;
            // "" inside a verbatim literal is an escaped quote, not the end of the literal.
            if (verbatim && i + 1 < code.Length && code[i + 1] == quote) { i++; continue; }
            return i;
        }

        return code.Length;
    }

    /// <summary>A directive line: <c>@inject</c> in the directive colour, its arguments as C#.</summary>
    private static int Directive(string source, Match match, StringBuilder builder)
    {
        var text = match.Value;
        var at = text.IndexOf('@');
        var end = at + 1;
        while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_')) end++;

        Escape(text[..at], builder);
        Token(builder, "meta", text[at..end]);
        Nested(builder, text[end..], "csharp");
        return text.Length;
    }

    /// <summary>A <c>@expression</c>: the transition marker, then C#.</summary>
    private static int Expression(string source, Match match, StringBuilder builder)
    {
        Token(builder, "meta", "@");
        Nested(builder, match.Value[1..], "csharp");
        return match.Length;
    }

    // -------------------------------------------------------------------------------------------
    // JSON, HTTP, shell, Markdown
    // -------------------------------------------------------------------------------------------

    private static readonly Rule[] Json =
    [
        // A key is a string with a colon after it, and it is worth its own colour: a JSON-RPC
        // envelope is mostly keys, and without the split the pane is one flat wall of string.
        new("\"(?:\\\\.|[^\"\\\\])*\"(?=\\s*:)", "key"),
        new("\"(?:\\\\.|[^\"\\\\])*\"", "str"),
        new(@"\b(?:true|false|null)\b", "lit"),
        new(@"-?\b\d+(?:\.\d+)?(?:[eE][+-]?\d+)?\b", "num"),
        new(@"[{}\[\],:]", "punc"),
    ];

    /// <summary>
    /// A raw HTTP request or response. The head is line-oriented; the body is whatever the head
    /// says it is, which on this site is always JSON, so it is handed to the JSON rules.
    /// </summary>
    private static readonly Rule[] Http =
    [
        new(@"^(?:GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE|CONNECT)[ \t]+\S+(?:[ \t]+HTTP/[\d.]+)?[ \t]*$",
            emit: RequestLine),
        new(@"^HTTP/[\d.]+[ \t]+\d{3}[^\n]*$", emit: (s, m, b) =>
        {
            var space = m.Value.IndexOf(' ');
            Token(b, "meta", m.Value[..space]);
            Token(b, "num", m.Value[space..]);
            return m.Length;
        }),
        new(@"^[A-Za-z][\w-]*:[^\n]*$", emit: (s, m, b) =>
        {
            var colon = m.Value.IndexOf(':');
            Token(b, "atn", m.Value[..colon]);
            Token(b, "punc", ":");
            Token(b, "atv", m.Value[(colon + 1)..]);
            return m.Length;
        }),
        // The blank line ends the head: past it there are no more headers, only a payload.
        new(@"\r?\n\r?\n[\s\S]+", emit: (s, m, b) =>
        {
            var body = m.Value.TrimStart('\r', '\n');
            Escape(m.Value[..^body.Length], b);

            var first = body.TrimStart();
            Nested(b, body, first.StartsWith('{') || first.StartsWith('[') ? "json" : "");
            return m.Length;
        }),
    ];

    private static int RequestLine(string source, Match match, StringBuilder builder)
    {
        var text = match.Value;
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var at = 0;

        for (var i = 0; i < parts.Length; i++)
        {
            var index = text.IndexOf(parts[i], at, StringComparison.Ordinal);
            Escape(text.AsSpan(at, index - at), builder);
            Token(builder, i switch { 0 => "kw", 1 => "atv", _ => "meta" }, parts[i]);
            at = index + parts[i].Length;
        }

        Escape(text.AsSpan(at), builder);
        return text.Length;
    }

    /// <summary>
    /// A shell transcript: the first word of a line is the command, a leading <c>$</c> or
    /// <c>&gt;</c> is the prompt rather than part of it, and the rest are arguments.
    /// </summary>
    private static readonly Rule[] Shell =
    [
        new(@"#[^\n]*", "com"),
        new("\"(?:\\\\.|[^\"\\\\])*\"", "str"),
        new(@"'[^']*'", "str"),
        new(@"^[ \t]*(?:[$>][ \t]+)?[\w.\-/]+", emit: (s, m, b) =>
        {
            var command = m.Value.TrimStart(' ', '\t', '$', '>');
            Token(b, "punc", m.Value[..^command.Length]);
            Token(b, "fn", command);
            return m.Length;
        }),
        new(@"(?<=\s)--?[\w-]+", "atn"),
        new(@"\$\{[^}]*\}|\$\w+", "var"),
        new(@"[|&;<>]+", "op"),
    ];

    private static readonly Rule[] Markdown =
    [
        new(@"^```[^\n]*", "meta"),
        new(@"^#{1,6}[ \t][^\n]*", "head"),
        new(@"^[ \t]*>[^\n]*", "com"),
        new(@"^[ \t]*(?:[-*+]|\d+\.)(?=[ \t])", "punc"),
        new(@"^[ \t]*(?:-{3,}|={3,}|\*{3,})[ \t]*$", "punc"),
        new(@"`[^`\n]+`", "str"),
        new(@"\*\*[^*\n]+\*\*|__[^_\n]+__", "strong"),
        new(@"(?<!\*)\*[^*\n]+\*(?!\*)", "em"),
        new(@"!?\[[^\]\n]*\]\([^)\n]*\)", emit: Link),
        new(@"^\|[^\n]*", emit: (s, m, b) => { Token(b, "punc", m.Value); return m.Length; }),
    ];

    private static readonly Regex LinkParts = new(@"\G(?<bang>!?)\[(?<text>[^\]]*)\]\((?<url>[^)]*)\)");

    private static int Link(string source, Match match, StringBuilder builder)
    {
        var parts = LinkParts.Match(match.Value);
        if (parts.Success is false)
        {
            Escape(match.Value, builder);
            return match.Length;
        }

        Token(builder, "punc", parts.Groups["bang"].Value + "[");
        Token(builder, "atn", parts.Groups["text"].Value);
        Token(builder, "punc", "](");
        Token(builder, "atv", parts.Groups["url"].Value);
        Token(builder, "punc", ")");
        return match.Length;
    }

    // -------------------------------------------------------------------------------------------
    // JavaScript and CSS - nothing on the site uses them yet, but a page that quotes the one
    // script tag Butil needs, or the variables its panes are themed with, should not go grey.
    // -------------------------------------------------------------------------------------------

    private const string JsKeywords =
        "as|async|await|break|case|catch|class|const|continue|debugger|default|delete|do|else|export|" +
        "extends|finally|for|from|function|get|if|import|in|instanceof|let|new|of|return|set|static|" +
        "super|switch|this|throw|try|typeof|var|void|while|with|yield";

    private static readonly Rule[] JavaScript =
    [
        new(@"//[^\n]*", "com"),
        new(@"/\*[\s\S]*?\*/", "com"),
        new("`(?:\\\\.|[^`\\\\])*`", "str"),
        new("\"(?:\\\\.|[^\"\\\\\n])*\"", "str"),
        new(@"'(?:\\.|[^'\\\n])*'", "str"),
        new(@"\b(?:true|false|null|undefined|NaN)\b", "lit"),
        new(@"\b0[xXbBoO][0-9a-fA-F_]+n?\b", "num"),
        new(@"\b\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?n?\b", "num"),
        new(@"\b(?:" + JsKeywords + @")\b", "kw"),
        new(@"\b[A-Za-z_$][\w$]*(?=\s*\()", "fn"),
        new(@"\b[A-Z][\w$]*\b", "typ"),
        new(@"\b[A-Za-z_$][\w$]*\b"),
        new(@"=>|===|!==|[+\-*/%=<>!&|^~?]+", "op"),
        new(@"[{}()\[\];,.:]", "punc"),
    ];

    private static readonly Rule[] Css =
    [
        new(@"/\*[\s\S]*?\*/", "com"),
        new("\"(?:\\\\.|[^\"\\\\])*\"|'(?:\\\\.|[^'\\\\])*'", "str"),
        new(@"@[\w-]+", "meta"),
        new(@"--[\w-]+", "var"),
        new(@"[-a-zA-Z]+(?=[ \t]*:)", "atn"),
        new(@"#[0-9a-fA-F]{3,8}\b", "num"),
        new(@"-?\b\d*\.?\d+(?:%|[a-zA-Z]+)?", "num"),
        new(@"[.#:]?[\w-]+(?=[^{};]*\{)", "tag"),
        new(@"[{}();:,]", "punc"),
    ];
}
