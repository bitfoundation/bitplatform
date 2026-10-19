using System.Buffers.Binary;
using System.Text;
using Bit.Bmage.Codecs;

namespace Bit.Bmage.Metadata;

/// <summary>Reads and writes metadata segments (EXIF, ICC, XMP) in JPEG files without touching image data.</summary>
public static class JpegContainer
{
    private static ReadOnlySpan<byte> ExifHeader => "Exif\0\0"u8;
    private static ReadOnlySpan<byte> XmpHeader => "http://ns.adobe.com/xap/1.0/\0"u8;
    private static ReadOnlySpan<byte> IccHeader => "ICC_PROFILE\0"u8;

    /// <summary>Extracts EXIF, ICC and XMP from a JPEG. Missing pieces are null/empty.</summary>
    public static ImageMetadata ReadMetadata(ReadOnlySpan<byte> jpeg, string? fileName = null)
    {
        ExifData? exif = null;
        ReadOnlyMemory<byte> xmp = default;
        var icc = new SortedDictionary<int, byte[]>();
        foreach (var seg in EnumerateSegments(jpeg))
        {
            var payload = jpeg.Slice(seg.PayloadOffset, seg.PayloadLength);
            if (seg.Marker == 0xE1)
            {
                if (exif is null && payload.StartsWith(ExifHeader)) exif = ExifReader.Read(payload[6..]);
                else if (xmp.IsEmpty && payload.StartsWith(XmpHeader)) xmp = payload[XmpHeader.Length..].ToArray();
            }
            else if (seg.Marker == 0xE2 && payload.StartsWith(IccHeader) && payload.Length > 14)
            {
                var seq = payload[12];
                icc[seq] = payload[14..].ToArray();
            }
            else if (seg.Marker == 0xDA) break;
        }
        ReadOnlyMemory<byte> profile = icc.Count == 0 ? default : icc.Values.SelectMany(b => b).ToArray();
        return new ImageMetadata(exif, profile, xmp, ImageFormat.Jpeg, fileName);
    }

    /// <summary>Reads only the EXIF orientation from a JPEG, stopping at the first SOS marker.</summary>
    public static Geometry.Orientation ReadOrientation(ReadOnlySpan<byte> jpeg) => ReadMetadata(jpeg).Orientation;

    /// <summary>
    /// Returns a copy of <paramref name="jpeg"/> with all existing APP1 (EXIF/XMP) and APP2 ICC segments removed and the
    /// given metadata inserted after the SOI (and JFIF) segment.
    /// </summary>
    public static byte[] WriteMetadata(ReadOnlySpan<byte> jpeg, ImageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8) throw new ImageEncodeException("Not a JPEG stream.");
        var ms = new MemoryStream(jpeg.Length + 1024);
        ms.Write(jpeg[..2]);
        var insertPos = 2;
        // Keep JFIF (APP0) first when present.
        var segments = EnumerateSegments(jpeg).ToList();
        if (segments.Count > 0 && segments[0].Marker == 0xE0)
        {
            ms.Write(jpeg.Slice(segments[0].Offset, segments[0].TotalLength));
            insertPos = segments[0].Offset + segments[0].TotalLength;
        }
        if (metadata.Exif is { IsEmpty: false } exif)
        {
            var tiff = exif.ToTiff();
            WriteSegment(ms, 0xE1, ExifHeader, tiff);
        }
        if (!metadata.Xmp.IsEmpty && metadata.Xmp.Length + XmpHeader.Length + 2 <= 65535)
            WriteSegment(ms, 0xE1, XmpHeader, metadata.Xmp.Span);
        if (!metadata.IccProfile.IsEmpty)
        {
            const int chunk = 65535 - 2 - 14;
            var icc = metadata.IccProfile.Span;
            var count = (icc.Length + chunk - 1) / chunk;
            if (count <= 255)
            {
                Span<byte> header = stackalloc byte[14];
                IccHeader.CopyTo(header);
                for (var i = 0; i < count; i++)
                {
                    var part = icc.Slice(i * chunk, Math.Min(chunk, icc.Length - i * chunk));
                    header[12] = (byte)(i + 1);
                    header[13] = (byte)count;
                    WriteSegment(ms, 0xE2, header, part);
                }
            }
        }
        // Copy the rest, skipping any existing metadata segments.
        var pos = insertPos;
        foreach (var seg in segments)
        {
            if (seg.Offset < insertPos) continue;
            if (seg.Marker == 0xDA)
            {
                ms.Write(jpeg[seg.Offset..]);
                pos = jpeg.Length;
                break;
            }
            var payload = jpeg.Slice(seg.PayloadOffset, seg.PayloadLength);
            var skip = (seg.Marker == 0xE1 && (payload.StartsWith(ExifHeader) || payload.StartsWith(XmpHeader))) || (seg.Marker == 0xE2 && payload.StartsWith(IccHeader));
            if (!skip) ms.Write(jpeg.Slice(seg.Offset, seg.TotalLength));
            pos = seg.Offset + seg.TotalLength;
        }
        if (pos < jpeg.Length) ms.Write(jpeg[pos..]);
        return ms.ToArray();
    }

    private static void WriteSegment(MemoryStream ms, byte marker, ReadOnlySpan<byte> header, ReadOnlySpan<byte> payload)
    {
        var len = header.Length + payload.Length + 2;
        if (len > 65535) return; // cannot fit; silently skipped
        Span<byte> b = stackalloc byte[4];
        b[0] = 0xFF; b[1] = marker;
        BinaryPrimitives.WriteUInt16BigEndian(b[2..], (ushort)len);
        ms.Write(b);
        ms.Write(header);
        ms.Write(payload);
    }

    /// <summary>A JPEG marker segment.</summary>
    public readonly record struct Segment(byte Marker, int Offset, int PayloadOffset, int PayloadLength)
    {
        public int TotalLength => 4 + PayloadLength;
    }

    /// <summary>Enumerates marker segments up to and including SOS (which is reported with a zero payload).</summary>
    public static IEnumerable<Segment> EnumerateSegments(ReadOnlyMemory<byte> jpeg)
    {
        var list = new List<Segment>();
        foreach (var s in EnumerateSegments(jpeg.Span)) list.Add(s);
        return list;
    }

    internal static List<Segment> EnumerateSegments(ReadOnlySpan<byte> jpeg)
    {
        var list = new List<Segment>();
        if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8) return list;
        var pos = 2;
        while (pos + 4 <= jpeg.Length)
        {
            if (jpeg[pos] != 0xFF) { pos++; continue; }
            var marker = jpeg[pos + 1];
            if (marker == 0xFF) { pos++; continue; }
            if (marker is 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7)) { pos += 2; continue; }
            if (marker == 0xD9) break;
            var len = BinaryPrimitives.ReadUInt16BigEndian(jpeg[(pos + 2)..]);
            if (len < 2 || pos + 2 + len > jpeg.Length) break;
            if (marker == 0xDA)
            {
                list.Add(new Segment(marker, pos, pos + 4, len - 2));
                break;
            }
            list.Add(new Segment(marker, pos, pos + 4, len - 2));
            pos += 2 + len;
        }
        return list;
    }
}

