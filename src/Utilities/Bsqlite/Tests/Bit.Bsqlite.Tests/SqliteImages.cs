using System.Buffers.Binary;

namespace Bit.Bsqlite;

/// <summary>
/// SQLite file images whose headers pass the checks an import makes - the magic string, a page size SQLite
/// can have, whole pages, and as many of them as the header says - without an engine to write them.
/// </summary>
internal static class SqliteImages
{
    /// <summary>
    /// An image of <paramref name="pages"/> pages of <paramref name="pageSize"/> bytes, everything past the
    /// header filled with <paramref name="fill"/> so two images can be told apart.
    /// </summary>
    public static byte[] Valid(byte fill = 7, int pages = 1, int pageSize = 1024, bool walMode = false)
    {
        var image = new byte[pages * pageSize];
        image.AsSpan(100).Fill(fill);

        "SQLite format 3\0"u8.CopyTo(image);
        BinaryPrimitives.WriteUInt16BigEndian(image.AsSpan(16), pageSize == 65536 ? (ushort)1 : (ushort)pageSize);
        image[18] = image[19] = walMode ? (byte)2 : (byte)1;
        BinaryPrimitives.WriteUInt32BigEndian(image.AsSpan(24), 1);
        BinaryPrimitives.WriteUInt32BigEndian(image.AsSpan(28), (uint)pages);
        BinaryPrimitives.WriteUInt32BigEndian(image.AsSpan(92), 1);
        return image;
    }
}
