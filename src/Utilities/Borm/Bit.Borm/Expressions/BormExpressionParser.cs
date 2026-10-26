using System.Globalization;
using System.Text;

namespace Bit.Borm;

/// <summary>
/// Turns expression text into a tree of <see cref="BormExpressionNode"/>s. A hand-written recursive
/// descent parser over a fixed grammar, bounded in length, depth and size so a hostile document cannot
/// make it do unbounded work:
/// </summary>
/// <remarks>
/// <code>
/// expression := or
/// or         := and (('or' | '||') and)*
/// and        := not (('and' | '&amp;&amp;') not)*
/// not        := ('not' | '!') not | comparison
/// comparison := additive (('=' | '==' | '!=' | '&lt;&gt;' | '&lt;' | '&lt;=' | '&gt;' | '&gt;=' | 'in' | 'not in') additive)?
/// additive   := term (('+' | '-') term)*
/// term       := unary (('*' | '/' | '%') unary)*
/// unary      := ('-' | '+') unary | primary
/// primary    := number | 'text' | "text" | true | false | null | {path} | name '(' arguments? ')'
///             | '[' arguments? ']' | '(' expression ')'
/// </code>
/// Keywords are case-insensitive. Comparisons do not chain: <c>1 &lt; {x} &lt; 5</c> is refused rather
/// than read as <c>(1 &lt; {x}) &lt; 5</c>, which is never what it means.
/// </remarks>
internal sealed class BormExpressionParser
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "or", "not", "in", "true", "false", "null"
    };

    private readonly string _text;
    private int _pos;
    private int _depth;
    private int _nodes;

    private BormExpressionParser(string text) => _text = text;

    public static BormExpressionNode Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new BormExpressionException("An expression cannot be empty", text, 0);
        if (text.Length > BormExpression.MaxLength)
            throw new BormExpressionException($"An expression may be at most {BormExpression.MaxLength} characters long", text, BormExpression.MaxLength);

        var parser = new BormExpressionParser(text);
        var root = parser.ParseOr();
        parser.SkipWhitespace();
        if (parser._pos < text.Length)
            throw parser.Error($"Unexpected '{text[parser._pos]}'");
        return root;
    }

    internal static bool IsKeyword(string name) => Keywords.Contains(name);

    internal static bool IsIdentifier(string name)
    {
        if (name.Length == 0 || !(char.IsAsciiLetter(name[0]) || name[0] == '_')) return false;
        foreach (var c in name)
            if (!(char.IsAsciiLetterOrDigit(c) || c == '_')) return false;
        return true;
    }

    // ---------------------------------------------------------------- grammar

    private BormExpressionNode ParseOr()
    {
        Enter();
        var left = ParseAnd();
        while (TryKeyword("or") || TrySymbol("||"))
            left = Node(new BormLogicalNode(isAnd: false, left, ParseAnd()));
        Leave();
        return left;
    }

    private BormExpressionNode ParseAnd()
    {
        var left = ParseNot();
        while (TryKeyword("and") || TrySymbol("&&"))
            left = Node(new BormLogicalNode(isAnd: true, left, ParseNot()));
        return left;
    }

    private BormExpressionNode ParseNot()
    {
        // `not in` belongs to the comparison it follows, never to a `not` starting an operand.
        if (PeekKeyword("not") && !PeekKeywordPair("not", "in") || PeekSymbol("!") && !PeekSymbol("!="))
        {
            if (!TryKeyword("not")) TrySymbol("!");
            Enter();
            var operand = ParseNot();
            Leave();
            return Node(new BormUnaryNode(BormUnaryOperator.Not, operand));
        }
        return ParseComparison();
    }

    private BormExpressionNode ParseComparison()
    {
        var left = ParseAdditive();
        if (TryComparisonOperator() is not { } op) return left;

        var start = _pos;
        var node = Node(new BormBinaryNode(op, left, ParseAdditive()));
        if (TryComparisonOperator() is not null)
            throw new BormExpressionException("Comparisons cannot be chained; join them with 'and'", _text, start);
        return node;
    }

    private BormBinaryOperator? TryComparisonOperator()
    {
        if (TrySymbol("==") || TrySymbol("=")) return BormBinaryOperator.Equal;
        if (TrySymbol("!=") || TrySymbol("<>")) return BormBinaryOperator.NotEqual;
        if (TrySymbol("<=")) return BormBinaryOperator.LessOrEqual;
        if (TrySymbol(">=")) return BormBinaryOperator.GreaterOrEqual;
        if (TrySymbol("<")) return BormBinaryOperator.Less;
        if (TrySymbol(">")) return BormBinaryOperator.Greater;
        if (TryKeyword("in")) return BormBinaryOperator.In;
        if (PeekKeywordPair("not", "in"))
        {
            TryKeyword("not");
            TryKeyword("in");
            return BormBinaryOperator.NotIn;
        }
        return null;
    }

    private BormExpressionNode ParseAdditive()
    {
        var left = ParseTerm();
        while (true)
        {
            if (TrySymbol("+")) left = Node(new BormBinaryNode(BormBinaryOperator.Add, left, ParseTerm()));
            else if (TrySymbol("-")) left = Node(new BormBinaryNode(BormBinaryOperator.Subtract, left, ParseTerm()));
            else return left;
        }
    }

    private BormExpressionNode ParseTerm()
    {
        var left = ParseUnary();
        while (true)
        {
            if (TrySymbol("*")) left = Node(new BormBinaryNode(BormBinaryOperator.Multiply, left, ParseUnary()));
            else if (TrySymbol("/")) left = Node(new BormBinaryNode(BormBinaryOperator.Divide, left, ParseUnary()));
            else if (TrySymbol("%")) left = Node(new BormBinaryNode(BormBinaryOperator.Modulo, left, ParseUnary()));
            else return left;
        }
    }

    private BormExpressionNode ParseUnary()
    {
        if (TrySymbol("-") || TrySymbol("+"))
        {
            var negate = _text[_pos - 1] == '-';
            Enter();
            var operand = ParseUnary();
            Leave();
            return Node(new BormUnaryNode(negate ? BormUnaryOperator.Negate : BormUnaryOperator.Plus, operand));
        }
        return ParsePrimary();
    }

    private BormExpressionNode ParsePrimary()
    {
        SkipWhitespace();
        if (_pos >= _text.Length) throw Error("The expression ends where a value was expected");

        var c = _text[_pos];

        if (char.IsAsciiDigit(c) || (c == '.' && _pos + 1 < _text.Length && char.IsAsciiDigit(_text[_pos + 1])))
            return Node(new BormLiteralNode(ReadNumber()));

        if (c is '\'' or '"') return Node(new BormLiteralNode(ReadString(c)));

        if (c == '{') return Node(new BormFieldNode(ReadPath()));

        if (c == '(')
        {
            _pos++;
            var inner = ParseOr();
            Expect(')', "Missing ')'");
            return inner;
        }

        if (c == '[')
        {
            _pos++;
            Enter();
            var items = ReadArguments(']');
            Leave();
            return Node(new BormListNode(items));
        }

        if (char.IsAsciiLetter(c) || c == '_') return ParseName();

        throw Error($"Unexpected '{c}'");
    }

    private BormExpressionNode ParseName()
    {
        var start = _pos;
        while (_pos < _text.Length && (char.IsAsciiLetterOrDigit(_text[_pos]) || _text[_pos] == '_')) _pos++;
        var name = _text[start.._pos];

        switch (name.ToLowerInvariant())
        {
            case "true": return Node(new BormLiteralNode(true));
            case "false": return Node(new BormLiteralNode(false));
            case "null": return Node(new BormLiteralNode(null));
        }

        SkipWhitespace();
        if (_pos >= _text.Length || _text[_pos] != '(')
            throw new BormExpressionException(
                IsKeyword(name)
                    ? $"'{name}' cannot stand where a value is expected"
                    : $"'{name}' is not a value; field references are written in braces, {{{name}}}",
                _text, start);

        if (BormExpressionFunctions.Find(name) is not { } function)
            throw new BormExpressionException($"There is no function called '{name}'", _text, start);

        _pos++;
        Enter();
        var args = ReadArguments(')');
        Leave();

        if (args.Count < function.MinArgs || args.Count > function.MaxArgs)
            throw new BormExpressionException(ArityMessage(function, args.Count), _text, start);
        if (function.Check?.Invoke(args) is { } problem)
            throw new BormExpressionException(problem, _text, start);

        return Node(new BormCallNode(function, args));
    }

    private static string ArityMessage(BormExpressionFunction function, int given)
    {
        var expected = function.MinArgs == function.MaxArgs
            ? $"{function.MinArgs}"
            : function.MaxArgs == int.MaxValue ? $"at least {function.MinArgs}" : $"{function.MinArgs} to {function.MaxArgs}";
        return $"{function.Name}() takes {expected} argument{(function.MaxArgs == 1 && function.MinArgs == 1 ? "" : "s")}, not {given}";
    }

    private List<BormExpressionNode> ReadArguments(char close)
    {
        var args = new List<BormExpressionNode>();
        SkipWhitespace();
        if (_pos < _text.Length && _text[_pos] == close)
        {
            _pos++;
            return args;
        }

        while (true)
        {
            args.Add(ParseOr());
            SkipWhitespace();
            if (_pos < _text.Length && _text[_pos] == ',')
            {
                _pos++;
                continue;
            }
            Expect(close, $"Missing '{close}'");
            return args;
        }
    }

    // ---------------------------------------------------------------- tokens

    private object ReadNumber()
    {
        var start = _pos;
        while (_pos < _text.Length && char.IsAsciiDigit(_text[_pos])) _pos++;
        if (_pos < _text.Length && _text[_pos] == '.')
        {
            _pos++;
            while (_pos < _text.Length && char.IsAsciiDigit(_text[_pos])) _pos++;
        }
        if (_pos < _text.Length && _text[_pos] is 'e' or 'E')
        {
            var mark = _pos;
            _pos++;
            if (_pos < _text.Length && _text[_pos] is '+' or '-') _pos++;
            if (_pos >= _text.Length || !char.IsAsciiDigit(_text[_pos])) { _pos = mark; }
            else while (_pos < _text.Length && char.IsAsciiDigit(_text[_pos])) _pos++;
        }

        var token = _text.AsSpan(start, _pos - start);
        if (decimal.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var m)) return m;
        if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) && double.IsFinite(d)) return d;
        throw new BormExpressionException($"'{token}' is not a number this language can hold", _text, start);
    }

    private string ReadString(char quote)
    {
        var start = _pos;
        _pos++;
        var sb = new StringBuilder();
        while (_pos < _text.Length)
        {
            var c = _text[_pos++];
            if (c == quote)
            {
                // A doubled quote is the quote itself, as SQL writes it: 'it''s'.
                if (_pos < _text.Length && _text[_pos] == quote)
                {
                    sb.Append(quote);
                    _pos++;
                    continue;
                }
                return sb.ToString();
            }
            if (c == '\\' && _pos < _text.Length)
            {
                // Only the escapes that mean something are taken. Any other backslash is kept, because
                // the text it most often appears in is a pattern — matches({Zip}, '^\d{5}$') — and
                // eating it would quietly turn \d into d.
                var escaped = _text[_pos];
                switch (escaped)
                {
                    case 'n': sb.Append('\n'); _pos++; continue;
                    case 't': sb.Append('\t'); _pos++; continue;
                    case 'r': sb.Append('\r'); _pos++; continue;
                    case '\\' or '\'' or '"': sb.Append(escaped); _pos++; continue;
                    default: sb.Append('\\'); continue;
                }
            }
            sb.Append(c);
        }
        throw new BormExpressionException("Text is missing its closing quote", _text, start);
    }

    /// <summary>
    /// Reads <c>{path}</c>. The path is a form path — names joined by dots, with <c>[n]</c> for a row
    /// and <c>[]</c> for every row — and is checked here, so a typo in an index is reported when the
    /// schema is loaded rather than read as "no value" forever after.
    /// </summary>
    private string ReadPath()
    {
        var start = _pos;
        var close = _text.IndexOf('}', _pos);
        if (close < 0) throw new BormExpressionException("A field reference is missing its closing '}'", _text, start);

        var path = _text[(start + 1)..close].Trim();
        _pos = close + 1;
        if (path.Length == 0) throw new BormExpressionException("A field reference names no field", _text, start);
        if (!IsValidPath(path))
            throw new BormExpressionException($"'{path}' is not a field path; write names joined by '.', with [0] for a row or [] for every row", _text, start);
        return path;
    }

    private static bool IsValidPath(string path)
    {
        var expectName = true;
        for (var i = 0; i < path.Length; i++)
        {
            var c = path[i];
            if (c == '.')
            {
                if (expectName || i == path.Length - 1) return false;
                expectName = true;
            }
            else if (c == '[')
            {
                if (i == 0) return false;
                var end = path.IndexOf(']', i);
                if (end < 0) return false;
                for (var j = i + 1; j < end; j++)
                    if (!char.IsAsciiDigit(path[j])) return false;
                i = end;
                expectName = false;
            }
            else if (c is ']' or '{')
            {
                return false;
            }
            else
            {
                expectName = false;
            }
        }
        return !expectName;
    }

    // ---------------------------------------------------------------- helpers

    private T Node<T>(T node) where T : BormExpressionNode
    {
        if (++_nodes > BormExpression.MaxNodes)
            throw new BormExpressionException($"An expression may have at most {BormExpression.MaxNodes} parts", _text, _pos);
        return node;
    }

    private void Enter()
    {
        if (++_depth > BormExpression.MaxDepth)
            throw new BormExpressionException($"An expression may nest at most {BormExpression.MaxDepth} levels deep", _text, _pos);
    }

    private void Leave() => _depth--;

    private void SkipWhitespace()
    {
        while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos])) _pos++;
    }

    private bool PeekSymbol(string symbol)
    {
        SkipWhitespace();
        return string.CompareOrdinal(_text, _pos, symbol, 0, symbol.Length) == 0;
    }

    private bool TrySymbol(string symbol)
    {
        if (!PeekSymbol(symbol)) return false;
        // Longer symbols are always tried before their prefixes (`<=` and `<>` before `<`, `==` before
        // `=`), so taking the first match is never taking half of a longer one.
        _pos += symbol.Length;
        return true;
    }

    private bool PeekKeyword(string keyword)
    {
        SkipWhitespace();
        return KeywordAt(_pos, keyword);
    }

    private bool PeekKeywordPair(string first, string second)
    {
        if (!PeekKeyword(first)) return false;
        var after = _pos + first.Length;
        while (after < _text.Length && char.IsWhiteSpace(_text[after])) after++;
        return KeywordAt(after, second);
    }

    /// <summary>Whether <paramref name="keyword"/> stands at <paramref name="position"/> as a whole word — "not", never the start of "notes".</summary>
    private bool KeywordAt(int position, string keyword)
    {
        if (position + keyword.Length > _text.Length) return false;
        if (string.Compare(_text, position, keyword, 0, keyword.Length, StringComparison.OrdinalIgnoreCase) != 0) return false;
        var end = position + keyword.Length;
        return end == _text.Length || !(char.IsAsciiLetterOrDigit(_text[end]) || _text[end] == '_');
    }

    private bool TryKeyword(string keyword)
    {
        if (!PeekKeyword(keyword)) return false;
        _pos += keyword.Length;
        return true;
    }

    private void Expect(char c, string message)
    {
        SkipWhitespace();
        if (_pos >= _text.Length || _text[_pos] != c) throw Error(message);
        _pos++;
    }

    private BormExpressionException Error(string message) => new(message, _text, Math.Min(_pos, _text.Length));
}
