using System.Buffers.Binary;
using System.Text;

namespace Bit.Bmage;

/// <summary>Reads and writes metadata segments (EXIF, ICC, XMP) in JPEG files without touching image data.</summary>
public static class BmageJpegContainer
{
    private static ReadOnlySpan<byte> ExifHeader => "Exif\0\0"u8;
    private static ReadOnlySpan<byte> XmpHeader => "http://ns.adobe.com/xap/1.0/\0"u8;
    private static ReadOnlySpan<byte> IccHeader => "ICC_PROFILE\0"u8;

    /// <summary>Extracts EXIF, ICC and XMP from a JPEG. Missing pieces are null/empty.</summary>
    public static BmageImageMetadata ReadMetadata(ReadOnlySpan<byte> jpeg, string? fileName = null)
    {
        BmageExifData? exif = null;
        ReadOnlyMemory<byte> xmp = default;
        var icc = new SortedDictionary<int, byte[]>();
        foreach (var seg in EnumerateSegments(jpeg))
        {
            var payload = jpeg.Slice(seg.PayloadOffset, seg.PayloadLength);
            if (seg.Marker == 0xE1)
            {
                if (exif is null && payload.StartsWith(ExifHeader)) exif = BmageExifReader.Read(payload[6..]);
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
        return new BmageImageMetadata(exif, profile, xmp, BmageImageFormat.Jpeg, fileName);
    }

    /// <summary>
    /// Reads only the EXIF orientation from a JPEG, stopping at the first SOS marker. Nothing is copied out, so it costs
    /// far less than <see cref="ReadMetadata"/>, which copies the EXIF values, the ICC profile and the XMP.
    /// </summary>
    public static BmageOrientation ReadOrientation(ReadOnlySpan<byte> jpeg)
    {
        foreach (var seg in EnumerateSegments(jpeg))
        {
            if (seg.Marker == 0xDA) break;
            var payload = jpeg.Slice(seg.PayloadOffset, seg.PayloadLength);
            // The first EXIF segment is the one ReadMetadata keeps.
            if (seg.Marker == 0xE1 && payload.StartsWith(ExifHeader))
                return BmageExifReader.ReadPrimaryNumber(payload[6..], BmageExifTag.Orientation) is uint v and >= 1 and <= 8 ? (BmageOrientation)v : BmageOrientation.Normal;
        }
        return BmageOrientation.Normal;
    }

    /// <summary>
    /// Returns a copy of <paramref name="jpeg"/> with all existing APP1 (EXIF/XMP) and APP2 ICC segments removed and the
    /// given metadata inserted after the SOI (and JFIF) segment.
    /// </summary>
    public static byte[] WriteMetadata(ReadOnlySpan<byte> jpeg, BmageImageMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        if (jpeg.Length < 4 || jpeg[0] != 0xFF || jpeg[1] != 0xD8) throw new BmageImageEncodeException("Not a JPEG stream.");
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
            // FF00 is a stuffed byte, not a marker. Reading a length after it would jump over whatever segment follows,
            // the frame header included, where the decoders, this library's and the browsers', skip it.
            if (marker is 0x00 or 0xD8 or 0x01 or (>= 0xD0 and <= 0xD7)) { pos += 2; continue; }
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
