using System.Buffers.Binary;
using System.Text;

namespace Bit.Bmage;

/// <summary>Reads and writes metadata chunks (eXIf, iCCP, iTXt XMP) in PNG files.</summary>
public static class BmagePngContainer
{
    private const int MaxIccProfileBytes = 4 * 1024 * 1024;

    private static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public readonly record struct Chunk(uint Type, int Offset, int DataOffset, int DataLength)
    {
        public int TotalLength => 12 + DataLength;
        public string TypeName => Encoding.ASCII.GetString([(byte)(Type >> 24), (byte)(Type >> 16), (byte)(Type >> 8), (byte)Type]);
    }

    public static uint MakeType(string name) => (uint)(name[0] << 24 | name[1] << 16 | name[2] << 8 | name[3]);

    public static List<Chunk> EnumerateChunks(ReadOnlySpan<byte> png)
    {
        var list = new List<Chunk>();
        if (!png.StartsWith(Signature)) return list;
        var pos = 8;
        while (pos + 12 <= png.Length)
        {
            var len = BinaryPrimitives.ReadUInt32BigEndian(png[pos..]);
            if ((long)pos + 12 + len > png.Length) break; // in long: a crafted length near int.MaxValue would wrap
            var type = BinaryPrimitives.ReadUInt32BigEndian(png[(pos + 4)..]);
            list.Add(new Chunk(type, pos, pos + 8, (int)len));
            pos += 12 + (int)len;
            if (type == MakeType("IEND")) break;
        }
        return list;
    }

    public static BmageImageMetadata ReadMetadata(ReadOnlySpan<byte> png, string? fileName = null)
    {
        BmageExifData? exif = null;
        ReadOnlyMemory<byte> icc = default, xmp = default;
        // Only the first iCCP is read, whatever comes of it: the standard allows one, and an oversized or corrupt profile
        // leaves icc empty, so testing that instead would inflate every further iCCP up to the limit again.
        var iccSeen = false;
        foreach (var c in EnumerateChunks(png))
        {
            var data = png.Slice(c.DataOffset, c.DataLength);
            if (c.Type == MakeType("eXIf") && exif is null) exif = BmageExifReader.Read(data);
            else if (c.Type == MakeType("iCCP"))
            {
                if (iccSeen) continue;
                iccSeen = true;
                var nul = data.IndexOf((byte)0);
                if (nul > 0 && nul + 2 < data.Length && data[nul + 1] == 0)
                {
                    try
                    {
                        using var zs = new System.IO.Compression.ZLibStream(new MemoryStream(data[(nul + 2)..].ToArray()), System.IO.Compression.CompressionMode.Decompress);
                        var ms = new MemoryStream();
                        var block = new byte[81920];
                        int read;
                        while ((read = zs.Read(block)) > 0 && ms.Length + read <= MaxIccProfileBytes) ms.Write(block, 0, read);
                        if (read == 0) icc = ms.ToArray();
                    }
                    catch (Exception) { /* corrupt profile: ignore */ }
                }
            }
            else if (c.Type == MakeType("iTXt") && xmp.IsEmpty)
            {
                if (data.StartsWith("XML:com.adobe.xmp\0"u8))
                {
                    // keyword\0 compression flag(1) method(1) language\0 translated\0 text
                    var p = 18;
                    if (p + 2 <= data.Length)
                    {
                        var compressed = data[p] == 1;
                        p += 2;
                        var l = data[p..].IndexOf((byte)0); if (l < 0) continue; p += l + 1;
                        var t = data[p..].IndexOf((byte)0); if (t < 0) continue; p += t + 1;
                        if (!compressed) xmp = data[p..].ToArray();
                    }
                }
            }
            else if (c.Type == MakeType("IDAT")) break;
        }
        return new BmageImageMetadata(exif, icc, xmp, BmageImageFormat.Png, fileName);
    }

    /// <summary>Returns a copy of the PNG with existing metadata chunks replaced by the given metadata (inserted after IHDR).</summary>
    public static byte[] WriteMetadata(ReadOnlySpan<byte> png, BmageImageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var chunks = EnumerateChunks(png);
        if (chunks.Count == 0 || chunks[0].Type != MakeType("IHDR")) throw new BmageImageEncodeException("Not a PNG stream.");
        var ms = new MemoryStream(png.Length + 1024);
        ms.Write(Signature);
        ms.Write(png.Slice(chunks[0].Offset, chunks[0].TotalLength));
        if (!metadata.IccProfile.IsEmpty)
        {
            var body = new MemoryStream();
            body.Write("ICC Profile\0\0"u8);
            using (var zs = new System.IO.Compression.ZLibStream(body, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true))
                zs.Write(metadata.IccProfile.Span);
            WriteChunk(ms, "iCCP", body.ToArray());
        }
        if (metadata.Exif is { IsEmpty: false } exif) WriteChunk(ms, "eXIf", exif.ToTiff());
        if (!metadata.Xmp.IsEmpty)
        {
            var body = new MemoryStream();
            body.Write("XML:com.adobe.xmp\0"u8);
            body.Write([0, 0, 0, 0]);
            body.Write(metadata.Xmp.Span);
            WriteChunk(ms, "iTXt", body.ToArray());
        }
        foreach (var c in chunks.Skip(1))
        {
            var isMeta = c.Type == MakeType("eXIf") || c.Type == MakeType("iCCP")
                || (c.Type == MakeType("iTXt") && png.Slice(c.DataOffset, c.DataLength).StartsWith("XML:com.adobe.xmp\0"u8));
            if (!isMeta) ms.Write(png.Slice(c.Offset, c.TotalLength));
        }
        return ms.ToArray();
    }

    public static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> header = stackalloc byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)data.Length);
        header[4] = (byte)type[0]; header[5] = (byte)type[1]; header[6] = (byte)type[2]; header[7] = (byte)type[3];
        stream.Write(header);
        stream.Write(data);
        var crc = BmageCrc32.Update(BmageCrc32.Update(0, header[4..]), data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }
}
