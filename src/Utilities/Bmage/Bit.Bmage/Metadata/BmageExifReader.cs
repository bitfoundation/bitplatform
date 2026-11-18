using System.Buffers.Binary;

namespace Bit.Bmage;

/// <summary>Parses TIFF/EXIF structures defensively: bad offsets, loops and oversized counts are ignored rather than thrown.</summary>
public static class BmageExifReader
{
    private const int MaxEntries = 512;
    private const int MaxValueBytes = 4 * 1024 * 1024;

    /// <summary>Reads EXIF from a payload that may start with "Exif\0\0" or directly with the TIFF header.</summary>
    public static BmageExifData? Read(ReadOnlySpan<byte> data)
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
        var budget = MaxValueBytes;
        var primary = ReadIfd(data, ifd0Offset, bigEndian, visited, ref budget, out var nextIfd);
        if (primary is null) return null;

        BmageExifDirectory? exif = null, gps = null, interop = null;
        if (primary.Get(BmageExifTag.ExifIfdPointer)?.GetDouble() is { } exifOff && exifOff < data.Length)
            exif = ReadIfd(data, (uint)exifOff, bigEndian, visited, ref budget, out _);
        if (primary.Get(BmageExifTag.GpsIfdPointer)?.GetDouble() is { } gpsOff && gpsOff < data.Length)
            gps = ReadIfd(data, (uint)gpsOff, bigEndian, visited, ref budget, out _);
        if (exif?.Get(BmageExifTag.InteropIfdPointer)?.GetDouble() is { } iopOff && iopOff < data.Length)
            interop = ReadIfd(data, (uint)iopOff, bigEndian, visited, ref budget, out _);

        // Pointers are re-created on write; drop them from the directories.
        primary = primary.Without(BmageExifTag.ExifIfdPointer, BmageExifTag.GpsIfdPointer, BmageExifTag.InteropIfdPointer);
        exif = exif?.Without(BmageExifTag.InteropIfdPointer);

        ReadOnlyMemory<byte> thumbnail = default;
        if (nextIfd != 0 && nextIfd < data.Length)
        {
            var ifd1 = ReadIfd(data, nextIfd, bigEndian, visited, ref budget, out _);
            if (ifd1?.Get(0x0201)?.GetDouble() is { } thOff && ifd1.Get(0x0202)?.GetDouble() is { } thLen && thOff >= 0 && thLen > 0 && thOff + thLen <= data.Length && thLen < MaxValueBytes && thLen <= budget)
                thumbnail = data.Slice((int)thOff, (int)thLen).ToArray();
        }

