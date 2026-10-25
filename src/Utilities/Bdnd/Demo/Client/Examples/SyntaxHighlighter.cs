using System.Text;

namespace Bit.Bdnd.Demo.Client.Examples;

/// <summary>
/// A small, dependency-free Razor/C# syntax highlighter for the demo's code snippets. It returns HTML in
/// which every token is wrapped in a <c>&lt;span class="tok-*"&gt;</c> and all text is already encoded, to
/// render inside the site kit's <c>pre.bb-code</c>; the token colours are in wwwroot/demo.css.
/// <para>
/// It reads a .razor file the way the Razor compiler does - markup, with C# behind every <c>@</c>: an
/// <c>@code</c> block, a control statement whose body switches back to markup on a line that starts with a
/// tag, an expression inside an attribute value, a render-fragment template. It is pragmatic rather than a
/// full lexer: it covers the constructs the demo pages use, and tells a type from a member by the shape of
/// the code around it rather than by knowing the types.
/// </para>
/// </summary>
public static class SyntaxHighlighter
{
    /// <summary>
    /// Highlights <paramref name="code"/> - <c>razor</c> (a .razor file, the default) or <c>csharp</c> (plain C#,
    /// such as a Program.cs excerpt) - and returns HTML that is safe to render verbatim.
    /// </summary>
    public static string Highlight(string code, string language = "razor")
    {
        code = code.Replace("\r\n", "\n").Replace('\r', '\n');
        try
        {
            return new Lexer(code).Run(csharp: string.Equals(language, "csharp", StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // A snippet the lexer trips over is still worth showing, uncoloured.
            return System.Net.WebUtility.HtmlEncode(code);
        }
    }

    private sealed class Lexer(string s)
    {
        // HTML elements that never have content or an end tag.
        private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase)
        {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr",
        };

        private static readonly HashSet<string> Keywords = new(StringComparer.Ordinal)
        {
            "abstract", "and", "as", "async", "await", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "dynamic",
            "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach",
            "get", "global", "goto", "if", "implicit", "in", "init", "int", "interface", "internal", "is", "lock",
            "long", "nameof", "namespace", "new", "nint", "not", "null", "object", "operator", "or", "out",
            "override", "params", "partial", "private", "protected", "public", "readonly", "record", "ref",
            "required", "return", "sbyte", "sealed", "set", "short", "sizeof", "stackalloc", "static", "string",
            "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe",
            "ushort", "using", "var", "virtual", "void", "volatile", "when", "where", "while", "with", "yield",
        };

        // The words after which a name is a type: `new Card(...)`, `record Note`, `is BoxItem`, `@inject Foo`.
        private static readonly HashSet<string> TypeIntroducers = new(StringComparer.Ordinal)
        {
            "new", "class", "record", "struct", "interface", "enum", "is", "as", "not", "typeof", "sizeof",
            "inject", "layout", "inherits", "implements", "typeparam",
        };

        // The words that can follow a value but never a type, so `Item is null` keeps Item a member.
        private static readonly HashSet<string> OperatorWords = new(StringComparer.Ordinal)
        {
            "is", "as", "and", "or", "not", "when", "with", "switch", "in",
        };

        // Razor directives whose argument runs to the end of the line.
        private static readonly HashSet<string> LineDirectives = new(StringComparer.Ordinal)
        {
            "using", "namespace", "page", "inject", "layout", "inherits", "implements", "typeparam", "attribute",
            "rendermode", "preservewhitespace",
        };

        // Razor control statements: a keyword, usually a condition in parentheses, then a body.
        private static readonly HashSet<string> ControlKeywords = new(StringComparer.Ordinal)
        {
            "if", "for", "foreach", "while", "switch", "lock", "do", "try",
        };

        private readonly StringBuilder _sb = new(s.Length * 2);
        private int _i;

        // The last significant C# character: in a code block, a '<' after '{', '}', ';' or ':' starts a line of
        // markup rather than a comparison or a generic.
        private char _last = '{';
        // The last word, when the last significant C# token was one: `new` makes the name after it a type.
        private string? _prevWord;
        // How deep the lexer is in generic type arguments, `List<Card>`: the names in there are types.
        private int _generic;
        // The name just read was a type with its '<' right after it: that '<' opens type arguments.
        private bool _openGeneric;
        // Inside an attribute's brackets at the start of a member, `[Parameter, EditorRequired]`.
        private bool _attribute;

        public string Run(bool csharp)
        {
            if (csharp)
            {
                while (_i < s.Length) CSharpToken();
            }
            else
            {
                MarkupContent(null);
            }
            return _sb.ToString();
        }

        // ---------------------------------------------------------------------------------------------- Markup

        // Markup up to the end tag of `element`, or to the end of the source when it is null: text, tags,
        // comments, and the @ transitions into C#.
        private void MarkupContent(string? element)
        {
            while (_i < s.Length)
            {
                var c = s[_i];
                if (c == '@' && IsTransition())
                {
                    Transition();
                    continue;
                }
                if (c == '<')
                {
                    if (At("<!--"))
                    {
                        Delimited("tok-com", 4, "-->");
                        continue;
                    }
                    if (Peek(1) == '/' && IsNameStart(Peek(2)))
                    {
                        if (EndTag() == element && element is not null) return;
                        continue;
                    }
                    if (IsNameStart(Peek(1)))
                    {
                        Element();
                        continue;
                    }
                }
                Plain(1);
            }
        }

        // A start tag and - unless it closes itself or is a void element - its content up to the matching end tag.
        private void Element()
        {
            var (name, selfClosing) = StartTag();
            if (!selfClosing && !VoidElements.Contains(name))
            {
                MarkupContent(name);
            }
        }

        private (string Name, bool SelfClosing) StartTag()
        {
            Take("tok-pun", 1); // <
            var name = ReadName();
            // A component is a type: it takes the type's colour, an HTML element the tag colour.
            Take(char.IsUpper(name[0]) ? "tok-type" : "tok-tag", name.Length);

            while (_i < s.Length)
            {
                var c = s[_i];
                if (c == '>')
                {
                    Take("tok-pun", 1);
                    return (name, false);
                }
                if (c == '/' && Peek(1) == '>')
                {
                    Take("tok-pun", 2);
                    return (name, true);
                }
                if (At("@*"))
                {
                    Delimited("tok-com", 2, "*@");
                    continue;
                }
                if (c == '@' && IsNameStart(Peek(1)))
                {
                    // A directive attribute - @onclick, @key, @bind-Value:event - whose value is C#.
                    Take("tok-raz", 1 + ReadName(_i + 1).Length);
                    if (Peek() == '=')
                    {
                        Plain(1);
                        AttributeValue(csharp: true);
                    }
                    continue;
                }
                if (IsNameStart(c))
                {
                    Take("tok-attr", ReadName().Length);
                    if (Peek() == '=')
                    {
                        Plain(1);
                        AttributeValue(csharp: false);
                    }
                    continue;
                }
                if (c == '@')
                {
                    Transition();
                    continue;
                }
                Plain(1);
            }
            return (name, true);
        }

        // `</name>`; returns the name.
        private string EndTag()
        {
            Take("tok-pun", 2);
            var name = ReadName();
            Take(char.IsUpper(name[0]) ? "tok-type" : "tok-tag", name.Length);
            while (_i < s.Length && char.IsWhiteSpace(s[_i])) Plain(1);
            if (Peek() == '>') Take("tok-pun", 1);
            return name;
        }

        // An attribute's value: a string with @ expressions in it, or - after a directive attribute - C#.
        private void AttributeValue(bool csharp)
        {
            var quote = Peek();
            if (quote is not ('"' or '\''))
            {
                if (quote == '@')
                {
                    Transition();
                    return;
                }
                var end = _i;
                while (end < s.Length && !char.IsWhiteSpace(s[end]) && s[end] != '>' && !(s[end] == '/' && end + 1 < s.Length && s[end + 1] == '>')) end++;
                Take("tok-str", end - _i);
                return;
            }

            Take("tok-str", 1);
            if (csharp && Peek() != '@')
            {
                // C# up to the closing quote; a quote inside parentheses is a C# string.
                EnterCSharp('=');
                var depth = 0;
                while (_i < s.Length && !(depth == 0 && s[_i] == quote))
                {
                    var c = s[_i];
                    if (c is '(' or '[' or '{') depth++;
                    else if (c is ')' or ']' or '}') depth--;
                    CSharpToken();
                }
            }
            else
            {
                var start = _i;
                while (_i < s.Length && s[_i] != quote)
                {
                    if (s[_i] == '@' && IsTransition())
                    {
                        if (Peek(1) == '@')
                        {
                            _i += 2;
                            continue;
                        }
                        Span("tok-str", start, _i);
                        Transition();
                        start = _i;
                        continue;
                    }
                    _i++;
                }
                Span("tok-str", start, _i);
            }
            if (_i < s.Length) Take("tok-str", 1);
        }

        // ------------------------------------------------------------------------------------------- Razor

        // An @ that is not part of a word (an e-mail address keeps its @).
        private bool IsTransition() => _i == 0 || !char.IsLetterOrDigit(s[_i - 1]);

        // From an @: an escaped @@, a comment, or the switch into C# - an expression, a block, a template,
        // a directive or a control statement.
        private void Transition()
        {
            var next = Peek(1);
            if (next == '@')
            {
                Plain(2);
                return;
            }
            if (next == '*')
            {
                Delimited("tok-com", 2, "*@");
                return;
            }
            if (next == ':')
            {
                MarkupLine();
                return;
            }
            var template = next == '<' && IsNameStart(Peek(2));
            if (next is not ('(' or '{') && !template && !IsIdentStart(next))
            {
                Plain(1);
                return;
            }

            Take("tok-raz", 1);
            EnterCSharp('@');

            if (next == '(')
            {
                CSharpGroup();
                return;
            }
            if (next == '{')
            {
                CodeBlock();
                return;
            }
            if (template)
            {
                // A template, `@<div>...</div>`: a render fragment written as markup.
                Element();
                return;
            }

            var word = ReadIdent(_i);
            if (word is "code" or "functions")
            {
                Take("tok-key", word.Length);
                Whitespace();
                if (Peek() == '{') CodeBlock();
                return;
            }
            if (LineDirectives.Contains(word))
            {
                Take("tok-key", word.Length);
                DirectiveLine(word);
                return;
            }
            if (ControlKeywords.Contains(word))
            {
                ControlStatement();
                return;
            }
            ImplicitExpression();
        }

        // `@:` - the rest of the line is markup text.
        private void MarkupLine()
        {
            Take("tok-raz", 2);
            while (_i < s.Length && s[_i] != '\n')
            {
                if (s[_i] == '@' && IsTransition()) Transition();
                else Plain(1);
            }
        }

        // The argument of a directive, to the end of the line: a namespace stays plain, anything else is C#.
        private void DirectiveLine(string directive)
        {
            if (directive is "using" or "namespace")
            {
                while (_i < s.Length && s[_i] != '\n') Plain(1);
                return;
            }
            _prevWord = directive;
            while (_i < s.Length && s[_i] != '\n') CSharpToken();
        }

        // `@if (...) { } else if (...) { } else { }`, `@foreach (...) { }`, `@try { } catch (...) { }` and the like.
        private void ControlStatement()
        {
            while (true)
            {
                var word = ReadIdent(_i);
                Take("tok-key", word.Length);
                _prevWord = word;
                Whitespace();

                if (word == "do")
                {
                    if (Peek() == '{') CodeBlock();
                    if (!TryChain("while")) return;
                    Take("tok-key", 5);
                    Whitespace();
                    if (Peek() == '(') CSharpGroup();
                    if (Peek() == ';') Plain(1);
                    return;
                }

                if (Peek() == '(')
                {
                    CSharpGroup();
                    Whitespace();
                }
                if (word == "else" && ReadIdent(_i) == "if") continue;
                if (Peek() == '{') CodeBlock();

                var chained = word switch
                {
                    "if" => TryChain("else"),
                    "try" or "catch" => TryChain("catch") || TryChain("finally"),
                    _ => false,
                };
                if (!chained) return;
            }
        }

        // Whether `keyword` follows, after any whitespace; when it does the whitespace is emitted and the
        // lexer stops on the keyword, otherwise nothing is consumed.
        private bool TryChain(string keyword)
        {
            var j = _i;
            while (j < s.Length && char.IsWhiteSpace(s[j])) j++;
            if (ReadIdent(j) != keyword) return false;
            while (_i < j) Plain(1);
            return true;
        }

        // `@name`, `@box.Name`, `@Show(layer.Offset)`, `@_items[0].Title`: a name, then member accesses,
        // calls and indexers - and nothing after a '.' that is the end of a sentence.
        private void ImplicitExpression()
        {
            Identifier();
            while (_i < s.Length)
            {
                var c = s[_i];
                if (c is '(' or '[')
                {
                    CSharpGroup();
                    continue;
                }
                var dot = c == '.' ? 1 : c == '?' && Peek(1) == '.' ? 2 : 0;
                if (dot > 0 && IsIdentStart(Peek(dot)))
                {
                    Plain(dot);
                    _last = '.';
                    Identifier();
                    continue;
                }
                break;
            }
        }

        // ---------------------------------------------------------------------------------------------- C#

        // A C# block - an @code body, @{ }, the body of an @if or @foreach - in which a line that starts
        // with a tag (or @:) is markup, and an @ switches to an expression or a nested statement.
        private void CodeBlock()
        {
            var depth = 0;
            while (_i < s.Length)
            {
                var c = s[_i];
                if (c == '<' && IsNameStart(Peek(1)) && _last is '{' or '}' or ';' or ':')
                {
                    Element();
                    _last = ';';
                    continue;
                }
                if (c == '@' && !IsStringStart())
                {
                    Transition();
                    _last = ';';
                    continue;
                }
                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}' && --depth == 0)
                {
                    CSharpToken();
                    return;
                }
                CSharpToken();
            }
        }

        // A bracketed C# group starting at '(' or '[' - a condition, an explicit expression, arguments -
        // through its matching close.
        private void CSharpGroup()
        {
            var depth = 0;
            while (_i < s.Length)
            {
                var c = s[_i];
                if (c == '@' && Peek(1) == '<' && IsNameStart(Peek(2)))
                {
                    Take("tok-raz", 1);
                    Element();
                    continue;
                }
                if (c is '(' or '[' or '{') depth++;
                else if (c is ')' or ']' or '}') depth--;
                CSharpToken();
                if (depth == 0) return;
            }
        }

        // One C# token: whitespace, a comment, a string, a number, a name or a punctuation character.
        private void CSharpToken()
        {
            var c = s[_i];
            if (char.IsWhiteSpace(c))
            {
                Plain(1);
                return;
            }
            if (At("//"))
            {
                var end = s.IndexOf('\n', _i);
                Take("tok-com", (end < 0 ? s.Length : end) - _i);
                return;
            }
            if (At("/*"))
            {
                Delimited("tok-com", 2, "*/");
                return;
            }
            if (At("@*"))
            {
                Delimited("tok-com", 2, "*@");
                return;
            }

            var openGeneric = _openGeneric;
            _openGeneric = false;

            if (c is '"' or '\'' || IsStringStart())
            {
                StringLiteral();
                Value();
                return;
            }
            if (char.IsDigit(c))
            {
                Number();
                Value();
                return;
            }
            if (IsIdentStart(c))
            {
                Identifier();
                return;
            }

            if (c == '<' && openGeneric) _generic++;
            else if (c == '>' && _generic > 0) _generic--;
            else if (c is ';' or '{' or '}') _generic = 0;

            // An attribute opens a member or follows another one; a '[' anywhere else is an index or a collection.
            if (c == '[' && _last is '{' or '}' or ';' or ']' or '@') _attribute = true;
            else if (c == ']') _attribute = false;

            Plain(1);
            _last = c;
            _prevWord = null;
        }

        // A name: a keyword, a method (a call or a declaration), a type, or a plain member or local.
        private void Identifier()
        {
            var start = _i;
            var word = ReadIdent(_i);
            _i += word.Length;

            var next = _i;
            while (next < s.Length && s[next] is ' ' or '\t') next++;
            var nc = next < s.Length ? s[next] : '\0';
            var upper = char.IsUpper(word[0]);

            string? cls;
            if (Keywords.Contains(word))
            {
                cls = "tok-key";
            }
            else if (_last == '.')
            {
                cls = nc == '(' ? "tok-fn" : upper && (_generic > 0 || _attribute) ? "tok-type" : null;
            }
            else if (_prevWord is not null && TypeIntroducers.Contains(_prevWord))
            {
                cls = "tok-type";
            }
            else if (nc == '(')
            {
                cls = _attribute ? "tok-type" : "tok-fn";
            }
            else
            {
                cls = upper && IsTypePosition(next, nc) ? "tok-type" : null;
            }

            if (cls is null) Plain(start, _i);
            else Span(cls, start, _i);

            _openGeneric = Peek() == '<' && cls is "tok-type" or "tok-fn";
            _last = 'a';
            _prevWord = word;
        }

        // Whether a capitalised name that is not called is used as a type: in type arguments or an attribute,
        // or followed by a declared name, type arguments, a nullable or array mark, or a static member access.
        private bool IsTypePosition(int next, char nc)
        {
            if (_generic > 0 || _attribute) return true;
            if (next == _i)
            {
                if (nc == '<') return true;
                if (nc == '?')
                {
                    var k = next + 1;
                    while (k < s.Length && s[k] is ' ' or '\t') k++;
                    return k < s.Length && (IsIdentStart(s[k]) || s[k] is '>' or ',' or ')' or '[');
                }
                if (nc == '[') return next + 1 < s.Length && s[next + 1] is ']' or ',';
            }
            if (nc == '.') return true;
            return IsIdentStart(nc) && !OperatorWords.Contains(ReadIdent(next));
        }

        // A string or char literal: regular, verbatim (@"..."), interpolated ($"...{x}...", its holes
        // coloured as C#) or raw ("""...""").
        private void StringLiteral()
        {
            var start = _i;
            var interpolated = false;
            var verbatim = false;
            while (s[_i] is '$' or '@')
            {
                interpolated |= s[_i] == '$';
                verbatim |= s[_i] == '@';
                _i++;
            }

            if (s[_i] == '\'')
            {
                _i++;
                while (_i < s.Length && s[_i] is not ('\'' or '\n')) _i += s[_i] == '\\' ? 2 : 1;
                _i = Math.Min(_i + 1, s.Length);
                Span("tok-str", start, _i);
                return;
            }

            if (At("\"\"\""))
            {
                var quotes = 0;
                while (_i < s.Length && s[_i] == '"')
                {
                    quotes++;
                    _i++;
                }
                var close = s.IndexOf(new string('"', quotes), _i, StringComparison.Ordinal);
                _i = close < 0 ? s.Length : close + quotes;
                Span("tok-str", start, _i);
                return;
            }

            _i++; // the opening quote
            var segment = start;
            while (_i < s.Length)
            {
                var c = s[_i];
                if (!verbatim && c == '\\')
                {
                    _i += 2;
                    continue;
                }
                if (c == '"')
                {
                    if (verbatim && Peek(1) == '"')
                    {
                        _i += 2;
                        continue;
                    }
                    _i++;
                    break;
                }
                if (!verbatim && c == '\n') break;
                if (interpolated && c == '{')
                {
                    if (Peek(1) == '{')
                    {
                        _i += 2;
                        continue;
                    }
                    Span("tok-str", segment, _i);
                    Plain(1);
                    Hole(verbatim);
                    segment = _i;
                    continue;
                }
                _i++;
            }
            _i = Math.Min(_i, s.Length);
            Span("tok-str", segment, _i);
        }

        // The C# inside an interpolation hole, through its closing '}'; a format after ':' is part of the string.
        private void Hole(bool verbatim)
        {
            var depth = 0;
            while (_i < s.Length)
            {
                var c = s[_i];
                if (depth == 0 && c == '}')
                {
                    Plain(1);
                    return;
                }
                if (depth == 0 && c == ':')
                {
                    var end = _i;
                    while (end < s.Length && s[end] is not ('}' or '"' or '\n')) end++;
                    Take("tok-str", end - _i);
                    continue;
                }
                if (!verbatim && c == '\n') return;
                if (c is '(' or '[' or '{') depth++;
                else if (c is ')' or ']' or '}') depth--;
                CSharpToken();
            }
        }

        private void Number()
        {
            var start = _i;
            if (At("0x") || At("0X"))
            {
                _i += 2;
                while (_i < s.Length && (char.IsAsciiHexDigit(s[_i]) || s[_i] == '_')) _i++;
            }
            else
            {
                while (_i < s.Length && (char.IsDigit(s[_i]) || s[_i] == '_')) _i++;
                if (Peek() == '.' && char.IsDigit(Peek(1)))
                {
                    _i++;
                    while (_i < s.Length && char.IsDigit(s[_i])) _i++;
                }
                if (Peek() is 'e' or 'E' && (char.IsDigit(Peek(1)) || (Peek(1) is '+' or '-' && char.IsDigit(Peek(2)))))
                {
                    _i += 2;
                    while (_i < s.Length && char.IsDigit(s[_i])) _i++;
                }
            }
            while (_i < s.Length && char.IsAsciiLetter(s[_i])) _i++; // a suffix: f, m, L, UL
            Span("tok-num", start, _i);
        }

        // Starts a stretch of C# that owes nothing to the C# before it, `last` standing for what precedes it.
        private void EnterCSharp(char last)
        {
            _last = last;
            _prevWord = null;
            _generic = 0;
            _openGeneric = false;
            _attribute = false;
        }

        // A literal was the last token: it ends whatever came before it.
        private void Value()
        {
            _last = 'a';
            _prevWord = null;
        }

        // At the start of a string literal: a quote, or the $ and @ prefixes of one.
        private bool IsStringStart()
        {
            var j = _i;
            while (j < s.Length && j - _i < 3 && s[j] is '$' or '@') j++;
            return j > _i && j < s.Length && s[j] == '"';
        }

        // ----------------------------------------------------------------------------------------- Helpers

        private char Peek(int offset = 0) => _i + offset < s.Length ? s[_i + offset] : '\0';

        private bool At(string text) => s.AsSpan(_i).StartsWith(text, StringComparison.Ordinal);

        private static bool IsIdentStart(char c) => char.IsLetter(c) || c == '_';

        private static bool IsNameStart(char c) => char.IsLetter(c);

        private string ReadIdent(int from)
        {
            var end = from;
            while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '_')) end++;
            return s[from..end];
        }

        // A tag or attribute name: letters, digits and - _ : . (aria-label, bind-Value:event).
        private string ReadName(int? from = null)
        {
            var start = from ?? _i;
            var end = start;
            while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] is '-' or '_' or ':' or '.')) end++;
            return s[start..end];
        }

        // An opener of `openLength` characters, then everything through `close`: a comment.
        private void Delimited(string cls, int openLength, string close)
        {
            var end = s.IndexOf(close, Math.Min(_i + openLength, s.Length), StringComparison.Ordinal);
            Take(cls, (end < 0 ? s.Length : end + close.Length) - _i);
        }

        private void Whitespace()
        {
            while (_i < s.Length && char.IsWhiteSpace(s[_i])) Plain(1);
        }

        // Emits the next `length` characters as a token and moves past them.
        private void Take(string cls, int length)
        {
            Span(cls, _i, _i + length);
            _i += length;
        }

        private void Span(string cls, int start, int end)
        {
            if (end <= start) return;
            _sb.Append("<span class=\"").Append(cls).Append("\">");
            Plain(start, end);
            _sb.Append("</span>");
        }

        // Emits the next `length` characters uncoloured and moves past them.
        private void Plain(int length)
        {
            Plain(_i, _i + length);
            _i += length;
        }

        private void Plain(int start, int end)
        {
            for (var k = start; k < end && k < s.Length; k++)
            {
                switch (s[k])
                {
                    case '&': _sb.Append("&amp;"); break;
                    case '<': _sb.Append("&lt;"); break;
                    case '>': _sb.Append("&gt;"); break;
                    default: _sb.Append(s[k]); break;
                }
            }
        }
    }
}
