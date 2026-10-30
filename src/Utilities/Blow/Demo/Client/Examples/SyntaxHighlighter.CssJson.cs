using System.Text;

namespace Bit.Blow.Demo.Client.Examples;

public static partial class SyntaxHighlighter
{
    // What the CSS and the JSON lexers share: a position in the source, and the HTML written so far - through Emit and
    // DelimitedEnd, as the Razor and C# lexer writes its own.
    private abstract class Scanner(string source)
    {
        private readonly StringBuilder _sb = new(source.Length * 2);

        protected string Text { get; } = source;

        protected int Index { get; set; }

        protected string Html => _sb.ToString();

        protected char Peek(int offset = 0)
        {
            return Index + offset < Text.Length ? Text[Index + offset] : '\0';
        }

        protected bool At(string text)
        {
            return Text.AsSpan(Index).StartsWith(text, StringComparison.Ordinal);
        }

        // A comment that runs from `open` through `close`, or to the end of the source when it is never closed.
        protected void Delimited(string cls, int openLength, string close)
        {
            Take(cls, DelimitedEnd(Text, Index, openLength, close) - Index);
        }

        // A quoted string through its closing quote - a backslash escapes the character after it - or to the end of
        // the line when it is never closed.
        protected int StringEnd(int from)
        {
            var quote = Text[from];
            var end = from + 1;
            while (end < Text.Length && Text[end] != quote && Text[end] != '\n')
            {
                end += Text[end] == '\\' ? 2 : 1;
            }
            return Math.Min(end + 1, Text.Length);
        }

        // Emits the next `length` characters as a token and moves past them.
        protected void Take(string cls, int length)
        {
            Span(cls, Index, Index + length);
            Index += length;
        }

        protected void Span(string cls, int start, int end)
        {
            Emit(_sb, cls, Text, start, end);
        }

        // Emits the next `length` characters uncoloured and moves past them.
        protected void Plain(int length)
        {
            Emit(_sb, null, Text, Index, Index + length);
            Index += length;
        }
    }

    // ------------------------------------------------------------------------------------------------- CSS

    // A stylesheet: rules, at-rules and the declarations inside them. Whether a stretch of it is a rule's prelude - a
    // selector or an at-rule - or a declaration is told by what ends it: a '{' ends a prelude, a ';' or a '}' a
    // declaration. That holds at the top level, inside an @media block and inside a rule with nested rules alike.
    private sealed class CssLexer(string code) : Scanner(code)
    {
        public string Run()
        {
            while (Index < Text.Length)
            {
                Statement();
            }
            return Html;
        }

        private void Statement()
        {
            var c = Text[Index];
            if (char.IsWhiteSpace(c))
            {
                Plain(1);
                return;
            }
            if (At("/*"))
            {
                Delimited("tok-com", 2, "*/");
                return;
            }
            if (c is '{' or '}' or ';')
            {
                Take("tok-pun", 1);
                return;
            }
            if (BlockFollows())
            {
                Prelude();
                return;
            }
            if (c == '@')
            {
                // A statement at-rule: `@import url("theme.css");`.
                Prelude();
                return;
            }
            Declaration();
        }

        // Whether the stretch from here ends with a '{' - a block - rather than with a ';' or a '}'.
        private bool BlockFollows()
        {
            var depth = 0;
            for (var k = Index; k < Text.Length; k++)
            {
                var c = Text[k];
                if (c is '"' or '\'')
                {
                    k = StringEnd(k) - 1;
                }
                else if (c == '/' && k + 1 < Text.Length && Text[k + 1] == '*')
                {
                    var end = Text.IndexOf("*/", k + 2, StringComparison.Ordinal);
                    k = end < 0 ? Text.Length : end + 1;
                }
                else if (c is '(' or '[')
                {
                    depth++;
                }
                else if (c is ')' or ']')
                {
                    depth--;
                }
                else if (depth <= 0 && c is '{' or ';' or '}')
                {
                    return c == '{';
                }
            }
            return false;
        }

