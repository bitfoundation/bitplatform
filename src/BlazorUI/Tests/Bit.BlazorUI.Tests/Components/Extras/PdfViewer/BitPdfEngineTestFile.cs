using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Bit.BlazorUI.Tests.Components.Extras.PdfViewer;

/// <summary>
/// Writes a PDF byte by byte for the engine tests: indirect objects (binary stream data
/// included), classic cross-reference tables with any subsections, cross-reference streams
/// and incremental updates appended to an earlier file. Every offset it records is the
/// real one, so a test breaks a file only where it means to.
/// </summary>
internal sealed class BitPdfEngineTestFile
{
    private readonly MemoryStream _output = new();

    /// <summary>The byte offset of every object written so far, newest definition last.</summary>
    public Dictionary<int, int> Offsets { get; } = [];

    /// <summary>The offset of the last cross-reference section written (table or stream).</summary>
    public int LastXRefOffset { get; private set; } = -1;

    public BitPdfEngineTestFile(string header = "%PDF-1.7\n")
    {
        Raw(header);
    }

    /// <summary>Continues an existing file, for an incremental update.</summary>
    public BitPdfEngineTestFile(byte[] existing, int lastXRefOffset)
    {
        _output.Write(existing);
        LastXRefOffset = lastXRefOffset;
    }

    public int Position => (int)_output.Position;

    public BitPdfEngineTestFile Raw(string text)
    {
        _output.Write(Encoding.Latin1.GetBytes(text));
        return this;
    }

    public BitPdfEngineTestFile Raw(byte[] bytes)
    {
        _output.Write(bytes);
        return this;
    }

    /// <summary>Writes <c>num gen obj body endobj</c>.</summary>
    public BitPdfEngineTestFile Obj(int num, string body, int gen = 0)
    {
        Offsets[num] = Position;
        return Raw($"{num} {gen} obj\n{body}\nendobj\n");
    }

    /// <summary>Writes a stream object with a correct <c>/Length</c> and the given extra dictionary entries.</summary>
    public BitPdfEngineTestFile Stream(int num, byte[] data, string dictEntries = "")
    {
        Offsets[num] = Position;
        Raw($"{num} 0 obj\n<< /Length {data.Length}{(dictEntries.Length > 0 ? " " + dictEntries : "")} >>\nstream\n");
        Raw(data);
        return Raw("\nendstream\nendobj\n");
    }

    public BitPdfEngineTestFile Stream(int num, string latin1Data, string dictEntries = "")
        => Stream(num, Encoding.Latin1.GetBytes(latin1Data), dictEntries);

    /// <summary>
    /// Writes a classic cross-reference table holding <paramref name="objectNumbers"/>
    /// (grouped into as many subsections as they need), its trailer and the
    /// <c>startxref</c> footer. <paramref name="freeEntries"/> are written as free
    /// (<c>f</c>) entries; object 0 is always the free-list head when listed.
    /// </summary>
    public int XRefTable(string trailerEntries, IEnumerable<int> objectNumbers, IEnumerable<int>? freeEntries = null, bool withPrev = true)
    {
        var free = new HashSet<int>(freeEntries ?? []);
        var numbers = objectNumbers.Concat(free).Distinct().OrderBy(n => n).ToList();
        int xref = Position;
        Raw("xref\n");
        int i = 0;
        while (i < numbers.Count)
        {
            int j = i;
            while (j + 1 < numbers.Count && numbers[j + 1] == numbers[j] + 1)
            {
                j++;
            }
            Raw($"{numbers[i]} {j - i + 1}\n");
            for (int k = i; k <= j; k++)
            {
                int num = numbers[k];
                // Each entry is exactly 20 bytes: 10-digit field, space, 5-digit field, space, type, space, LF.
                Raw(num == 0 ? "0000000000 65535 f \n"
                    : free.Contains(num) ? "0000000000 00001 f \n"
                    : $"{Offsets[num]:D10} 00000 n \n");
            }
            i = j + 1;
        }
        string prev = withPrev && LastXRefOffset >= 0 ? $" /Prev {LastXRefOffset}" : "";
        Raw($"trailer\n<< {trailerEntries}{prev} >>\nstartxref\n{xref}\n%%EOF\n");
        LastXRefOffset = xref;
        return xref;
    }

