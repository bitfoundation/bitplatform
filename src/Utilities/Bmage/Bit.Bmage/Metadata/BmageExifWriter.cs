using System.Buffers.Binary;

namespace Bit.Bmage;

/// <summary>Serialises <see cref="BmageExifData"/> to a little-endian TIFF structure.</summary>
public static class BmageExifWriter
{
    public static byte[] Write(BmageExifData exif)
    {
        ArgumentNullException.ThrowIfNull(exif);
        var primary = new SortedDictionary<ushort, BmageExifValue>(exif.Primary.Tags.ToDictionary(k => k.Key, v => v.Value));
        var exifDir = new SortedDictionary<ushort, BmageExifValue>(exif.Exif.Tags.ToDictionary(k => k.Key, v => v.Value));
        var gpsDir = new SortedDictionary<ushort, BmageExifValue>(exif.Gps.Tags.ToDictionary(k => k.Key, v => v.Value));
        var interop = new SortedDictionary<ushort, BmageExifValue>(exif.Interop.Tags.ToDictionary(k => k.Key, v => v.Value));
        primary.Remove(BmageExifTag.ExifIfdPointer); primary.Remove(BmageExifTag.GpsIfdPointer); primary.Remove(BmageExifTag.InteropIfdPointer);
        exifDir.Remove(BmageExifTag.InteropIfdPointer);
        if (interop.Count > 0) exifDir[BmageExifTag.InteropIfdPointer] = BmageExifValue.FromLong(0);
        if (exifDir.Count > 0) primary[BmageExifTag.ExifIfdPointer] = BmageExifValue.FromLong(0);
        if (gpsDir.Count > 0) primary[BmageExifTag.GpsIfdPointer] = BmageExifValue.FromLong(0);

        // Layout: header(8) | IFD0 | IFD0 values | ExifIFD | values | GPS | values | Interop | values
        var ms = new MemoryStream();
        ms.Write([(byte)'I', (byte)'I', 0x2A, 0x00, 8, 0, 0, 0]);

        var ifd0Pos = 8;
        var exifPos = 0; var gpsPos = 0; var iopPos = 0;
        var end = ifd0Pos + IfdSize(primary);
        if (exifDir.Count > 0) { exifPos = end; end += IfdSize(exifDir); }
        if (gpsDir.Count > 0) { gpsPos = end; end += IfdSize(gpsDir); }
        if (interop.Count > 0) { iopPos = end; end += IfdSize(interop); }

        if (exifPos > 0) primary[BmageExifTag.ExifIfdPointer] = BmageExifValue.FromLong((uint)exifPos);
        if (gpsPos > 0) primary[BmageExifTag.GpsIfdPointer] = BmageExifValue.FromLong((uint)gpsPos);
        if (iopPos > 0) exifDir[BmageExifTag.InteropIfdPointer] = BmageExifValue.FromLong((uint)iopPos);

        WriteIfd(ms, primary, ifd0Pos);
        if (exifPos > 0) WriteIfd(ms, exifDir, exifPos);
        if (gpsPos > 0) WriteIfd(ms, gpsDir, gpsPos);
        if (iopPos > 0) WriteIfd(ms, interop, iopPos);
        return ms.ToArray();
    }

    private static int IfdSize(SortedDictionary<ushort, BmageExifValue> dir)
    {
        var size = 2 + dir.Count * 12 + 4;
        foreach (var v in dir.Values)
        {
            var len = v.Data.Length;
            if (len > 4) size += len + (len & 1);
        }
        return size;
    }

    private static void WriteIfd(MemoryStream ms, SortedDictionary<ushort, BmageExifValue> dir, int position)
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
