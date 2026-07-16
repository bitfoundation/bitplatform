// The object parser for PDF token streams.

namespace Bit.BlazorUI;

/// <summary>
/// Builds composite PDF objects from the token stream produced by a
/// <see cref="BitPdfLexer"/>: arrays, dictionaries, indirect references
/// (<c>n g R</c>), indirect objects (<c>n g obj ... endobj</c>) and streams.
/// Uses two-token lookahead.
/// </summary>
public sealed class BitPdfParser
{
    private readonly BitPdfLexer _lexer;
    private readonly IBitPdfXRef? _xref;
    private readonly bool _allowStreams;

    private object _buf1;
    private object _buf2;

    // Guards against hostile deeply-nested arrays/dicts ([[[[…]]]]) that would
    // otherwise recurse until the process dies with an uncatchable
    // StackOverflowException. 512 comfortably exceeds any real document.
    private const int MaxDepth = 512;
    private int _depth;

    public BitPdfParser(BitPdfLexer lexer, IBitPdfXRef? xref = null, bool allowStreams = true)
    {
        _lexer = lexer;
        _xref = xref;
        _allowStreams = allowStreams;
        _buf1 = _lexer.GetObj();
        _buf2 = _lexer.GetObj();
    }

    private void Shift()
    {
        _buf1 = _buf2;
        _buf2 = _lexer.GetObj();
    }

    private static bool BufIsCmd(object buf, string cmd) => buf is BitPdfCmd c && c.Value == cmd;

    private static bool IsInteger(double value) => value == Math.Floor(value) && !double.IsInfinity(value);

    /// <summary>
    /// Parses and returns the next object. Composite values (arrays, dicts,
    /// streams) are fully assembled; <c>n g R</c> becomes a <see cref="BitPdfRef"/>.
    /// </summary>
    public object GetObj()
    {
        if (++_depth > MaxDepth)
        {
            _depth--;
            throw new BitPdfFormatException("PDF object nesting too deep.");
        }
        try
        {
            return GetObjCore();
        }
        finally
        {
            _depth--;
        }
    }

    private object GetObjCore()
    {
        object buf1 = _buf1;

        // Array: [ obj obj ... ]
        if (BufIsCmd(buf1, "["))
        {
            Shift();
            var array = new List<object?>();
            while (!BufIsCmd(_buf1, "]") && !ReferenceEquals(_buf1, BitPdfPrimitives.EOF))
            {
                array.Add(GetObj());
            }
            if (ReferenceEquals(_buf1, BitPdfPrimitives.EOF))
            {
                throw new BitPdfFormatException("End of file inside array.");
            }
            Shift(); // consume ']'
            return array;
        }

        // Dictionary: << /Key val ... >>  (optionally followed by a stream)
        if (BufIsCmd(buf1, "<<"))
        {
            Shift();
            var dict = new BitPdfDict(_xref);
            while (!BufIsCmd(_buf1, ">>") && !ReferenceEquals(_buf1, BitPdfPrimitives.EOF))
            {
                if (_buf1 is not BitPdfName key)
                {
                    Shift(); // skip a malformed key
                    continue;
                }
                Shift();
                if (ReferenceEquals(_buf1, BitPdfPrimitives.EOF) || BufIsCmd(_buf1, ">>"))
                {
                    break;
                }
                dict.Set(key.Value, GetObj());
            }

            if (ReferenceEquals(_buf1, BitPdfPrimitives.EOF))
            {
                throw new BitPdfFormatException("End of file inside dictionary.");
            }

            // When _buf1 == ">>" and _buf2 == "stream", the lexer is positioned
            // immediately after the "stream" keyword, so we can read raw bytes.
            if (_allowStreams && BufIsCmd(_buf2, "stream"))
            {
                return ParseStream(dict);
            }

            Shift(); // consume '>>'
            return dict;
        }

