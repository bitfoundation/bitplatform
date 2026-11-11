namespace Bit.BlazorUI;

/// <summary>
/// Turns source code into coloured spans.
/// </summary>
/// <remarks>
/// <para>
/// Lexical rather than syntactic, and therefore total: it produces sensible output for a fragment
/// that is still being written, which a grammar-based highlighter cannot. That matters more here
/// than perfect fidelity, because most code this framework renders is half-finished most of the time.
/// </para>
/// <para>
/// Only non-plain spans are returned, in ascending order and never overlapping. A renderer walks
/// the tokens and emits the gaps between them as unstyled text, so whitespace and ordinary
/// identifiers cost nothing.
/// </para>
/// </remarks>
public static class BitAiSyntaxHighlighter
{
    /// <summary>
    /// Highlights source code.
    /// </summary>
    /// <param name="code">The source.</param>
    /// <param name="language">The language name from the fence info string.</param>
    /// <returns>
    /// The non-plain spans, in order. Empty when the language is unknown, which renders as plain text.
    /// </returns>
    public static IReadOnlyList<BitAiCodeToken> Highlight(string? code, string? language)
    {
        if (string.IsNullOrEmpty(code)) return [];

        var definition = BitAiLanguageRegistry.Find(language);
        if (definition is null) return [];

        var tokens = new List<BitAiCodeToken>(Math.Max(8, code.Length / 8));

        if (definition.Id == "diff") ScanDiff(code, tokens);
        else if (definition.IsMarkup) ScanMarkup(code, definition, tokens);
        else ScanGeneral(code, definition, tokens);

        return tokens;
    }

