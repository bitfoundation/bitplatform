using System.Buffers.Binary;
using System.Text;

namespace Bit.Bmage;

/// <summary>Reads and writes EXIF/ICC/XMP chunks in WebP (RIFF) containers.</summary>
public static class BmageWebPContainer
{
    public readonly record struct Chunk(string FourCc, int Offset, int DataOffset, int DataLength)
    {
        public int PaddedLength => DataLength + (DataLength & 1);
        public int TotalLength => 8 + PaddedLength;
    }

    public static List<Chunk> EnumerateChunks(ReadOnlySpan<byte> webp)
    {
        var list = new List<Chunk>();
        if (webp.Length < 12 || !webp.StartsWith("RIFF"u8) || !webp.Slice(8, 4).SequenceEqual("WEBP"u8)) return list;
        var pos = 12;
        while (pos + 8 <= webp.Length)
        {
            var fourcc = Encoding.ASCII.GetString(webp.Slice(pos, 4));
            var len = BinaryPrimitives.ReadUInt32LittleEndian(webp[(pos + 4)..]);
            if ((long)pos + 8 + len > webp.Length) break; // in long: a crafted length near int.MaxValue would wrap
            list.Add(new Chunk(fourcc, pos, pos + 8, (int)len));
            pos += 8 + (int)len + ((int)len & 1);
        }
        return list;
    }

    public static BmageImageMetadata ReadMetadata(ReadOnlySpan<byte> webp, string? fileName = null)
    {
        BmageExifData? exif = null;
        ReadOnlyMemory<byte> icc = default, xmp = default;
        foreach (var c in EnumerateChunks(webp))
        {
            var data = webp.Slice(c.DataOffset, c.DataLength);
            switch (c.FourCc)
            {
                case "EXIF": exif ??= BmageExifReader.Read(data); break;
                case "ICCP": if (icc.IsEmpty) icc = data.ToArray(); break;
                case "XMP ": if (xmp.IsEmpty) xmp = data.ToArray(); break;
            }
        }
        return new BmageImageMetadata(exif, icc, xmp, BmageImageFormat.WebP, fileName);
    }

    /// <summary>Reads the canvas size from a WebP file (VP8X, VP8 or VP8L header).</summary>
    public static (int Width, int Height)? ReadSize(ReadOnlySpan<byte> webp)
    {
        foreach (var c in EnumerateChunks(webp))
        {
            var d = webp.Slice(c.DataOffset, c.DataLength);
            switch (c.FourCc)
            {
                case "VP8X" when d.Length >= 10:
                    return (1 + (d[4] | d[5] << 8 | d[6] << 16), 1 + (d[7] | d[8] << 8 | d[9] << 16));
                case "VP8 " when d.Length >= 10 && d[3] == 0x9D && d[4] == 0x01 && d[5] == 0x2A:
                    return ((d[6] | d[7] << 8) & 0x3FFF, (d[8] | d[9] << 8) & 0x3FFF);
                case "VP8L" when d.Length >= 5 && d[0] == 0x2F:
                {
                    var bits = (uint)(d[1] | d[2] << 8 | d[3] << 16 | d[4] << 24);
                    return ((int)(bits & 0x3FFF) + 1, (int)((bits >> 14) & 0x3FFF) + 1);
                }
            }
        }
        return null;
    }

    /// <summary>Returns a copy of the WebP with metadata chunks replaced (an extended VP8X header is added when needed).</summary>
    public static byte[] WriteMetadata(ReadOnlySpan<byte> webp, BmageImageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var chunks = EnumerateChunks(webp);
        if (chunks.Count == 0) throw new BmageImageEncodeException("Not a WebP stream.");
        var size = ReadSize(webp) ?? throw new BmageImageEncodeException("Cannot determine WebP dimensions.");
        var hasIcc = !metadata.IccProfile.IsEmpty;
        var hasExif = metadata.Exif is { IsEmpty: false };
        var hasXmp = !metadata.Xmp.IsEmpty;

        var body = new MemoryStream();
        var existingVp8x = chunks.FirstOrDefault(c => c.FourCc == "VP8X");
        byte flags = existingVp8x.FourCc == "VP8X" ? webp[existingVp8x.DataOffset] : (byte)0;
        // Clear metadata flags and set according to what we write. Bits: ICC=0x20, alpha=0x10, EXIF=0x08, XMP=0x04, anim=0x02
        flags &= 0x10 | 0x02;
        if (!hasIcc && !hasExif && !hasXmp && existingVp8x.FourCc != "VP8X")
        {
            // Nothing to add: strip any stray metadata chunks (none exist without VP8X) and return a copy.
            return webp.ToArray();
        }
        if (hasIcc) flags |= 0x20;
        if (hasExif) flags |= 0x08;
        if (hasXmp) flags |= 0x04;
        // Alpha flag: detect from an ALPH chunk or VP8L header alpha bit.
        foreach (var c in chunks)
        {
            if (c.FourCc == "ALPH") flags |= 0x10;
            if (c.FourCc == "VP8L" && c.DataLength >= 5 && ((webp[c.DataOffset + 4] >> 4) & 1) == 1) flags |= 0x10;
        }
        var vp8x = new byte[10];
        vp8x[0] = flags;
        var w = size.Width - 1; var h = size.Height - 1;
        vp8x[4] = (byte)w; vp8x[5] = (byte)(w >> 8); vp8x[6] = (byte)(w >> 16);
        vp8x[7] = (byte)h; vp8x[8] = (byte)(h >> 8); vp8x[9] = (byte)(h >> 16);
        WriteChunk(body, "VP8X", vp8x);
        if (hasIcc) WriteChunk(body, "ICCP", metadata.IccProfile.Span);
        foreach (var c in chunks)
        {
            if (c.FourCc is "VP8X" or "ICCP" or "EXIF" or "XMP ") continue;
            WriteChunk(body, c.FourCc, webp.Slice(c.DataOffset, c.DataLength));
        }
        if (hasExif) WriteChunk(body, "EXIF", metadata.Exif!.ToTiff());
        if (hasXmp) WriteChunk(body, "XMP ", metadata.Xmp.Span);

        var result = new byte[12 + body.Length];
        "RIFF"u8.CopyTo(result);
        BinaryPrimitives.WriteUInt32LittleEndian(result.AsSpan(4), (uint)(4 + body.Length));
        "WEBP"u8.CopyTo(result.AsSpan(8));
        body.Position = 0;
        body.Read(result, 12, (int)body.Length);
        return result;
    }

    private static void WriteChunk(Stream s, string fourcc, ReadOnlySpan<byte> data)
    {
        Span<byte> header = stackalloc byte[8];
        for (var i = 0; i < 4; i++) header[i] = (byte)fourcc[i];
        BinaryPrimitives.WriteUInt32LittleEndian(header[4..], (uint)data.Length);
        s.Write(header);
        s.Write(data);
        if ((data.Length & 1) == 1) s.WriteByte(0);
    }
}
