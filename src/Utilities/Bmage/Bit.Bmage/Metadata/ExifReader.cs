using System.Buffers.Binary;

namespace Bit.Bmage.Metadata;

/// <summary>Parses TIFF/EXIF structures defensively: bad offsets, loops and oversized counts are ignored rather than thrown.</summary>
public static class ExifReader
{
    private const int MaxEntries = 512;
    private const int MaxValueBytes = 4 * 1024 * 1024;

    /// <summary>Reads EXIF from a payload that may start with "Exif\0\0" or directly with the TIFF header.</summary>
    public static ExifData? Read(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 6 && data[0] == 'E' && data[1] == 'x' && data[2] == 'i' && data[3] == 'f' && data[4] == 0 && data[5] == 0)
            data = data[6..];
        if (data.Length < 8) return null;
        bool bigEndian;
        if (data[0] == 'I' && data[1] == 'I' && data[2] == 0x2A && data[3] == 0) bigEndian = false;
        else if (data[0] == 'M' && data[1] == 'M' && data[2] == 0 && data[3] == 0x2A) bigEndian = true;
        else return null;

        var ifd0Offset = ReadU32(data, 4, bigEndian);
        if (ifd0Offset >= (uint)data.Length) return null;

        var visited = new HashSet<uint>();
        var primary = ReadIfd(data, ifd0Offset, bigEndian, visited, out var nextIfd);
        if (primary is null) return null;

        ExifDirectory? exif = null, gps = null, interop = null;
        if (primary.Get(ExifTag.ExifIfdPointer)?.GetDouble() is { } exifOff && exifOff < data.Length)
            exif = ReadIfd(data, (uint)exifOff, bigEndian, visited, out _);
        if (primary.Get(ExifTag.GpsIfdPointer)?.GetDouble() is { } gpsOff && gpsOff < data.Length)
            gps = ReadIfd(data, (uint)gpsOff, bigEndian, visited, out _);
        if (exif?.Get(ExifTag.InteropIfdPointer)?.GetDouble() is { } iopOff && iopOff < data.Length)
            interop = ReadIfd(data, (uint)iopOff, bigEndian, visited, out _);

        // Pointers are re-created on write; drop them from the directories.
        primary = primary.Without(ExifTag.ExifIfdPointer, ExifTag.GpsIfdPointer, ExifTag.InteropIfdPointer);
        exif = exif?.Without(ExifTag.InteropIfdPointer);

        ReadOnlyMemory<byte> thumbnail = default;
        if (nextIfd != 0 && nextIfd < data.Length)
        {
            var ifd1 = ReadIfd(data, nextIfd, bigEndian, visited, out _);
            if (ifd1?.Get(0x0201)?.GetDouble() is { } thOff && ifd1.Get(0x0202)?.GetDouble() is { } thLen && thOff >= 0 && thLen > 0 && thOff + thLen <= data.Length && thLen < MaxValueBytes)
                thumbnail = data.Slice((int)thOff, (int)thLen).ToArray();
        }

        return new ExifData(primary, exif, gps, interop, thumbnail);
    }

    private static ExifDirectory? ReadIfd(ReadOnlySpan<byte> data, uint offset, bool be, HashSet<uint> visited, out uint nextIfd)
    {
        nextIfd = 0;
        // Written without addition so an offset near uint.MaxValue cannot wrap into the valid range.
        if (offset > (uint)data.Length - 2 || !visited.Add(offset)) return null;
        int count = ReadU16(data, (int)offset, be);
        if (count > MaxEntries) count = MaxEntries;
        var dir = new Dictionary<ushort, ExifValue>();
        var pos = (int)offset + 2;
        for (var i = 0; i < count; i++, pos += 12)
        {
            if (pos + 12 > data.Length) break;
            var tag = ReadU16(data, pos, be);
            var type = (ExifType)ReadU16(data, pos + 2, be);
            var n = ReadU32(data, pos + 4, be);
            var size = ExifValue.SizeOf(type);
            if (!Enum.IsDefined(type) || n > MaxValueBytes / (uint)size) continue;
            var byteLen = (int)(n * (uint)size);
            ReadOnlySpan<byte> raw;
            if (byteLen <= 4)
            {
                raw = data.Slice(pos + 8, byteLen);
            }
            else
            {
                // Compare without adding, so a hostile offset near uint.MaxValue cannot wrap past the check.
                var valueOffset = ReadU32(data, pos + 8, be);
                if (valueOffset > (uint)data.Length || byteLen > data.Length - (int)valueOffset) continue;
                raw = data.Slice((int)valueOffset, byteLen);
            }
            dir[tag] = new ExifValue(type, (int)n, Normalise(raw, type, be));
        }
        if (pos + 4 <= data.Length) nextIfd = ReadU32(data, pos, be);
        return new ExifDirectory(dir);
    }

    /// <summary>Converts raw value bytes to little-endian component order.</summary>
    private static byte[] Normalise(ReadOnlySpan<byte> raw, ExifType type, bool bigEndian)
    {
        var result = raw.ToArray();
        if (!bigEndian) return result;
        var size = ExifValue.SizeOf(type);
        switch (type)
        {
            case ExifType.Short or ExifType.SShort:
                for (var i = 0; i + 1 < result.Length; i += 2) (result[i], result[i + 1]) = (result[i + 1], result[i]);
                break;
            // A rational is two 4-byte integers, so it byte-swaps exactly like a pair of longs. The numerator and the
            // denominator are separate values: swapping only whole 8-byte groups would leave the denominator in the
            // wrong order and silently corrupt exposure, aperture and every GPS coordinate in a big-endian file.
            case ExifType.Long or ExifType.SLong or ExifType.Float or ExifType.Rational or ExifType.SRational:
                for (var i = 0; i + 3 < result.Length; i += 4) Array.Reverse(result, i, 4);
                break;
            case ExifType.Double:
                for (var i = 0; i + 7 < result.Length; i += 8) Array.Reverse(result, i, 8);
                break;
        }
        _ = size;
        return result;
    }

    private static ushort ReadU16(ReadOnlySpan<byte> d, int o, bool be) => be ? BinaryPrimitives.ReadUInt16BigEndian(d[o..]) : BinaryPrimitives.ReadUInt16LittleEndian(d[o..]);
    private static uint ReadU32(ReadOnlySpan<byte> d, int o, bool be) => be ? BinaryPrimitives.ReadUInt32BigEndian(d[o..]) : BinaryPrimitives.ReadUInt32LittleEndian(d[o..]);
}

