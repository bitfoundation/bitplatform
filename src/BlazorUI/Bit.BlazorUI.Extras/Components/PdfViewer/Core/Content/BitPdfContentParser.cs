// Content-stream preprocessing into operations.

namespace Bit.BlazorUI;

/// <summary>
/// Parses a decoded page content stream into a flat list of
/// <see cref="BitPdfOperation"/>s. Operands are accumulated until an operator keyword
/// is read; inline images (<c>BI … ID … EI</c>) are captured as a single op.
/// </summary>
public sealed class BitPdfContentParser
{
    private readonly BitPdfLexer _lexer;

    public BitPdfContentParser(byte[] content) => _lexer = new BitPdfLexer(new BitPdfStream(content));

    /// <summary>Reads all operations from the content stream.</summary>
    public List<BitPdfOperation> Parse()
    {
        var operations = new List<BitPdfOperation>();
        var operands = new List<object?>();

        while (true)
        {
            object token = _lexer.GetObj();
            if (ReferenceEquals(token, BitPdfPrimitives.EOF))
            {
                break;
            }

            if (token is BitPdfCmd cmd)
            {
                string op = cmd.Value;

                // Structural delimiters can appear as operands (arrays/dicts).
                if (op == "[")
                {
                    operands.Add(ReadArray());
                    continue;
                }
                if (op == "<<")
                {
                    operands.Add(ReadDict());
                    continue;
                }

                if (op == "BI")
                {
                    operations.Add(ReadInlineImage());
                    operands.Clear();
                    continue;
                }

                operations.Add(new BitPdfOperation(op, operands));
                operands = new List<object?>();
                continue;
            }

            // An operand: number, name, string, bool, or null.
            operands.Add(token);
            if (operands.Count > 64)
            {
                // Guard against runaway operand accumulation in malformed content,
                // but keep the most recent operands: the next valid operator still
                // needs its (few) trailing operands, so drop the oldest, not all.
                operands.RemoveAt(0);
            }
        }

        return operations;
    }

    private List<object?> ReadArray()
    {
        var array = new List<object?>();
        while (true)
        {
            object token = _lexer.GetObj();
            if (ReferenceEquals(token, BitPdfPrimitives.EOF) || (token is BitPdfCmd { Value: "]" }))
            {
                break;
            }
            if (token is BitPdfCmd { Value: "[" })
            {
                array.Add(ReadArray());
            }
            else
            {
                array.Add(token);
            }
        }
        return array;
    }

    private BitPdfDict ReadDict()
    {
        var dict = new BitPdfDict();
        while (true)
        {
            object token = _lexer.GetObj();
            if (ReferenceEquals(token, BitPdfPrimitives.EOF) || token is BitPdfCmd { Value: ">>" })
            {
                break;
            }
            if (token is not BitPdfName key)
            {
                continue;
            }
            object value = _lexer.GetObj();
            if (value is BitPdfCmd { Value: "[" })
            {
                dict.Set(key.Value, ReadArray());
            }
            else if (value is BitPdfCmd { Value: "<<" })
            {
                dict.Set(key.Value, ReadDict());
            }
            else
            {
                dict.Set(key.Value, value);
            }
        }
        return dict;
    }

    private BitPdfOperation ReadInlineImage()
    {
        // Parse the inline-image dictionary (key/value pairs up to ID), then
        // capture the raw image bytes up to the EI marker.
        var dict = new BitPdfDict();
        while (true)
        {
            object token = _lexer.GetObj();
            if (ReferenceEquals(token, BitPdfPrimitives.EOF) || token is BitPdfCmd { Value: "ID" })
            {
                break;
            }
            if (token is not BitPdfName key)
            {
                continue;
            }
            object value = _lexer.GetObj();
            if (value is BitPdfCmd { Value: "[" })
            {
                dict.Set(key.Value, ReadArray());
            }
            else if (value is BitPdfCmd { Value: "<<" })
            {
                // Nested dictionary value, e.g. /DecodeParms << /K -1 … >>.
                dict.Set(key.Value, ReadDict());
            }
            else
            {
                dict.Set(key.Value, value);
            }
        }

        byte[] data = ReadInlineImageData(dict);
        return new BitPdfOperation("INLINE_IMAGE", new List<object?> { dict, data });
    }

