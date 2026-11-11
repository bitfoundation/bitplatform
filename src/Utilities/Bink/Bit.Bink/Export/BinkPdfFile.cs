using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace Bit.Bink;

/// <summary>The objects of a PDF file being written, and the cross-reference table that ties them together.</summary>
internal sealed class BinkPdfFile(bool compress)
{
    private readonly List<byte[]?> _objects = new();

    /// <summary>Reserves an object number to be filled in later with <see cref="Set(int, string)"/>.</summary>
    public int Reserve()
    {
        _objects.Add(null);
        return _objects.Count;
    }

    public void Set(int id, string body) => _objects[id - 1] = Encoding.Latin1.GetBytes(body);

    public int Add(string body)
    {
        int id = Reserve();
        Set(id, body);
        return id;
    }

    /// <summary>Adds a stream, compressed unless the file is written uncompressed.</summary>
    /// <param name="dictionary">Entries of the stream's dictionary other than its length and filter.</param>
    /// <param name="data">The stream's data.</param>
    public int AddStream(string dictionary, ReadOnlySpan<byte> data)
    {
        if (!compress) return AddRawStream(dictionary, data);
        using var buffer = new MemoryStream(data.Length / 3 + 64);
        using (var zlib = new ZLibStream(buffer, CompressionLevel.Optimal, leaveOpen: true)) zlib.Write(data);
        return AddRawStream(dictionary + " /Filter /FlateDecode", buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
    }

    /// <summary>Adds a stream as it is, its dictionary naming any filter the data is already encoded with.</summary>
    public int AddRawStream(string dictionary, ReadOnlySpan<byte> data)
    {
        byte[] head = Encoding.Latin1.GetBytes($"<<{(dictionary.Length > 0 ? " " + dictionary : "")} /Length {data.Length} >>\nstream\n");
        byte[] tail = "\nendstream"u8.ToArray();
        byte[] body = new byte[head.Length + data.Length + tail.Length];
        head.CopyTo(body, 0);
        data.CopyTo(body.AsSpan(head.Length));
        tail.CopyTo(body, head.Length + data.Length);
        int id = Reserve();
        _objects[id - 1] = body;
        return id;
    }

    /// <summary>Writes the file: header, objects, cross-reference table and trailer.</summary>
    public byte[] ToBytes(int root, int info)
    {
        using var output = new MemoryStream();
        // The second line's high bytes tell transfer programs the file is binary.
        output.Write(Encoding.Latin1.GetBytes("%PDF-1.4\n%âãÏÓ\n"));
        var offsets = new long[_objects.Count];
        for (int i = 0; i < _objects.Count; i++)
        {
            offsets[i] = output.Position;
            output.Write(Encoding.Latin1.GetBytes($"{i + 1} 0 obj\n"));
            output.Write(_objects[i] ?? "null"u8.ToArray());
            output.Write("\nendobj\n"u8);
        }
        long xref = output.Position;
        var table = new StringBuilder();
        table.Append(CultureInfo.InvariantCulture, $"xref\n0 {_objects.Count + 1}\n0000000000 65535 f \n");
        foreach (long offset in offsets) table.Append(CultureInfo.InvariantCulture, $"{offset:D10} 00000 n \n");
        table.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {_objects.Count + 1} /Root {root} 0 R /Info {info} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        output.Write(Encoding.Latin1.GetBytes(table.ToString()));
        return output.ToArray();
    }

    /// <summary>A number as PDF writes it: invariant, at most three decimals, and zero for anything not finite.</summary>
    public static string Number(float value)
    {
        if (!float.IsFinite(value)) return "0";
        string text = MathF.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
        return text == "-0" ? "0" : text;
    }

    /// <summary>
    /// A number at full single precision, never in exponent form: for the page's scale, which every coordinate is
    /// multiplied by, so rounding it to three decimals would misplace content by up to the page's size times the error.
    /// </summary>
    public static string Precise(float value)
    {
        if (!float.IsFinite(value)) return "0";
        string text = ((double)value).ToString("0.##########", CultureInfo.InvariantCulture);
        return text == "-0" ? "0" : text;
    }

    /// <summary>Appends a number to content.</summary>
    public static StringBuilder Number(StringBuilder builder, float value) => builder.Append(Number(value));

    /// <summary>A text string for the file's properties: UTF-16 with a byte order mark, as hexadecimal, so any character survives.</summary>
    public static string TextString(string value)
    {
        var builder = new StringBuilder(value.Length * 4 + 6);
        builder.Append("<FEFF");
        foreach (char c in value) builder.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
        builder.Append('>');
        return builder.ToString();
    }
}