        // A selector - `.palette [data-balette="item"]:hover > kbd` - or an at-rule's prelude - `@media (max-width: 600px)` -
        // up to the '{' of its block, or the ';' that ends a statement at-rule.
        private void Prelude()
        {
            var atRule = Peek() == '@';
            while (Index < Text.Length && Peek() is not ('{' or ';' or '}'))
            {
                var c = Peek();
                if (At("/*"))
                {
                    Delimited("tok-com", 2, "*/");
                }
                else if (c is '"' or '\'')
                {
                    Take("tok-str", StringEnd(Index) - Index);
                }
                else if (c == '@' && IsNameStart(Index + 1))
                {
                    Take("tok-key", 1 + NameLength(Index + 1));
                }
                else if (c == '[' && !atRule)
                {
                    AttributeSelector();
                }
                else if (c is '.' or '#' && IsNameStart(Index + 1) && !atRule)
                {
                    Take("tok-type", 1 + NameLength(Index + 1));
                }
                else if (c == ':' && !atRule && (IsNameStart(Index + 1) || (Peek(1) == ':' && IsNameStart(Index + 2))))
                {
                    var colons = Peek(1) == ':' ? 2 : 1;
                    Take("tok-fn", colons + NameLength(Index + colons));
                }
                else if (atRule && IsNameStart(Index) && Peek(NameLength(Index)) == '(')
                {
                    // A function, `url(theme.css)` or `layer(base)`, rather than a media feature's parentheses.
                    Function();
                }
                else if (c == '(' && atRule)
                {
                    // A media feature, `(max-width: 600px)`: its name, then a value.
                    Plain(1);
                    while (Index < Text.Length && char.IsWhiteSpace(Peek()))
                    {
                        Plain(1);
                    }
                    if (IsNameStart(Index))
                    {
                        Take("tok-attr", NameLength(Index));
                    }
                }
                else if (IsNumberStart())
                {
                    Number();
                }
                else if (IsNameStart(Index) || c == '*')
                {
                    // An element in a selector; a media type or `and` in an at-rule.
                    var length = c == '*' ? 1 : NameLength(Index);
                    if (atRule)
                    {
                        Plain(length);
                    }
                    else
                    {
                        Take("tok-tag", length);
                    }
                }
                else if (c == '&')
                {
                    // The parent selector of a nested rule.
                    Take("tok-key", 1);
                }
                else
                {
                    Plain(1);
                }
            }
            if (Peek() is '{' or ';')
            {
                Take("tok-pun", 1);
            }
        }

        // `[data-balette="item"]`, `[data-active]`, `[dir=rtl]`.
        private void AttributeSelector()
        {
            Take("tok-pun", 1);
            while (Index < Text.Length && Peek() is not (']' or '{' or '\n'))
            {
                var c = Peek();
                if (c is '"' or '\'')
                {
                    Take("tok-str", StringEnd(Index) - Index);
                }
                else if (IsNameStart(Index))
                {
                    // The attribute's name, or - after the operator - an unquoted value.
                    var length = NameLength(Index);
                    var value = Index > 0 && Text[Index - 1] == '=';
                    Take(value ? "tok-str" : "tok-attr", length);
                }
                else
                {
                    Plain(1);
                }
            }
            if (Peek() == ']')
            {
                Take("tok-pun", 1);
            }
        }

        // `color: var(--accent) !important;` - the property, then its value up to the ';', or up to the '}' that closes
        // the block when the last declaration has none.
        private void Declaration()
        {
            if (!IsNameStart(Index))
            {
                Plain(1);
                return;
            }
            Take("tok-attr", NameLength(Index));
            while (Index < Text.Length && Peek() is ' ' or '\t')
            {
                Plain(1);
            }
            if (Peek() != ':')
            {
                return;
            }
            Take("tok-pun", 1);

            while (Index < Text.Length && Peek() is not (';' or '}'))
            {
                var c = Peek();
                if (At("/*"))
                {
                    Delimited("tok-com", 2, "*/");
                }
                else if (c is '"' or '\'')
                {
                    Take("tok-str", StringEnd(Index) - Index);
                }
                else if (c == '#' && char.IsAsciiHexDigit(Peek(1)))
                {
                    // A colour.
                    var end = Index + 1;
                    while (end < Text.Length && char.IsAsciiLetterOrDigit(Text[end]))
                    {
                        end++;
                    }
                    Take("tok-num", end - Index);
                }
                else if (c == '!' && IsNameStart(Index + 1))
                {
                    Take("tok-key", 1 + NameLength(Index + 1));
                }
                else if (IsNumberStart())
                {
                    Number();
                }
                else if (c == '-' && Peek(1) == '-')
                {
                    // A custom property, `var(--accent)`.
                    Take("tok-attr", NameLength(Index));
                }
                else if (IsNameStart(Index))
                {
                    var length = NameLength(Index);
                    if (Index + length < Text.Length && Text[Index + length] == '(')
                    {
                        Function();
                    }
                    else
                    {
                        Plain(length);
                    }
                }
                else
                {
                    Plain(1);
                }
            }
            if (Peek() == ';')
            {
                Take("tok-pun", 1);
            }
        }

