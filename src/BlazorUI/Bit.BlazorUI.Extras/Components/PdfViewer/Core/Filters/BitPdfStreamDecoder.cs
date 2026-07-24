// The stream filter pipeline: decode-stream factories.

using System.IO.Compression;

namespace Bit.BlazorUI;

/// <summary>
/// Applies the <c>/Filter</c> chain declared on a PDF stream dictionary to
/// produce the decoded bytes. Supports the filters required for structural
/// decoding (FlateDecode with predictors) plus a few simple ASCII filters.
/// Image-only filters (DCT/JPX/CCITT/JBIG2) are returned undecoded for now.
/// </summary>
public static class BitPdfStreamDecoder
{
    /// <summary>
    /// Returns the fully decoded bytes for <paramref name="stream"/>. The
    /// <c>/F</c> and <c>/DP</c> abbreviations for <c>/Filter</c>/<c>/DecodeParms</c>
    /// are only valid on inline images; for a regular stream <c>/F</c> is a file
    /// specification, not a filter, so it is honoured only when
    /// <paramref name="inline"/> is <c>true</c> (1.7).
    /// </summary>
    public static byte[] Decode(BitPdfStream stream, bool inline = false)
    {
        BitPdfDict dict = stream.Dict ?? throw new BitPdfFormatException("Stream has no dictionary.");

        stream.Reset();
        byte[] data = stream.GetBytes();

        object? filterObj = inline ? dict.Get("Filter", "F") : dict.Get("Filter");
        var filters = ResolveNames(filterObj, dict.XRef);
        if (filters.Count == 0)
        {
            return data;
        }

        object? parmsObj = inline ? dict.Get("DecodeParms", "DP") : dict.Get("DecodeParms");
        var parmsList = ResolveParms(parmsObj, filters.Count, dict.XRef);
        for (int i = 0; i < filters.Count; i++)
        {
            data = ApplyFilter(filters[i], data, parmsList[i]);
        }
        return data;
    }

    private static byte[] ApplyFilter(string name, byte[] data, BitPdfDict? parms)
    {
        switch (name)
        {
            case "FlateDecode":
            case "Fl":
                return ApplyPredictorIfAny(Inflate(data), parms);
            case "LZWDecode":
            case "LZW":
                return ApplyPredictorIfAny(BitPdfLzwDecode.Decode(data, EarlyChange(parms)), parms);
            case "ASCIIHexDecode":
            case "AHx":
                return AsciiHexDecode(data);
            case "ASCII85Decode":
            case "A85":
                return Ascii85Decode(data);
            case "RunLengthDecode":
            case "RL":
                return RunLengthDecode(data);
            // The Crypt filter is not a data transform: decryption (or the decision
            // to skip it for /Name /Identity) happens in the xref layer, so by the
            // time the bytes reach here they are already plaintext. (1.7)
            case "Crypt":
                return data;
            // Image compression filters are decoded later by the image pipeline.
            case "DCTDecode":
            case "DCT":
            case "JPXDecode":
            case "CCITTFaxDecode":
            case "CCF":
            case "JBIG2Decode":
                return data;
            default:
                return data;
        }
    }

    private static byte[] ApplyPredictorIfAny(byte[] data, BitPdfDict? parms)
    {
        if (parms is null)
        {
            return data;
        }
        int predictor = ToInt(parms.Get("Predictor"), 1);
        if (predictor <= 1)
        {
            return data;
        }
        int colors = ToInt(parms.Get("Colors"), 1);
        int bpc = ToInt(parms.Get("BitsPerComponent"), 8);
        int columns = ToInt(parms.Get("Columns"), 1);
        return BitPdfPredictor.Apply(data, predictor, colors, bpc, columns);
    }

    private static int EarlyChange(BitPdfDict? parms) => ToInt(parms?.Get("EarlyChange"), 1);

    private static byte[] Inflate(byte[] data)
    {
        // PDF FlateDecode is zlib-wrapped deflate (RFC 1950). Read incrementally
        // and keep whatever inflated before any error: a truncated stream, or a
        // bad trailing Adler-32 checksum, must not throw away the whole page.
        byte[] zlib = InflateIncremental(data, raw: false, skipHeader: 0);
        if (zlib.Length > 0)
        {
            return zlib;
        }
        // Non-zlib producers: raw deflate, then raw deflate past a 2-byte header.
        byte[] rawStream = InflateIncremental(data, raw: true, skipHeader: 0);
        if (rawStream.Length > 0)
        {
            return rawStream;
        }
        return InflateIncremental(data, raw: true, skipHeader: 2);
    }