/// <summary>Reads and writes metadata chunks (eXIf, iCCP, iTXt XMP) in PNG files.</summary>
public static class PngContainer
{
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

    public static ImageMetadata ReadMetadata(ReadOnlySpan<byte> png, string? fileName = null)
    {
        ExifData? exif = null;
        ReadOnlyMemory<byte> icc = default, xmp = default;
        foreach (var c in EnumerateChunks(png))
        {
            var data = png.Slice(c.DataOffset, c.DataLength);
            if (c.Type == MakeType("eXIf") && exif is null) exif = ExifReader.Read(data);
            else if (c.Type == MakeType("iCCP") && icc.IsEmpty)
            {
                var nul = data.IndexOf((byte)0);
                if (nul > 0 && nul + 2 < data.Length && data[nul + 1] == 0)
                {
                    try
                    {
                        using var zs = new System.IO.Compression.ZLibStream(new MemoryStream(data[(nul + 2)..].ToArray()), System.IO.Compression.CompressionMode.Decompress);
                        var ms = new MemoryStream();
                        zs.CopyTo(ms);
                        icc = ms.ToArray();
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
        return new ImageMetadata(exif, icc, xmp, ImageFormat.Png, fileName);
    }

    /// <summary>Returns a copy of the PNG with existing metadata chunks replaced by the given metadata (inserted after IHDR).</summary>
    public static byte[] WriteMetadata(ReadOnlySpan<byte> png, ImageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var chunks = EnumerateChunks(png);
        if (chunks.Count == 0 || chunks[0].Type != MakeType("IHDR")) throw new ImageEncodeException("Not a PNG stream.");
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
        var crc = Crc32.Update(Crc32.Update(0, header[4..]), data);
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }
}

/// <summary>Reads and writes EXIF/ICC/XMP chunks in WebP (RIFF) containers.</summary>
public static class WebPContainer
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

    public static ImageMetadata ReadMetadata(ReadOnlySpan<byte> webp, string? fileName = null)
    {
        ExifData? exif = null;
        ReadOnlyMemory<byte> icc = default, xmp = default;
        foreach (var c in EnumerateChunks(webp))
        {
            var data = webp.Slice(c.DataOffset, c.DataLength);
            switch (c.FourCc)
            {
                case "EXIF": exif ??= ExifReader.Read(data); break;
                case "ICCP": if (icc.IsEmpty) icc = data.ToArray(); break;
                case "XMP ": if (xmp.IsEmpty) xmp = data.ToArray(); break;
            }
        }
        return new ImageMetadata(exif, icc, xmp, ImageFormat.WebP, fileName);
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
    public static byte[] WriteMetadata(ReadOnlySpan<byte> webp, ImageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        var chunks = EnumerateChunks(webp);
        if (chunks.Count == 0) throw new ImageEncodeException("Not a WebP stream.");
        var size = ReadSize(webp) ?? throw new ImageEncodeException("Cannot determine WebP dimensions.");
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