/// <summary>Serialises <see cref="ExifData"/> to a little-endian TIFF structure.</summary>
public static class ExifWriter
{
    public static byte[] Write(ExifData exif)
    {
        ArgumentNullException.ThrowIfNull(exif);
        var primary = new SortedDictionary<ushort, ExifValue>(exif.Primary.Tags.ToDictionary(k => k.Key, v => v.Value));
        var exifDir = new SortedDictionary<ushort, ExifValue>(exif.Exif.Tags.ToDictionary(k => k.Key, v => v.Value));
        var gpsDir = new SortedDictionary<ushort, ExifValue>(exif.Gps.Tags.ToDictionary(k => k.Key, v => v.Value));
        var interop = new SortedDictionary<ushort, ExifValue>(exif.Interop.Tags.ToDictionary(k => k.Key, v => v.Value));
        primary.Remove(ExifTag.ExifIfdPointer); primary.Remove(ExifTag.GpsIfdPointer); primary.Remove(ExifTag.InteropIfdPointer);
        exifDir.Remove(ExifTag.InteropIfdPointer);
        if (interop.Count > 0) exifDir[ExifTag.InteropIfdPointer] = ExifValue.FromLong(0);
        if (exifDir.Count > 0) primary[ExifTag.ExifIfdPointer] = ExifValue.FromLong(0);
        if (gpsDir.Count > 0) primary[ExifTag.GpsIfdPointer] = ExifValue.FromLong(0);

        // Layout: header(8) | IFD0 | IFD0 values | ExifIFD | values | GPS | values | Interop | values
        var ms = new MemoryStream();
        ms.Write([(byte)'I', (byte)'I', 0x2A, 0x00, 8, 0, 0, 0]);

        var ifd0Pos = 8;
        var exifPos = 0; var gpsPos = 0; var iopPos = 0;
        var end = ifd0Pos + IfdSize(primary);
        if (exifDir.Count > 0) { exifPos = end; end += IfdSize(exifDir); }
        if (gpsDir.Count > 0) { gpsPos = end; end += IfdSize(gpsDir); }
        if (interop.Count > 0) { iopPos = end; end += IfdSize(interop); }

        if (exifPos > 0) primary[ExifTag.ExifIfdPointer] = ExifValue.FromLong((uint)exifPos);
        if (gpsPos > 0) primary[ExifTag.GpsIfdPointer] = ExifValue.FromLong((uint)gpsPos);
        if (iopPos > 0) exifDir[ExifTag.InteropIfdPointer] = ExifValue.FromLong((uint)iopPos);

        WriteIfd(ms, primary, ifd0Pos);
        if (exifPos > 0) WriteIfd(ms, exifDir, exifPos);
        if (gpsPos > 0) WriteIfd(ms, gpsDir, gpsPos);
        if (iopPos > 0) WriteIfd(ms, interop, iopPos);
        return ms.ToArray();
    }

    private static int IfdSize(SortedDictionary<ushort, ExifValue> dir)
    {
        var size = 2 + dir.Count * 12 + 4;
        foreach (var v in dir.Values)
        {
            var len = v.Data.Length;
            if (len > 4) size += len + (len & 1);
        }
        return size;
    }

    private static void WriteIfd(MemoryStream ms, SortedDictionary<ushort, ExifValue> dir, int position)
    {
        if (ms.Position != position) throw new InvalidOperationException("EXIF layout mismatch.");
        Span<byte> buf = stackalloc byte[12];
        BinaryPrimitives.WriteUInt16LittleEndian(buf, (ushort)dir.Count);
        ms.Write(buf[..2]);
        var valuePos = position + 2 + dir.Count * 12 + 4;
        var values = new MemoryStream();
        foreach (var (tag, v) in dir)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(buf, tag);
            BinaryPrimitives.WriteUInt16LittleEndian(buf[2..], (ushort)v.Type);
            BinaryPrimitives.WriteUInt32LittleEndian(buf[4..], (uint)v.Count);
            buf[8..].Clear();
            var data = v.Data.Span;
            if (data.Length <= 4)
            {
                data.CopyTo(buf[8..]);
            }
            else
            {
                BinaryPrimitives.WriteUInt32LittleEndian(buf[8..], (uint)(valuePos + values.Length));
                values.Write(data);
                if ((data.Length & 1) == 1) values.WriteByte(0);
            }
            ms.Write(buf);
        }
        buf[..4].Clear();
        ms.Write(buf[..4]); // next IFD = 0
        values.Position = 0;
        values.CopyTo(ms);
    }
}