        // A function's name and its '(' - `var(`, `url(` - in a value or an at-rule's prelude; what is inside follows.
        private void Function()
        {
            var length = NameLength(Index);
            var url = Text.AsSpan(Index, length).Equals("url", StringComparison.OrdinalIgnoreCase);
            Take("tok-fn", length);
            Plain(1);
            if (url && Peek() is not ('"' or '\''))
            {
                // An unquoted address is a string as much as a quoted one.
                var close = Text.IndexOf(')', Index);
                Take("tok-str", (close < 0 ? Text.Length : close) - Index);
            }
        }

        // `760px`, `.5`, `-2.25rem`, `45%`, `140ms`.
        private void Number()
        {
            var end = Index;
            if (Text[end] is '-' or '+')
            {
                end++;
            }
            while (end < Text.Length && (char.IsAsciiDigit(Text[end]) || Text[end] == '.'))
            {
                end++;
            }
            while (end < Text.Length && (char.IsAsciiLetter(Text[end]) || Text[end] == '%'))
            {
                end++;
            }
            Take("tok-num", end - Index);
        }

        private bool IsNumberStart()
        {
            var c = Peek();
            if (char.IsAsciiDigit(c))
            {
                // Not a digit inside a name: `h1`, `col-2`.
                return Index == 0 || !(char.IsLetterOrDigit(Text[Index - 1]) || Text[Index - 1] is '-' or '_');
            }
            if (c == '.')
            {
                return char.IsAsciiDigit(Peek(1));
            }
            return c is '-' or '+' && (char.IsAsciiDigit(Peek(1)) || (Peek(1) == '.' && char.IsAsciiDigit(Peek(2))));
        }

        // A CSS identifier starts with a letter, an underscore, or a hyphen that a letter, a hyphen or an underscore follows.
        private bool IsNameStart(int at)
        {
            var c = at < Text.Length ? Text[at] : ' ';
            var next = at + 1 < Text.Length ? Text[at + 1] : ' ';
            return char.IsLetter(c) || c == '_' || (c == '-' && (char.IsLetter(next) || next is '-' or '_'));
        }

        private int NameLength(int from)
        {
            var end = from;
            while (end < Text.Length && (char.IsLetterOrDigit(Text[end]) || Text[end] is '-' or '_'))
            {
                end++;
            }
            return end - from;
        }
    }

    // ------------------------------------------------------------------------------------------------ JSON

    // JSON, with the comments a configuration file such as .mcp.json is allowed: a string followed by a ':' is a key.
    private sealed class JsonLexer(string code) : Scanner(code)
    {
        public string Run()
        {
            while (Index < Text.Length)
            {
                var c = Text[Index];
                if (At("//"))
                {
                    var end = Text.IndexOf('\n', Index);
                    Take("tok-com", (end < 0 ? Text.Length : end) - Index);
                }
                else if (At("/*"))
                {
                    Delimited("tok-com", 2, "*/");
                }
                else if (c == '"')
                {
                    var end = StringEnd(Index);
                    var next = end;
                    while (next < Text.Length && char.IsWhiteSpace(Text[next]))
                    {
                        next++;
                    }
                    Take(next < Text.Length && Text[next] == ':' ? "tok-attr" : "tok-str", end - Index);
                }
                else if (char.IsAsciiDigit(c) || (c == '-' && char.IsAsciiDigit(Peek(1))))
                {
                    var end = Index + 1;
                    while (end < Text.Length && (char.IsAsciiDigit(Text[end]) || Text[end] is '.' or 'e' or 'E' or '+' or '-'))
                    {
                        end++;
                    }
                    Take("tok-num", end - Index);
                }
                else if (char.IsAsciiLetter(c))
                {
                    var end = Index;
                    while (end < Text.Length && char.IsAsciiLetter(Text[end]))
                    {
                        end++;
                    }
                    var word = Text[Index..end];
                    if (word is "true" or "false" or "null")
                    {
                        Take("tok-key", word.Length);
                    }
                    else
                    {
                        Plain(word.Length);
                    }
                }
                else
                {
                    Plain(1);
                }
            }
            return Html;
        }
    }
}
