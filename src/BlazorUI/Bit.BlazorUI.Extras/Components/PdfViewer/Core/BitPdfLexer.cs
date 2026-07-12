// The lexer tokenizing PDF byte streams.

using System.Text;

namespace Bit.BlazorUI;

/// <summary>
/// Tokenizes a PDF byte stream into primitive objects: numbers, names,
/// strings, booleans, <c>null</c>, command keywords (<see cref="BitPdfCmd"/>) and the
/// structural delimiters <c>[ ] &lt;&lt; &gt;&gt; { }</c> (emitted as commands).
/// </summary>
public sealed class BitPdfLexer
{
    private readonly BitPdfBaseStream _stream;
    private int _currentChar;

    // PDF "regular" characters are everything except whitespace and delimiters.
    // 0x00, 0x09, 0x0A, 0x0C, 0x0D, 0x20 are whitespace; ( ) < > [ ] { } / % are delimiters.
    private static bool IsWhitespace(int ch) =>
        ch is 0x00 or 0x09 or 0x0A or 0x0C or 0x0D or 0x20;

    private static bool IsDelimiter(int ch) =>
        ch is '(' or ')' or '<' or '>' or '[' or ']' or '{' or '}' or '/' or '%';

    private static bool IsRegular(int ch) =>
        ch >= 0 && !IsWhitespace(ch) && !IsDelimiter(ch);

    public BitPdfLexer(BitPdfBaseStream stream)
    {
        _stream = stream;
        _currentChar = stream.GetByte();
    }

    /// <summary>Current stream read position (one past <see cref="_currentChar"/>).</summary>
    public int Pos => _stream.Pos;

    /// <summary>The underlying byte source being tokenized.</summary>
    public BitPdfBaseStream Stream => _stream;

    /// <summary>The most recently read character (one byte), or -1 at end of stream.</summary>
    public int CurrentChar => _currentChar;

    /// <summary>Repositions the lexer to <paramref name="pos"/> and re-reads the current character.</summary>
    public void Seek(int pos)
    {
        _stream.Pos = pos;
        _currentChar = _stream.GetByte();
    }

    private int NextChar() => _currentChar = _stream.GetByte();

    private int PeekChar() => _stream.PeekByte();

    /// <summary>
    /// Reads and returns the next object token. Returns <see cref="BitPdfPrimitives.EOF"/>
    /// at end of stream.
    /// </summary>
    public object GetObj()
    {
        // Skip whitespace and comments.
        bool comment = false;
        int ch = _currentChar;
        while (true)
        {
            if (ch < 0)
            {
                return BitPdfPrimitives.EOF;
            }
            if (comment)
            {
                if (ch is 0x0A or 0x0D)
                {
                    comment = false;
                }
            }
            else if (ch == '%')
            {
                comment = true;
            }
            else if (!IsWhitespace(ch))
            {
                break;
            }
            ch = NextChar();
        }

        // Dispatch on the leading character.
        switch (ch)
        {
            case '0': case '1': case '2': case '3': case '4':
            case '5': case '6': case '7': case '8': case '9':
            case '+': case '-': case '.':
                return GetNumber();
            case '(':
                return GetString();
            case '/':
                return GetName();
            case '[':
                NextChar();
                return BitPdfCmd.Get("[");
            case ']':
                NextChar();
                return BitPdfCmd.Get("]");
            case '<':
                ch = NextChar();
                if (ch == '<')
                {
                    NextChar();
                    return BitPdfCmd.Get("<<");
                }
                return GetHexString();
            case '>':
                ch = NextChar();
                if (ch == '>')
                {
                    NextChar();
                    return BitPdfCmd.Get(">>");
                }
                return BitPdfCmd.Get(">");
            case '{':
                NextChar();
                return BitPdfCmd.Get("{");
            case '}':
                NextChar();
                return BitPdfCmd.Get("}");
            case ')':
                // Stray close paren; skip and treat as an error token.
                NextChar();
                return BitPdfCmd.Get(")");
        }

        // Otherwise: a keyword / command (true, false, null, obj, R, BT, ...).
        var sb = new StringBuilder();
        sb.Append((char)ch);
        while ((ch = NextChar()) >= 0 && IsRegular(ch))
        {
            sb.Append((char)ch);
            if (sb.Length == 128)
            {
                throw new BitPdfFormatException("Command token too long.");
            }
        }

        string keyword = sb.ToString();
        return keyword switch
        {
            "true" => true,
            "false" => false,
            "null" => BitPdfPrimitives.Null,
            _ => BitPdfCmd.Get(keyword),
        };
    }

    /// <summary>Reads the raw keyword/command without object substitution (used for trailer scanning).</summary>
    public object GetObjRaw() => GetObj();

