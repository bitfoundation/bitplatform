using System.Text;

namespace Bit.Bswup.Demo.Server.Services;

/// <summary>One <c>&lt;target&gt;.&lt;name&gt; = &lt;value&gt;</c> assignment found in a script.</summary>
public record JsAssignment(string Name, string Value, int Index);

/// <summary>
/// The little bit of JavaScript reading the Bswup tools need: Bswup is configured in JavaScript,
/// so both the answers (the shipped <c>bit-bswup.sw.js</c> defaults) and the questions (an app's
/// own <c>service-worker.js</c>, pasted in for review) arrive as source text.
/// <para>
/// This is deliberately a scanner and not a parser: it tracks strings, template literals, regular
/// expression literals and comments so that a <c>//</c> inside a pattern is never mistaken for a
/// comment and a <c>;</c> inside a string never ends a statement - which is all that reading
/// `self.x = ...` assignments and regex literals actually requires. Anything beyond that (control
/// flow, computed settings) is reported as unreadable rather than guessed at.
/// </para>
/// </summary>
public static class JavaScriptSource
{
    /// <summary>
    /// The script with every comment replaced by spaces. Same length and same line breaks as the
    /// original, so offsets and line numbers still line up with what the author wrote.
    /// </summary>
    public static string StripComments(string code)
    {
        var builder = new StringBuilder(code.Length);
        var index = 0;
        var lastSignificant = '\0';

        while (index < code.Length)
        {
            var c = code[index];

            if (c == '/' && index + 1 < code.Length && (code[index + 1] == '/' || code[index + 1] == '*'))
            {
                int end;
                if (code[index + 1] == '*')
                {
                    var stop = code.IndexOf("*/", index + 2, StringComparison.Ordinal);
                    end = stop < 0 ? code.Length : stop + 2;
                }
                else
                {
                    var line = code.IndexOf('\n', index);
                    end = line < 0 ? code.Length : line;
                }

                // Newlines are kept so a line comment cannot swallow the statement below it.
                for (int i = index; i < end; i++) builder.Append(code[i] == '\n' ? '\n' : ' ');

                index = end;
                continue;
            }

            if (c is '\'' or '"' or '`' || (c == '/' && StartsRegex(lastSignificant)))
            {
                var end = SkipLiteral(code, index);
                builder.Append(code, index, end - index);
                index = end;
                lastSignificant = code[end - 1];
                continue;
            }

            builder.Append(c);
            if (char.IsWhiteSpace(c) is false) lastSignificant = c;
            index++;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Every <c>&lt;target&gt;.&lt;name&gt; = ...</c> assignment, in source order, with the assigned
    /// expression verbatim. Pass comment-stripped code (see <see cref="StripComments"/>).
    /// </summary>
    public static IReadOnlyList<JsAssignment> ReadAssignments(string code, string target)
    {
        var assignments = new List<JsAssignment>();
        var prefix = $"{target}.";
        var index = 0;

        while (true)
        {
            index = code.IndexOf(prefix, index, StringComparison.Ordinal);
            if (index < 0) break;

            var start = index;
            index += prefix.Length;

            // `x.self.foo` and `myself.foo` are not assignments to `self`.
            if (start > 0 && (char.IsLetterOrDigit(code[start - 1]) || code[start - 1] is '_' or '$' or '.'))
            {
                continue;
            }

            var nameEnd = index;
            while (nameEnd < code.Length && (char.IsLetterOrDigit(code[nameEnd]) || code[nameEnd] is '_' or '$')) nameEnd++;
            if (nameEnd == index) continue;

            var name = code[index..nameEnd];

            var equals = nameEnd;
            while (equals < code.Length && char.IsWhiteSpace(code[equals])) equals++;

            // Assignment only: `self.mode === 'x'` is a comparison, `self.errorTolerance ||= 'lax'`
            // is a defaulting assignment the shipped worker itself uses, so `=` and every compound
            // operator count while `==`/`===` and the `=>` of an arrow function do not.
            var valueStart = ReadAssignmentOperator(code, equals);
            if (valueStart < 0)
            {
                index = nameEnd;
                continue;
            }

            var valueEnd = FindStatementEnd(code, valueStart);

            assignments.Add(new JsAssignment(name, code[valueStart..valueEnd].Trim(), start));

            // Deliberately resumed right after the NAME rather than after the value: a value whose
            // end this scanner reads wrongly would otherwise swallow - and silently hide - every
            // assignment after it, which is the one failure mode a configuration review must not have.
            index = nameEnd;
        }

        return assignments;
    }

    /// <summary>
    /// The regular-expression and string literals of an expression, in order - the two entry
    /// shapes every Bswup URL-matching list accepts. A regex keeps its delimiters and flags
    /// (<c>/\.js$/i</c>); a string is returned quoted, so the caller can tell the two apart.
    /// </summary>
    public static IReadOnlyList<string> ReadLiterals(string expression)
    {
        var literals = new List<string>();
        var index = 0;
        var lastSignificant = '\0';

        while (index < expression.Length)
        {
            var c = expression[index];

            if (c is '\'' or '"' or '`' || (c == '/' && StartsRegex(lastSignificant)))
            {
                var end = SkipLiteral(expression, index);
                literals.Add(expression[index..end]);
                lastSignificant = expression[end - 1];
                index = end;
                continue;
            }

            if (char.IsWhiteSpace(c) is false) lastSignificant = c;
            index++;
        }

        return literals;
    }

    /// <summary>
    /// The body of the object literal assigned in <paramref name="declaration"/> (e.g.
    /// <c>const defaultoptions =</c>), without its braces - or null when the script has no such
    /// declaration.
    /// </summary>
    public static string? ReadObjectLiteral(string code, string declaration)
    {
        var index = code.IndexOf(declaration, StringComparison.Ordinal);
        if (index < 0) return null;

        var open = code.IndexOf('{', index + declaration.Length);
        if (open < 0) return null;

        var depth = 0;
        for (int i = open; i < code.Length; i++)
        {
            var c = code[i];

            if (c is '\'' or '"' or '`' || (c == '/' && i > open && StartsRegex(PreviousSignificant(code, i))))
            {
                i = SkipLiteral(code, i) - 1;
                continue;
            }

            if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return code[(open + 1)..i];
        }

        return null;
    }

    /// <summary>The <c>key: value</c> pairs directly inside an object-literal body, in order.</summary>
    public static IReadOnlyList<(string Key, string Value)> ReadObjectEntries(string body)
    {
        var entries = new List<(string, string)>();
        var index = 0;

        while (index < body.Length)
        {
            while (index < body.Length && (char.IsWhiteSpace(body[index]) || body[index] == ',')) index++;
            if (index >= body.Length) break;

            var nameEnd = index;
            while (nameEnd < body.Length && (char.IsLetterOrDigit(body[nameEnd]) || body[nameEnd] is '_' or '$')) nameEnd++;

            if (nameEnd == index || nameEnd >= body.Length || body[nameEnd] != ':')
            {
                // Not a plain `key:` - skip to the next top-level comma and try again. The scan
                // stops where it stands on a closing bracket, so the skip has to step over that
                // character itself or the loop would sit on it forever.
                var skipped = FindStatementEnd(body, index, ',');
                index = skipped > index ? skipped : index + 1;
                continue;
            }

            var key = body[index..nameEnd];
            var valueEnd = FindStatementEnd(body, nameEnd + 1, ',');

            entries.Add((key, body[(nameEnd + 1)..valueEnd].Trim()));

            index = valueEnd + 1;
        }

        return entries;
    }

    /// <summary>
    /// The end of the expression starting at <paramref name="start"/>: the first
    /// <paramref name="terminator"/> that is not inside a literal, bracket, brace or parenthesis -
    /// or the line break that ends it, for the semicolon-less files JavaScript also accepts.
    /// </summary>
    private static int FindStatementEnd(string code, int start, char terminator = ';')
    {
        var depth = 0;
        var lastSignificant = '=';

        for (int i = start; i < code.Length; i++)
        {
            var c = code[i];

            // Automatic semicolon insertion: at depth 0 a line break ends the statement unless the
            // last thing before it was an operator still waiting for a right-hand side. Without
            // this, one `self.isPassive = false` written without its semicolon swallows the whole
            // rest of the file as its value.
            if (c == '\n' && depth == 0 && ContinuesExpression(lastSignificant) is false) return i;

            if (c is '\'' or '"' or '`' || (c == '/' && StartsRegex(lastSignificant)))
            {
                i = SkipLiteral(code, i) - 1;
                lastSignificant = code[i];
                continue;
            }

            if (c is '(' or '[' or '{') depth++;
            else if (c is ')' or ']' or '}')
            {
                // A closing bracket at depth 0 belongs to the construct that contains this
                // expression (the end of an object literal, say), so the expression ends here.
                if (depth == 0) return i;
                depth--;
            }
            else if (c == terminator && depth == 0) return i;

            if (char.IsWhiteSpace(c) is false) lastSignificant = c;
        }

        return code.Length;
    }

    /// <summary>The end (exclusive) of the string, template or regex literal starting at <paramref name="start"/>.</summary>
    private static int SkipLiteral(string code, int start)
    {
        var quote = code[start];
        var inClass = false;

        for (int i = start + 1; i < code.Length; i++)
        {
            var c = code[i];

            if (c == '\\')
            {
                i++;
                continue;
            }

            if (quote == '/')
            {
                // A '/' inside a character class does not end the pattern: /[a-z/]+/ is one regex.
                if (c == '[') inClass = true;
                else if (c == ']') inClass = false;
                else if (c == '\n') return i;                    // an unterminated regex - stop at the line
                else if (c == '/' && inClass is false)
                {
                    var end = i + 1;
                    while (end < code.Length && char.IsLetter(code[end])) end++;   // the flags

                    return end;
                }

                continue;
            }

            if (c == quote) return i + 1;
            if (quote != '`' && c == '\n') return i;             // an unterminated string - stop at the line
        }

        return code.Length;
    }

    /// <summary>Every JavaScript assignment operator, longest first so `>>>=` wins over `>>=`.</summary>
    private static readonly string[] _assignmentOperators =
        [">>>=", "**=", "<<=", ">>=", "&&=", "||=", "??=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^="];

    /// <summary>
    /// The index just past the assignment operator at <paramref name="start"/>, or -1 when what is
    /// there is not one: `==` and `===` compare, and `=>` opens an arrow function.
    /// </summary>
    private static int ReadAssignmentOperator(string code, int start)
    {
        if (start >= code.Length) return -1;

        foreach (var op in _assignmentOperators)
        {
            if (string.CompareOrdinal(code, start, op, 0, op.Length) == 0) return start + op.Length;
        }

        if (code[start] != '=') return -1;
        if (start + 1 < code.Length && code[start + 1] is '=' or '>') return -1;

        return start + 1;
    }

    /// <summary>
    /// Whether an expression ending in <paramref name="last"/> is still waiting for more - which is
    /// what decides, at a line break, whether JavaScript would have inserted a semicolon there.
    /// </summary>
    private static bool ContinuesExpression(char last)
    {
        return last is '+' or '-' or '*' or '/' or '%' or '=' or '<' or '>' or '&' or '|' or '^'
                    or '!' or '~' or '?' or ':' or ',' or '.' or '(' or '[' or '{';
    }

    /// <summary>
    /// Whether a '/' following <paramref name="previous"/> starts a regular expression rather than
    /// a division. Division by a literal pattern is not a thing anyone writes in a configuration
    /// file, so the operand/operator distinction is enough.
    /// </summary>
    private static bool StartsRegex(char previous)
    {
        return previous is '\0' or '=' or '(' or ',' or ':' or '[' or '!' or '&' or '|' or '?'
                        or '{' or '}' or ';' or '+' or '-' or '*' or '~' or '^' or '%' or '<' or '>' or '\n';
    }

    private static char PreviousSignificant(string code, int index)
    {
        for (int i = index - 1; i >= 0; i--)
        {
            if (char.IsWhiteSpace(code[i]) is false) return code[i];
        }

        return '\0';
    }
}