    /// <summary>
    /// Writes a cross-reference stream as object <paramref name="num"/> with
    /// <c>/W [1 4 2]</c>, the given rows (type, field 2, field 3), an <c>/Index</c> covering
    /// exactly those rows, and the <c>startxref</c> footer. When <paramref name="compress"/>
    /// is set the rows are PNG-Up predicted and Flate-compressed, as Acrobat writes them.
    /// </summary>
    public int XRefStream(int num, string trailerEntries, IEnumerable<(int Num, int Type, int Field2, int Field3)> rows,
        bool compress = false, bool withPrev = true)
    {
        int xref = Position;
        Offsets[num] = xref;
        var all = rows.Append((num, 1, xref, 0)).OrderBy(r => r.Item1).ToList();

        var index = new List<int>();
        for (int i = 0; i < all.Count;)
        {
            int j = i;
            while (j + 1 < all.Count && all[j + 1].Item1 == all[j].Item1 + 1)
            {
                j++;
            }
            index.Add(all[i].Item1);
            index.Add(j - i + 1);
            i = j + 1;
        }

        const int width = 7;
        var data = new byte[all.Count * width];
        for (int r = 0; r < all.Count; r++)
        {
            var (_, type, field2, field3) = all[r];
            int at = r * width;
            data[at] = (byte)type;
            data[at + 1] = (byte)(field2 >> 24);
            data[at + 2] = (byte)(field2 >> 16);
            data[at + 3] = (byte)(field2 >> 8);
            data[at + 4] = (byte)field2;
            data[at + 5] = (byte)(field3 >> 8);
            data[at + 6] = (byte)field3;
        }

        string filter = "";
        if (compress)
        {
            data = Zlib(PngPredict(data, width, filterType: 2));
            filter = $" /Filter /FlateDecode /DecodeParms << /Predictor 12 /Columns {width} >>";
        }

        int size = Math.Max(all.Max(r => r.Item1) + 1, 1);
        string prev = withPrev && LastXRefOffset >= 0 ? $" /Prev {LastXRefOffset}" : "";
        Raw($"{num} 0 obj\n<< /Type /XRef /Size {size} /W [1 4 2] /Index [{string.Join(' ', index)}]{prev} {trailerEntries}{filter} /Length {data.Length} >>\nstream\n");
        Raw(data);
        Raw($"\nendstream\nendobj\nstartxref\n{xref}\n%%EOF\n");
        LastXRefOffset = xref;
        return xref;
    }

    public byte[] ToArray() => _output.ToArray();

    // ----- Encoders the fixtures need, written out so they do not lean on the engine -----

    /// <summary>zlib (RFC 1950) compression, the format FlateDecode reads.</summary>
    public static byte[] Zlib(byte[] data)
    {
        using var output = new MemoryStream();
        using (var z = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(data);
        }
        return output.ToArray();
    }