    private double GetNumber()
    {
        int ch = _currentChar;
        bool isReal = false;
        var sb = new StringBuilder();

        if (ch == '-')
        {
            sb.Append('-');
            ch = NextChar();
            // Collapse repeated signs, e.g. "--" -> "-".
            while (ch == '-')
            {
                ch = NextChar();
            }
        }
        else if (ch == '+')
        {
            ch = NextChar();
        }
        if (ch == '.')
        {
            isReal = true;
            sb.Append('.');
            ch = NextChar();
        }

        if (ch is < '0' or > '9' && ch != '.')
        {
            // Not actually a number (e.g. a lone sign). Treat as zero.
            if (sb.Length == 0 || sb.ToString() is "-" or ".")
            {
                return 0d;
            }
        }

        while (true)
        {
            if (ch is >= '0' and <= '9')
            {
                sb.Append((char)ch);
            }
            else if (ch == '.')
            {
                if (isReal)
                {
                    break; // second dot terminates the number
                }
                isReal = true;
                sb.Append('.');
            }
            else if (ch is 'e' or 'E')
            {
                // Exponent (rare, non-standard but produced by some writers).
                isReal = true;
                sb.Append('e');
                ch = NextChar();
                if (ch is '+' or '-')
                {
                    sb.Append((char)ch);
                }
                else
                {
                    continue;
                }
            }
            else if (ch == '-')
            {
                // Embedded minus inside a number is ignored.
            }
            else
            {
                break;
            }
            ch = NextChar();
        }

        string text = sb.ToString();
        if (text.Length == 0 || text is "-" or "." or "-.")
        {
            return 0d;
        }
        return double.TryParse(text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double value)
            ? value
            : 0d;
    }

    private BitPdfString GetString()
    {
        var bytes = new List<byte>();
        int depth = 1;
        int ch = NextChar(); // consume '('

        while (true)
        {
            if (ch < 0)
            {
                break; // unterminated string
            }

            if (ch == '\\')
            {
                ch = NextChar();
                switch (ch)
                {
                    case 'n': bytes.Add((byte)'\n'); break;
                    case 'r': bytes.Add((byte)'\r'); break;
                    case 't': bytes.Add((byte)'\t'); break;
                    case 'b': bytes.Add((byte)'\b'); break;
                    case 'f': bytes.Add((byte)'\f'); break;
                    case '(': bytes.Add((byte)'('); break;
                    case ')': bytes.Add((byte)')'); break;
                    case '\\': bytes.Add((byte)'\\'); break;
                    case 0x0D: // backslash-newline => line continuation
                        if (PeekChar() == 0x0A)
                        {
                            NextChar();
                        }
                        break;
                    case 0x0A:
                        break;
                    case >= '0' and <= '7':
                        // Up to three octal digits.
                        int code = ch - '0';
                        for (int i = 0; i < 2; i++)
                        {
                            int peek = PeekChar();
                            if (peek is < '0' or > '7')
                            {
                                break;
                            }
                            code = (code << 3) + (NextChar() - '0');
                        }
                        bytes.Add((byte)(code & 0xFF));
                        break;
                    default:
                        if (ch >= 0)
                        {
                            bytes.Add((byte)ch);
                        }
                        break;
                }
                ch = NextChar();
                continue;
            }

            if (ch == '(')
            {
                depth++;
            }
            else if (ch == ')')
            {
                if (--depth == 0)
                {
                    NextChar(); // consume ')'
                    break;
                }
            }

            bytes.Add((byte)ch);
            ch = NextChar();
        }

        return new BitPdfString(bytes.ToArray());
    }

    private BitPdfName GetName()
    {
        var sb = new StringBuilder();
        int ch;
        while ((ch = NextChar()) >= 0 && IsRegular(ch))
        {
            if (ch == '#')
            {
                int h1 = PeekChar();
                int d1 = HexValue(h1);
                if (d1 >= 0)
                {
                    NextChar();
                    int h2 = PeekChar();
                    int d2 = HexValue(h2);
                    if (d2 >= 0)
                    {
                        NextChar();
                        sb.Append((char)((d1 << 4) | d2));
                        continue;
                    }
                    sb.Append('#');
                    sb.Append((char)h1);
                    continue;
                }
            }
            if (sb.Length >= 127)
            {
                // The spec limits names to 127 bytes; rather than aborting the
                // whole content stream on an overlong name, truncate it and keep
                // consuming the remaining name characters so tokenizing resyncs.
                continue;
            }
            sb.Append((char)ch);
        }
        return BitPdfName.Get(sb.ToString());
    }

    private BitPdfString GetHexString()
    {
        var bytes = new List<byte>();
        int ch = _currentChar;
        int hi = -1;

        while (true)
        {
            if (ch < 0)
            {
                break;
            }
            if (ch == '>')
            {
                NextChar();
                break;
            }
            int digit = HexValue(ch);
            if (digit >= 0)
            {
                if (hi < 0)
                {
                    hi = digit;
                }
                else
                {
                    bytes.Add((byte)((hi << 4) | digit));
                    hi = -1;
                }
            }
            ch = NextChar();
        }

        if (hi >= 0)
        {
            // Odd number of digits: trailing nibble is treated as low 0.
            bytes.Add((byte)(hi << 4));
        }

        return new BitPdfString(bytes.ToArray());
    }

    private static int HexValue(int ch) => ch switch
    {
        >= '0' and <= '9' => ch - '0',
        >= 'A' and <= 'F' => ch - 'A' + 10,
        >= 'a' and <= 'f' => ch - 'a' + 10,
        _ => -1,
    };

    /// <summary>Skips forward to the start of the next line.</summary>
    public void SkipToNextLine()
    {
        int ch = _currentChar;
        while (ch >= 0)
        {
            if (ch == 0x0D)
            {
                ch = NextChar();
                if (ch == 0x0A)
                {
                    NextChar();
                }
                return;
            }
            if (ch == 0x0A)
            {
                NextChar();
                return;
            }
            ch = NextChar();
        }
    }
}