    private byte[] ReadInlineImageData(BitPdfDict dict)
    {
        // After "ID" exactly one whitespace byte separates the keyword from data.
        BitPdfBaseStream stream = _lexer.Stream;
        var buffer = ((BitPdfStream)stream).Buffer;
        int pos = _lexer.Pos - 1; // current char position

        // Skip the whitespace after ID. A single space or LF is standard, but
        // tolerate a full CRLF pair so the image data starts at the right byte.
        if (pos < buffer.Length && buffer[pos] == 0x0D)
        {
            pos++;
        }
        if (pos < buffer.Length && (buffer[pos] is 0x20 or 0x0A))
        {
            pos++;
        }

        int start = pos;

        // For an unfiltered image the exact data length is W*H*components*BPC,
        // so we can skip straight past it and avoid a false "EI" match inside
        // binary sample data.
        int rawLen = UnfilteredLength(dict);
        int scanFrom = rawLen >= 0 && start + rawLen <= buffer.Length ? start + rawLen : start;

        // Scan for the "EI" delimiter surrounded by whitespace.
        for (int i = scanFrom; i + 1 < buffer.Length; i++)
        {
            if (buffer[i] == (byte)'E' && buffer[i + 1] == (byte)'I'
                && (i == 0 || IsWhitespaceByte(buffer[i - 1]))
                && (i + 2 >= buffer.Length || IsWhitespaceByte(buffer[i + 2])))
            {
                int end = i;
                _lexer.Seek(i + 2);
                int len = rawLen >= 0 ? rawLen : end - start;
                return buffer[start..(start + len)];
            }
        }

        _lexer.Seek(buffer.Length);
        return buffer[start..];
    }

    private static bool IsWhitespaceByte(byte b) => b is 0x00 or 0x09 or 0x0A or 0x0C or 0x0D or 0x20;

    /// <summary>
    /// The exact byte length of an <em>unfiltered</em> inline image
    /// (W×H rows of components×BPC bits, byte-aligned per row), or -1 when the
    /// image is filtered (compressed length is not predictable) or the geometry
    /// is unknown.
    /// </summary>
    private static int UnfilteredLength(BitPdfDict dict)
    {
        if (dict.Get("Filter", "F") is not null)
        {
            return -1;
        }
        int w = AsInt(dict.Get("Width", "W"));
        int h = AsInt(dict.Get("Height", "H"));
        if (w <= 0 || h <= 0)
        {
            return -1;
        }

        bool imageMask = dict.Get("ImageMask", "IM") is bool im && im;
        int bpc = imageMask ? 1 : AsInt(dict.Get("BitsPerComponent", "BPC"));
        if (bpc <= 0)
        {
            return -1;
        }
        int comps = imageMask ? 1 : ComponentCount(dict.Get("ColorSpace", "CS"));
        if (comps <= 0)
        {
            return -1;
        }

        long rowBytes = ((long)w * comps * bpc + 7) / 8;
        long total = rowBytes * h;
        return total is > 0 and <= int.MaxValue ? (int)total : -1;
    }

    private static int ComponentCount(object? cs) => (cs as BitPdfName)?.Value switch
    {
        "DeviceGray" or "G" or "CalGray" => 1,
        "DeviceRGB" or "RGB" or "CalRGB" => 3,
        "DeviceCMYK" or "CMYK" => 4,
        "Indexed" or "I" => 1,
        _ => -1, // named resource / array space: not resolvable here
    };

    private static int AsInt(object? v) => v is double d ? (int)d : 0;
}