    /// <summary>Raw deflate (RFC 1951) with no zlib wrapper, which some producers write.</summary>
    public static byte[] RawDeflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var z = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            z.Write(data);
        }
        return output.ToArray();
    }

    /// <summary>
    /// Applies a PNG row filter to every row of <paramref name="rowBytes"/> bytes (one byte
    /// per pixel), prefixing each row with its filter type. <paramref name="filterType"/> -1
    /// cycles None, Sub, Up, Average, Paeth across the rows.
    /// </summary>
    public static byte[] PngPredict(byte[] data, int rowBytes, int filterType, int bytesPerPixel = 1)
    {
        int rows = data.Length / rowBytes;
        var output = new byte[rows * (rowBytes + 1)];
        var previous = new byte[rowBytes];
        for (int row = 0; row < rows; row++)
        {
            int type = filterType >= 0 ? filterType : row % 5;
            int inAt = row * rowBytes;
            int outAt = row * (rowBytes + 1);
            output[outAt] = (byte)type;
            for (int i = 0; i < rowBytes; i++)
            {
                int raw = data[inAt + i];
                int left = i >= bytesPerPixel ? data[inAt + i - bytesPerPixel] : 0;
                int up = previous[i];
                int upLeft = i >= bytesPerPixel ? previous[i - bytesPerPixel] : 0;
                int predicted = type switch
                {
                    1 => left,
                    2 => up,
                    3 => (left + up) >> 1,
                    4 => Paeth(left, up, upLeft),
                    _ => 0,
                };
                output[outAt + 1 + i] = (byte)(raw - predicted);
            }
            Array.Copy(data, inAt, previous, 0, rowBytes);
        }
        return output;
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    /// <summary>The TIFF predictor 2 (horizontal differencing) for 8-bit samples.</summary>
    public static byte[] TiffPredict(byte[] data, int columns, int colors)
    {
        int rowBytes = columns * colors;
        var output = (byte[])data.Clone();
        for (int row = 0; row < data.Length / rowBytes; row++)
        {
            for (int i = rowBytes - 1; i >= colors; i--)
            {
                int at = (row * rowBytes) + i;
                output[at] = (byte)(data[at] - data[at - colors]);
            }
        }
        return output;
    }

    /// <summary>ASCIIHex encoding with the <c>&gt;</c> end-of-data marker.</summary>
    public static string AsciiHex(byte[] data) => Convert.ToHexString(data) + ">";

    /// <summary>ASCII85 encoding (with the <c>z</c> shorthand) and the <c>~&gt;</c> end-of-data marker.</summary>
    public static string Ascii85(byte[] data)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i += 4)
        {
            int n = Math.Min(4, data.Length - i);
            uint value = 0;
            for (int k = 0; k < 4; k++)
            {
                value = (value << 8) | (k < n ? data[i + k] : 0u);
            }
            if (n == 4 && value == 0)
            {
                sb.Append('z');
                continue;
            }
            var group = new char[5];
            for (int k = 4; k >= 0; k--)
            {
                group[k] = (char)('!' + (value % 85));
                value /= 85;
            }
            sb.Append(group, 0, n + 1);
        }
        return sb.Append("~>").ToString();
    }

    /// <summary>
    /// LZW encoding as PDF's LZWDecode reads it: 9-12 bit codes, MSB first, a Clear code
    /// first and whenever the table fills, the EOD code last, and the code width growing
    /// one code early when <paramref name="earlyChange"/> is 1.
    /// </summary>
    public static byte[] Lzw(byte[] data, int earlyChange = 1)
    {
        const int clear = 256, eod = 257;
        var output = new List<byte>();
        int bitBuffer = 0, bitCount = 0, width = 9;

        void Emit(int code)
        {
            bitBuffer = (bitBuffer << width) | code;
            bitCount += width;
            while (bitCount >= 8)
            {
                output.Add((byte)(bitBuffer >> (bitCount - 8)));
                bitCount -= 8;
            }
            bitBuffer &= (1 << bitCount) - 1;
        }

        var table = new Dictionary<string, int>();
        int next = 258;
        void Reset()
        {
            table.Clear();
            for (int i = 0; i < 256; i++)
            {
                table[((char)i).ToString()] = i;
            }
            next = 258;
        }

        Reset();
        Emit(clear);
        string current = "";
        foreach (byte b in data)
        {
            string candidate = current + (char)b;
            if (table.ContainsKey(candidate))
            {
                current = candidate;
                continue;
            }
            Emit(table[current]);
            table[candidate] = next++;
            // The decoder's table trails this one by an entry, so it widens when
            // (next - 1) reaches the limit; widen in step with it.
            if (next - 1 >= (1 << width) - earlyChange && width < 12)
            {
                width++;
            }
            if (next >= 4000)
            {
                Emit(clear);
                Reset();
                width = 9;
            }
            current = ((char)b).ToString();
        }
        if (current.Length > 0)
        {
            Emit(table[current]);
            next++;
            if (next - 1 >= (1 << width) - earlyChange && width < 12)
            {
                width++;
            }
        }
        Emit(eod);
        if (bitCount > 0)
        {
            output.Add((byte)(bitBuffer << (8 - bitCount)));
        }
        return [.. output];
    }

    /// <summary>A one-page document whose page content is <paramref name="content"/> (Helvetica as /F1).</summary>
    public static byte[] SinglePage(byte[] content, string contentDictEntries = "", string infoTitle = "")
    {
        var file = new BitPdfEngineTestFile();
        file.Obj(1, "<< /Type /Catalog /Pages 2 0 R >>");
        file.Obj(2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        file.Obj(3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>");
        file.Stream(4, content, contentDictEntries);
        file.Obj(5, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        var objects = new List<int> { 0, 1, 2, 3, 4, 5 };
        string info = "";
        if (infoTitle.Length > 0)
        {
            file.Obj(6, $"<< /Title {infoTitle} >>");
            objects.Add(6);
            info = " /Info 6 0 R";
        }
        file.XRefTable($"/Size {objects.Count} /Root 1 0 R{info}", objects);
        return file.ToArray();
    }

    /// <summary>A text-showing content stream: <c>BT /F1 12 Tf 10 100 Td (text) Tj ET</c>.</summary>
    public static byte[] ShowText(string text) => Encoding.Latin1.GetBytes($"BT /F1 12 Tf 10 100 Td ({text}) Tj ET");
}