        return new BmageExifData(primary, exif, gps, interop, thumbnail);
    }

    /// <summary>
    /// Reads one whole-number tag of the first directory without copying any value out, for a caller that needs only a
    /// JPEG's orientation or a TIFF's size. Null when the tag is missing or holds something else.
    /// </summary>
    internal static uint? ReadPrimaryNumber(ReadOnlySpan<byte> data, ushort tag)
    {
        if (data.StartsWith("Exif\0\0"u8)) data = data[6..];
        if (data.Length < 8) return null;
        bool be;
        if (data[0] == 'I' && data[1] == 'I' && data[2] == 0x2A && data[3] == 0) be = false;
        else if (data[0] == 'M' && data[1] == 'M' && data[2] == 0 && data[3] == 0x2A) be = true;
        else return null;

        var offset = ReadU32(data, 4, be);
        if (offset > (uint)data.Length - 2) return null;
        var count = Math.Min((int)ReadU16(data, (int)offset, be), MaxEntries);
        uint? value = null;
        for (int i = 0, pos = (int)offset + 2; i < count && pos + 12 <= data.Length; i++, pos += 12)
        {
            if (ReadU16(data, pos, be) != tag || ReadU32(data, pos + 4, be) == 0) continue;
            // A tag listed twice keeps its last readable value, as Read does.
            value = (BmageExifType)ReadU16(data, pos + 2, be) switch
            {
                BmageExifType.Byte => data[pos + 8],
                BmageExifType.Short => ReadU16(data, pos + 8, be),
                BmageExifType.Long => ReadU32(data, pos + 8, be),
                _ => value,
            };
        }
        return value;
    }

    private static BmageExifDirectory? ReadIfd(ReadOnlySpan<byte> data, uint offset, bool be, HashSet<uint> visited, ref int budget, out uint nextIfd)
    {
        nextIfd = 0;
        // Written without addition so an offset near uint.MaxValue cannot wrap into the valid range.
        if (offset > (uint)data.Length - 2 || !visited.Add(offset)) return null;
        int count = ReadU16(data, (int)offset, be);
        if (count > MaxEntries) count = MaxEntries;
        // A tag listed twice keeps its last readable value. Only that one is copied and charged to the budget, so
        // repeating a tag cannot spend the budget the directory's other tags, the orientation among them, need.
        var entries = new Dictionary<ushort, (BmageExifType Type, uint Count, int Offset, int Length)>();
        var pos = (int)offset + 2;
        for (var i = 0; i < count; i++, pos += 12)
        {
            if (pos + 12 > data.Length) break;
            var tag = ReadU16(data, pos, be);
            var type = (BmageExifType)ReadU16(data, pos + 2, be);
            var n = ReadU32(data, pos + 4, be);
            var size = BmageExifValue.SizeOf(type);
            if (!Enum.IsDefined(type) || n > MaxValueBytes / (uint)size) continue;
            var byteLen = (int)(n * (uint)size);
            var rawOffset = pos + 8;
            if (byteLen > 4)
            {
                // Compare without adding, so a hostile offset near uint.MaxValue cannot wrap past the check.
                var valueOffset = ReadU32(data, pos + 8, be);
                if (valueOffset > (uint)data.Length || byteLen > data.Length - (int)valueOffset) continue;
                rawOffset = (int)valueOffset;
            }
            entries[tag] = (type, n, rawOffset, byteLen);
        }
        if (pos + 4 <= data.Length) nextIfd = ReadU32(data, pos, be);
        var dir = new Dictionary<ushort, BmageExifValue>(entries.Count);
        foreach (var (tag, entry) in entries)
        {
            if (entry.Length > budget) continue;
            budget -= entry.Length;
            dir[tag] = new BmageExifValue(entry.Type, (int)entry.Count, Normalise(data.Slice(entry.Offset, entry.Length), entry.Type, be));
        }
        return new BmageExifDirectory(dir);
    }

    /// <summary>Converts raw value bytes to little-endian component order.</summary>
    private static byte[] Normalise(ReadOnlySpan<byte> raw, BmageExifType type, bool bigEndian)
    {
        var result = raw.ToArray();
        if (!bigEndian) return result;
        var size = BmageExifValue.SizeOf(type);
        switch (type)
        {
            case BmageExifType.Short or BmageExifType.SShort:
                for (var i = 0; i + 1 < result.Length; i += 2) (result[i], result[i + 1]) = (result[i + 1], result[i]);
                break;
            // A rational is two 4-byte integers, so it byte-swaps exactly like a pair of longs. The numerator and the
            // denominator are separate values: swapping only whole 8-byte groups would leave the denominator in the
            // wrong order and silently corrupt exposure, aperture and every GPS coordinate in a big-endian file.
            case BmageExifType.Long or BmageExifType.SLong or BmageExifType.Float or BmageExifType.Rational or BmageExifType.SRational:
                for (var i = 0; i + 3 < result.Length; i += 4) Array.Reverse(result, i, 4);
                break;
            case BmageExifType.Double:
                for (var i = 0; i + 7 < result.Length; i += 8) Array.Reverse(result, i, 8);
                break;
        }
        _ = size;
        return result;
    }

    private static ushort ReadU16(ReadOnlySpan<byte> d, int o, bool be) => be ? BinaryPrimitives.ReadUInt16BigEndian(d[o..]) : BinaryPrimitives.ReadUInt16LittleEndian(d[o..]);
    private static uint ReadU32(ReadOnlySpan<byte> d, int o, bool be) => be ? BinaryPrimitives.ReadUInt32BigEndian(d[o..]) : BinaryPrimitives.ReadUInt32LittleEndian(d[o..]);
}