    private static byte[] InflateIncremental(byte[] data, bool raw, int skipHeader)
    {
        using var output = new MemoryStream();
        try
        {
            if (skipHeader > 0 && data.Length <= skipHeader)
            {
                return [];
            }
            using var input = new MemoryStream(data, skipHeader, data.Length - skipHeader);
            using Stream decompressor = raw
                ? new DeflateStream(input, CompressionMode.Decompress)
                : new ZLibStream(input, CompressionMode.Decompress);
            var buffer = new byte[8192];
            int read;
            while ((read = decompressor.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }
        catch
        {
            // Return the bytes decoded before the failure (may be empty).
        }
        return output.ToArray();
    }

    private static byte[] AsciiHexDecode(byte[] data)
    {
        var output = new List<byte>(data.Length / 2);
        int hi = -1;
        foreach (byte b in data)
        {
            if (b == (byte)'>')
            {
                break;
            }
            int v = b switch
            {
                >= (byte)'0' and <= (byte)'9' => b - '0',
                >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
                >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
                _ => -1,
            };
            if (v < 0)
            {
                continue; // skip whitespace and other characters
            }
            if (hi < 0)
            {
                hi = v;
            }
            else
            {
                output.Add((byte)((hi << 4) | v));
                hi = -1;
            }
        }
        if (hi >= 0)
        {
            output.Add((byte)(hi << 4));
        }
        return output.ToArray();
    }

    private static byte[] Ascii85Decode(byte[] data)
    {
        var output = new List<byte>(data.Length);
        var group = new int[5];
        int count = 0;

        for (int idx = 0; idx < data.Length; idx++)
        {
            int c = data[idx];
            if (c == '~')
            {
                break;
            }
            if (c is 0x20 or 0x09 or 0x0A or 0x0C or 0x0D or 0x00)
            {
                continue;
            }
            if (c == 'z' && count == 0)
            {
                output.Add(0);
                output.Add(0);
                output.Add(0);
                output.Add(0);
                continue;
            }
            if (c < '!' || c > 'u')
            {
                continue;
            }
            group[count++] = c - '!';
            if (count == 5)
            {
                long value = 0;
                for (int i = 0; i < 5; i++)
                {
                    value = value * 85 + group[i];
                }
                output.Add((byte)(value >> 24));
                output.Add((byte)(value >> 16));
                output.Add((byte)(value >> 8));
                output.Add((byte)value);
                count = 0;
            }
        }

        if (count > 0)
        {
            for (int i = count; i < 5; i++)
            {
                group[i] = 84; // pad with 'u'
            }
            long value = 0;
            for (int i = 0; i < 5; i++)
            {
                value = value * 85 + group[i];
            }
            for (int i = 0; i < count - 1; i++)
            {
                output.Add((byte)(value >> (24 - i * 8)));
            }
        }

        return output.ToArray();
    }

    private static byte[] RunLengthDecode(byte[] data)
    {
        var output = new List<byte>(data.Length * 2);
        int i = 0;
        while (i < data.Length)
        {
            int length = data[i++];
            if (length == 128)
            {
                break; // EOD
            }
            if (length < 128)
            {
                int count = length + 1;
                for (int j = 0; j < count && i < data.Length; j++)
                {
                    output.Add(data[i++]);
                }
            }
            else
            {
                int count = 257 - length;
                if (i < data.Length)
                {
                    byte value = data[i++];
                    for (int j = 0; j < count; j++)
                    {
                        output.Add(value);
                    }
                }
            }
        }
        return output.ToArray();
    }

    // Filters and DecodeParms entries may themselves be indirect references
    // (both the whole value and individual array elements), so resolve through
    // the owning dictionary's xref before interpreting them.
    private static readonly HashSet<string> KnownFilters = new(StringComparer.Ordinal)
    {
        "FlateDecode", "Fl", "LZWDecode", "LZW", "ASCIIHexDecode", "AHx",
        "ASCII85Decode", "A85", "RunLengthDecode", "RL", "DCTDecode", "DCT",
        "JPXDecode", "CCITTFaxDecode", "CCF", "JBIG2Decode",
    };

    private static List<string> ResolveNames(object? filter, IBitPdfXRef? xref)
    {
        var result = new List<string>();
        switch (filter)
        {
            case BitPdfName n:
                AddFilterName(result, n.Value);
                break;
            case List<object?> arr:
                foreach (var item in arr)
                {
                    if ((xref?.FetchIfRef(item) ?? item) is BitPdfName name)
                    {
                        AddFilterName(result, name.Value);
                    }
                }
                break;
        }
        return result;
    }

    private static void AddFilterName(List<string> result, string name)
    {
        if (!KnownFilters.Contains(name))
        {
            // Unknown filter: surface a diagnostic rather than silently passing
            // the data through unchanged (which would render as garbage).
            System.Diagnostics.Debug.WriteLine($"BitPdf: unknown stream filter '{name}'.");
        }
        result.Add(name);
    }

    private static List<BitPdfDict?> ResolveParms(object? parms, int count, IBitPdfXRef? xref)
    {
        var result = new List<BitPdfDict?>(count);
        if (parms is List<object?> arr)
        {
            for (int i = 0; i < count; i++)
            {
                result.Add(i < arr.Count ? xref?.FetchIfRef(arr[i]) as BitPdfDict ?? arr[i] as BitPdfDict : null);
            }
        }
        else
        {
            result.Add(parms as BitPdfDict);
            for (int i = 1; i < count; i++)
            {
                result.Add(null);
            }
        }
        return result;
    }

    private static int ToInt(object? value, int fallback)
        => value is double d ? (int)d : fallback;
}