    private static void ScanGeneral(string code, BitAiLanguageDefinition language, List<BitAiCodeToken> tokens)
    {
        var comparison = language.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var i = 0;

        while (i < code.Length)
        {
            var c = code[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            // Before comments, because the "//" in url(https://example.com) is not one.
            if (language.UrlFunctions && TryUrlFunction(code, i, tokens, out var urlEnd))
            {
                i = urlEnd;
                continue;
            }

            if (TryLineComment(code, i, language, out var commentEnd))
            {
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Comment, i, commentEnd - i));
                i = commentEnd;
                continue;
            }

            if (TryBlockComment(code, i, language, out var blockEnd))
            {
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Comment, i, blockEnd - i));
                i = blockEnd;
                continue;
            }

            if (language.Lifetimes && c == '\'' && TryLifetime(code, i, out var lifetimeEnd))
            {
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Label, i, lifetimeEnd - i));
                i = lifetimeEnd;
                continue;
            }

            if (language.VariablePrefix == c && TryVariable(code, i, out var variableEnd))
            {
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Variable, i, variableEnd - i));
                i = variableEnd;
                continue;
            }

            if (language.HyphenatedCommands && c == '-' && TryParameter(code, i, language, comparison, tokens, out var parameterEnd))
            {
                i = parameterEnd;
                continue;
            }

            if (language.StringDelimiters.Contains(c))
            {
                var end = ScanString(code, i, language);

                // A quoted name followed by a colon is a key ("name": in JSON, "a key": in YAML), quotes and all, the
                // way a bare word followed by one is in these languages (Classify). A value is followed by anything else.
                var kind = language.ColonProperties && end > i + 1 && code[end - 1] == c && NextNonSpace(code, end) == ':'
                    ? BitAiTokenKind.Property
                    : BitAiTokenKind.String;

                tokens.Add(new BitAiCodeToken(kind, i, end - i));
                i = end;
                continue;
            }

            if (char.IsAsciiDigit(c) || (c == '.' && i + 1 < code.Length && char.IsAsciiDigit(code[i + 1])))
            {
                var end = ScanNumber(code, i);
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Number, i, end - i));
                i = end;
                continue;
            }

            if (language.AnnotationPrefix == c && TryAnnotation(code, i, out var annotationEnd))
            {
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Attribute, i, annotationEnd - i));
                i = annotationEnd;
                continue;
            }

            if (IsWordStart(c))
            {
                var end = i;
                while (end < code.Length && IsWordChar(code[end])) end++;

                // Verb-Noun is a command whatever the words are, so Get-ChildItem is a function
                // even though neither half is followed by a parenthesis.
                if (language.HyphenatedCommands && end + 1 < code.Length && code[end] == '-' && char.IsLetter(code[end + 1]))
                {
                    while (end + 1 < code.Length && code[end] == '-' && char.IsLetter(code[end + 1]))
                    {
                        end++;
                        while (end < code.Length && IsWordChar(code[end])) end++;
                    }

                    tokens.Add(new BitAiCodeToken(BitAiTokenKind.Function, i, end - i));
                    i = end;
                    continue;
                }

                var word = code[i..end];
                tokens.Add(new BitAiCodeToken(Classify(word, code, end, language, comparison), i, end - i));
                i = end;
                continue;
            }

            if (IsPunctuation(c))
            {
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Punctuation, i, 1));
                i++;
                continue;
            }

            if (IsOperator(c))
            {
                var end = i;
                while (end < code.Length && IsOperator(code[end])) end++;
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Operator, i, end - i));
                i = end;
                continue;
            }

            i++;
        }
    }

    private static BitAiTokenKind Classify(
        string word,
        string code,
        int end,
        BitAiLanguageDefinition language,
        StringComparison comparison)
    {
        if (Contains(language.Constants, word, comparison)) return BitAiTokenKind.Constant;
        if (Contains(language.Keywords, word, comparison)) return BitAiTokenKind.Keyword;
        if (Contains(language.Types, word, comparison)) return BitAiTokenKind.Type;

        var next = NextNonSpace(code, end);
        if (next == '(') return BitAiTokenKind.Function;
        if (language.ColonProperties && next == ':') return BitAiTokenKind.Property;
        if (char.IsUpper(word[0]) && word.Length > 1) return BitAiTokenKind.Type;

        return BitAiTokenKind.Plain;
    }

    private static bool Contains(IReadOnlySet<string> set, string word, StringComparison comparison)
    {
        if (comparison == StringComparison.Ordinal) return set.Contains(word);
        foreach (var candidate in set)
        {
            if (string.Equals(candidate, word, comparison)) return true;
        }

        return false;
    }

    private static bool IsLineStart(string code, int index)
    {
        for (var i = index - 1; i >= 0; i--)
        {
            if (code[i] == '\n') return true;
            if (code[i] is not (' ' or '\t' or '\r')) return false;
        }

        return true;
    }

    // An annotation is its prefix and a name, or a path such as tokio::main. A bracket only
    // introduces one at the start of a line; elsewhere, as in items[index], it is an indexer. A hash
    // may be followed by "[" or "![", which is how Rust writes #[derive] and #![allow].
    private static bool TryAnnotation(string code, int start, out int end)
    {
        var prefix = code[start];
        end = start + 1;

        if (prefix == '[' && !IsLineStart(code, start)) return false;
        if (prefix == '#')
        {
            if (end < code.Length && code[end] == '!') end++;
            if (end < code.Length && code[end] == '[') end++;
            else end = start + 1;
        }

        if (end >= code.Length || !IsWordStart(code[end])) return false;

        while (end < code.Length)
        {
            if (IsWordChar(code[end])) end++;
            else if (MatchesAt(code, end, "::") && end + 2 < code.Length && IsWordStart(code[end + 2])) end += 2;
            else break;
        }

        return true;
    }

    // A sigil and a name, as in $name, $env:Path, $_, or ${a name}. A sigil on its own is an operator.
    private static bool TryVariable(string code, int start, out int end)
    {
        end = start + 1;
        if (end >= code.Length) return false;

        if (code[end] == '{')
        {
            var close = code.IndexOf('}', end);
            if (close < 0) return false;
            end = close + 1;
            return true;
        }

        if (code[end] is '_' or '$' or '?' or '^')
        {
            end++;
            while (end < code.Length && IsWordChar(code[end])) end++;
            return true;
        }

        if (!char.IsLetter(code[end])) return false;
        while (end < code.Length && (IsWordChar(code[end]) || (code[end] == ':' && end + 1 < code.Length && IsWordStart(code[end + 1])))) end++;
        return true;
    }

    // A hyphen and a word, after whitespace or at the start: a keyword such as -eq when the
    // language lists it, otherwise a parameter such as -Recurse.
    private static bool TryParameter(string code, int start, BitAiLanguageDefinition language, StringComparison comparison, List<BitAiCodeToken> tokens, out int end)
    {
        end = start + 1;
        if (end >= code.Length || !char.IsLetter(code[end])) return false;
        if (start > 0 && !char.IsWhiteSpace(code[start - 1]) && code[start - 1] is not ('(' or '{' or ',' or ';' or '|')) return false;

        while (end < code.Length && IsWordChar(code[end])) end++;
        var word = code[start..end];
        var kind = Contains(language.Keywords, word, comparison) ? BitAiTokenKind.Keyword : BitAiTokenKind.Attribute;
        tokens.Add(new BitAiCodeToken(kind, start, end - start));
        return true;
    }

    // A Rust lifetime or loop label, such as 'a, 'static, or 'outer. A name closed by a second
    // apostrophe, as in 'a', is a character literal instead.
    private static bool TryLifetime(string code, int start, out int end)
    {
        end = start + 1;
        if (end >= code.Length || !(char.IsLetter(code[end]) || code[end] == '_')) return false;

        while (end < code.Length && (char.IsLetterOrDigit(code[end]) || code[end] == '_')) end++;
        return end >= code.Length || code[end] != '\'';
    }

    // Reads url(...) as the function name and one string argument, quoted or not, so that neither
    // the "//" of https:// nor a parenthesis inside quotes ends it early.
    private static bool TryUrlFunction(string code, int start, List<BitAiCodeToken> tokens, out int end)
    {
        end = start;
        if (!code.AsSpan(start).StartsWith("url(", StringComparison.OrdinalIgnoreCase)) return false;
        if (start > 0 && (IsWordChar(code[start - 1]) || code[start - 1] == '-')) return false;

        var open = start + 3;
        var i = open + 1;
        var quote = '\0';

        while (i < code.Length && code[i] != '\n')
        {
            var c = code[i];
            if (c == '\\' && i + 1 < code.Length) { i += 2; continue; }
            if (quote != '\0') { if (c == quote) quote = '\0'; }
            else if (c is '"' or '\'') quote = c;
            else if (c == ')') break;
            i++;
        }

        tokens.Add(new BitAiCodeToken(BitAiTokenKind.Function, start, 3));
        tokens.Add(new BitAiCodeToken(BitAiTokenKind.Punctuation, open, 1));
        if (i > open + 1) tokens.Add(new BitAiCodeToken(BitAiTokenKind.String, open + 1, i - open - 1));

        end = i;
        if (i < code.Length && code[i] == ')')
        {
            tokens.Add(new BitAiCodeToken(BitAiTokenKind.Punctuation, i, 1));
            end = i + 1;
        }

        return true;
    }

    private static char NextNonSpace(string code, int index)
    {
        for (var i = index; i < code.Length; i++)
        {
            if (code[i] is ' ' or '\t') continue;
            return code[i];
        }

        return '\0';
    }

    private static bool TryLineComment(string code, int index, BitAiLanguageDefinition language, out int end)
    {
        end = index;
        foreach (var marker in language.LineComments)
        {
            if (!MatchesAt(code, index, marker)) continue;
            var newline = code.IndexOf('\n', index);
            end = newline < 0 ? code.Length : newline;
            return true;
        }

        return false;
    }

    private static bool TryBlockComment(string code, int index, BitAiLanguageDefinition language, out int end)
    {
        end = index;
        foreach (var (open, close) in language.BlockComments)
        {
            if (!MatchesAt(code, index, open)) continue;
            var closeIndex = code.IndexOf(close, index + open.Length, StringComparison.Ordinal);
            end = closeIndex < 0 ? code.Length : closeIndex + close.Length;
            return true;
        }

        return false;
    }

    private static bool MatchesAt(string code, int index, string value) =>
        value.Length > 0 && index + value.Length <= code.Length
        && code.AsSpan(index, value.Length).SequenceEqual(value);

    private static int ScanString(string code, int start, BitAiLanguageDefinition language)
    {
        var quote = code[start];

        // A tripled delimiter opens a string that runs to the matching triple across lines: Python
        // docstrings, C# raw strings, and Java or Kotlin text blocks. Read as three ordinary
        // strings, the apostrophe in """It's""" would open a stray literal.
        if (start + 2 < code.Length && code[start + 1] == quote && code[start + 2] == quote)
        {
            var triple = new string(quote, 3);
            for (var j = start + 3; j < code.Length; j++)
            {
                if (language.StringEscapes && code[j] == language.StringEscapeCharacter) { j++; continue; }
                if (MatchesAt(code, j, triple)) return j + 3;
            }

            return code.Length;
        }

        var i = start + 1;

        while (i < code.Length)
        {
            var c = code[i];

            if (language.StringEscapes && c == language.StringEscapeCharacter && i + 1 < code.Length)
            {
                i += 2;
                continue;
            }

            if (c == quote) return i + 1;
            if (c == '\n' && !language.MultiLineStrings && quote != '`') return i;
            i++;
        }

        return code.Length;
    }

    private static int ScanNumber(string code, int start)
    {
        var i = start;
        if (i + 1 < code.Length && code[i] == '0' && (code[i + 1] is 'x' or 'X' or 'b' or 'B' or 'o' or 'O'))
        {
            i += 2;
            while (i < code.Length && (char.IsAsciiLetterOrDigit(code[i]) || code[i] == '_')) i++;
            return i;
        }

        var seenDot = false;
        var seenExponent = false;

        while (i < code.Length)
        {
            var c = code[i];
            if (char.IsAsciiDigit(c) || c == '_') { i++; continue; }
            if (c == '.' && !seenDot && !seenExponent) { seenDot = true; i++; continue; }
            if ((c is 'e' or 'E') && !seenExponent && i + 1 < code.Length
                && (char.IsAsciiDigit(code[i + 1]) || code[i + 1] is '+' or '-'))
            {
                seenExponent = true;
                i += 2;
                continue;
            }

            if (c is 'f' or 'F' or 'd' or 'D' or 'm' or 'M' or 'L' or 'l' or 'u' or 'U') { i++; continue; }
            break;
        }

        return i;
    }

    private static bool IsWordStart(char c) => char.IsLetter(c) || c == '_' || c == '$';

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '$';

    private static bool IsPunctuation(char c) => c is '(' or ')' or '[' or ']' or '{' or '}' or ',' or ';';

    private static bool IsOperator(char c) =>
        c is '+' or '-' or '*' or '/' or '%' or '=' or '<' or '>' or '!' or '&' or '|' or '^' or '~'
            or '?' or ':' or '.' or '@' or '#';

    private static void ScanMarkup(string code, BitAiLanguageDefinition language, List<BitAiCodeToken> tokens)
    {
        var i = 0;

        while (i < code.Length)
        {
            var open = code.IndexOf('<', i);
            if (open < 0) break;

            if (MatchesAt(code, open, "<!--"))
            {
                var close = code.IndexOf("-->", open, StringComparison.Ordinal);
                var end = close < 0 ? code.Length : close + 3;
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.Comment, open, end - open));
                i = end;
                continue;
            }

            var tagEnd = code.IndexOf('>', open);
            if (tagEnd < 0) tagEnd = code.Length - 1;

            ScanTag(code, open, tagEnd, tokens);
            i = tagEnd + 1;
        }
    }

    private static void ScanTag(string code, int open, int close, List<BitAiCodeToken> tokens)
    {
        var i = open + 1;
        if (i < code.Length && code[i] == '/') i++;

        var nameStart = i;
        while (i <= close && i < code.Length && (char.IsLetterOrDigit(code[i]) || code[i] is '-' or '_' or ':')) i++;
        if (i > nameStart) tokens.Add(new BitAiCodeToken(BitAiTokenKind.Tag, nameStart, i - nameStart));

        while (i < close && i < code.Length)
        {
            var c = code[i];

            if (char.IsWhiteSpace(c) || c is '/' or '=') { i++; continue; }

            if (c is '"' or '\'')
            {
                var end = ScanString(code, i, BitAiBuiltInLanguages.Html with { StringDelimiters = ['"', '\''] });
                end = Math.Min(end, close + 1);
                tokens.Add(new BitAiCodeToken(BitAiTokenKind.String, i, end - i));
                i = end;
                continue;
            }

            var attributeStart = i;
            while (i < close && i < code.Length && !char.IsWhiteSpace(code[i]) && code[i] is not ('=' or '/' or '>')) i++;
            if (i > attributeStart) tokens.Add(new BitAiCodeToken(BitAiTokenKind.Attribute, attributeStart, i - attributeStart));
            else i++;
        }
    }

    private static void ScanDiff(string code, List<BitAiCodeToken> tokens)
    {
        var i = 0;

        while (i < code.Length)
        {
            var newline = code.IndexOf('\n', i);
            var end = newline < 0 ? code.Length : newline;
            var length = end - i;

            if (length > 0)
            {
                var kind = code[i] switch
                {
                    '+' when !MatchesAt(code, i, "+++") => BitAiTokenKind.Inserted,
                    '-' when !MatchesAt(code, i, "---") => BitAiTokenKind.Deleted,
                    '@' => BitAiTokenKind.Meta,
                    '+' or '-' => BitAiTokenKind.Meta,
                    'd' when MatchesAt(code, i, "diff ") => BitAiTokenKind.Meta,
                    'i' when MatchesAt(code, i, "index ") => BitAiTokenKind.Meta,
                    _ => BitAiTokenKind.Plain,
                };

                if (kind != BitAiTokenKind.Plain) tokens.Add(new BitAiCodeToken(kind, i, length));
            }

            i = end + 1;
        }
    }
}