        // Possible indirect reference / object: <int> <int> (R|obj)
        if (buf1 is double n1 && IsInteger(n1) && _buf2 is double n2 && IsInteger(n2))
        {
            int num = (int)n1;
            int gen = (int)n2;
            object third = _lexer.GetObj();
            if (third is BitPdfCmd c)
            {
                if (c.Value == "R")
                {
                    _buf1 = _lexer.GetObj();
                    _buf2 = _lexer.GetObj();
                    return new BitPdfRef(num, gen);
                }
                if (c.Value == "obj")
                {
                    _buf1 = _lexer.GetObj();
                    _buf2 = _lexer.GetObj();
                    object inner = GetObj();
                    if (BufIsCmd(_buf1, "endobj"))
                    {
                        Shift();
                    }
                    return inner;
                }
            }
            // Not a ref/obj: restore lookahead and return the first number.
            // Return the original double (not the int `num`) so that downstream
            // `is double` checks continue to work for integer-valued operands.
            _buf1 = _buf2;
            _buf2 = third;
            return n1;
        }

        // Simple token: number, name, string, bool, null, or a bare command.
        Shift();
        return buf1;
    }

    private BitPdfStream ParseStream(BitPdfDict dict)
    {
        var source = (BitPdfStream)_lexer.Stream;
        byte[] buffer = source.Buffer;

        // The lexer has just produced the "stream" keyword as _buf2, so its
        // current character is the first byte after "stream".
        int i = _lexer.Pos - 1; // index of the lexer's current character

        // Skip the end-of-line marker (CRLF or LF) following "stream".
        if (i < buffer.Length && buffer[i] == 0x0D)
        {
            i++;
        }
        if (i < buffer.Length && buffer[i] == 0x0A)
        {
            i++;
        }

        int dataStart = i;
        int dataEnd;

        object? lengthObj = dict.Get("Length");
        int declaredLength = lengthObj is double dl && IsInteger(dl) ? (int)dl : -1;

        if (declaredLength >= 0
            && dataStart + declaredLength <= buffer.Length
            && LooksLikeEndstream(buffer, dataStart + declaredLength))
        {
            dataEnd = dataStart + declaredLength;
        }
        else
        {
            dataEnd = FindEndstream(buffer, dataStart);
        }

        var streamObj = new BitPdfStream(buffer, dataStart, dataEnd - dataStart, dict);

        // Resume tokenizing just past "endstream".
        int resume = SkipPastEndstream(buffer, dataEnd);
        _lexer.Seek(resume);
        _buf1 = _lexer.GetObj();
        _buf2 = _lexer.GetObj();

        // Consume a trailing "endobj" if this stream was an indirect object body.
        if (BufIsCmd(_buf1, "endobj"))
        {
            Shift();
        }

        return streamObj;
    }

    private static readonly byte[] EndstreamKeyword = "endstream"u8.ToArray();

    private static bool LooksLikeEndstream(byte[] buffer, int at)
    {
        int i = at;
        while (i < buffer.Length && (buffer[i] == 0x0D || buffer[i] == 0x0A || buffer[i] == 0x20))
        {
            i++;
        }
        return MatchesAt(buffer, i, EndstreamKeyword);
    }

    private static int FindEndstream(byte[] buffer, int start)
    {
        for (int i = start; i <= buffer.Length - EndstreamKeyword.Length; i++)
        {
            if (MatchesAt(buffer, i, EndstreamKeyword))
            {
                int end = i;
                if (end > start && buffer[end - 1] == 0x0A)
                {
                    end--;
                }
                if (end > start && buffer[end - 1] == 0x0D)
                {
                    end--;
                }
                return end;
            }
        }
        return buffer.Length;
    }

    private static int SkipPastEndstream(byte[] buffer, int dataEnd)
    {
        int i = dataEnd;
        while (i < buffer.Length && (buffer[i] == 0x0D || buffer[i] == 0x0A || buffer[i] == 0x20))
        {
            i++;
        }
        if (MatchesAt(buffer, i, EndstreamKeyword))
        {
            i += EndstreamKeyword.Length;
        }
        return i;
    }

    private static bool MatchesAt(byte[] buffer, int at, byte[] keyword)
    {
        if (at < 0 || at + keyword.Length > buffer.Length)
        {
            return false;
        }
        for (int k = 0; k < keyword.Length; k++)
        {
            if (buffer[at + k] != keyword[k])
            {
                return false;
            }
        }
        return true;
    }
}
