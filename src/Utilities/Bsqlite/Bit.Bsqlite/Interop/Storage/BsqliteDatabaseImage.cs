using System.Buffers.Binary;
using Bit.Bsqlite.Data;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Checks a SQLite file image before it is written over a database, and prepares it for a web backend.
/// </summary>
/// <remarks>
/// <para>
/// A file that is not a database, or is cut short, is far cheaper to refuse here than after it has been
/// imported and bound: from then on every open fails until somebody deletes it. The header says enough
/// to catch both - the magic string, a page size SQLite can have, a length that is a whole number of
/// pages, and, when the header's own page count is current, at least that many pages.
/// </para>
/// <para>
/// A file that was last written in WAL mode - every database EF Core creates on a server or a desktop -
/// says so in bytes 18 and 19 of its header, and SQLite then looks for its <c>-wal</c> file through
/// shared memory no web VFS has, so the first read fails with <c>SQLITE_CANTOPEN</c>. An image is one
/// file, without its <c>-wal</c>: whatever is in it is the database as of its last checkpoint, which is
/// what SQLite would read from it too. <see cref="Prepare"/> marks such an image as a rollback-journal
/// database, as <c>PRAGMA journal_mode = DELETE</c> would after a checkpoint, so it opens anywhere.
/// </para>
/// </remarks>
internal static class BsqliteDatabaseImage
{
    private const int HeaderLength = 100;

    private static ReadOnlySpan<byte> Magic => "SQLite format 3\0"u8;

    /// <summary>
    /// Throws <see cref="BsqliteCorruptDatabaseException"/> when <paramref name="image"/> is not a whole
    /// SQLite database. An empty image is accepted: it is an empty database, which SQLite creates on open.
    /// </summary>
    public static void Verify(ReadOnlySpan<byte> image, string description = "The image")
    {
        if (image.Length == 0)
        {
            return;
        }

        if (image.Length < HeaderLength || !image[..Magic.Length].SequenceEqual(Magic))
        {
            throw new BsqliteCorruptDatabaseException(
                $"{description} is not a SQLite database: it does not start with the 'SQLite format 3' header.");
        }

        var pageSize = PageSizeOf(image);
        if (pageSize is 0)
        {
            throw new BsqliteCorruptDatabaseException(
                $"{description} is not a usable SQLite database: its header names an invalid page size.");
        }

        if (image.Length % pageSize != 0)
        {
            throw new BsqliteCorruptDatabaseException(
                $"{description} is cut short: {image.Length} bytes is not a whole number of "
                + $"{pageSize}-byte pages.");
        }

        // The page count at offset 28 is only current when the change counter it was written with (offset
        // 24) matches the one at offset 92; a file written by a SQLite older than 3.7.0 may leave it stale.
        var changeCounter = BinaryPrimitives.ReadUInt32BigEndian(image[24..]);
        var validFor = BinaryPrimitives.ReadUInt32BigEndian(image[92..]);
        var pageCount = BinaryPrimitives.ReadUInt32BigEndian(image[28..]);
        if (pageCount != 0 && changeCounter == validFor && (long)pageCount * pageSize > image.Length)
        {
            throw new BsqliteCorruptDatabaseException(
                $"{description} is cut short: its header says {pageCount} pages of {pageSize} bytes, "
                + $"but it holds {image.Length / pageSize}.");
        }
    }

    /// <summary>
    /// Verifies <paramref name="image"/> and returns it ready to import: the same bytes, or a copy marked as
    /// a rollback-journal database when it was last written in WAL mode.
    /// </summary>
    public static ReadOnlyMemory<byte> Prepare(ReadOnlyMemory<byte> image, string description = "The image")
    {
        Verify(image.Span, description);

        if (!IsWalMode(image.Span))
        {
            return image;
        }

        var copy = image.ToArray();
        copy[18] = 1;
        copy[19] = 1;
        return copy;
    }

    /// <summary>Whether the header marks the file as a WAL-mode database (file format versions 2).</summary>
    public static bool IsWalMode(ReadOnlySpan<byte> image)
        => image.Length >= HeaderLength && (image[18] == 2 || image[19] == 2);

    /// <summary>The page size the header names, or 0 when it names none SQLite can have.</summary>
    private static int PageSizeOf(ReadOnlySpan<byte> image)
    {
        int size = BinaryPrimitives.ReadUInt16BigEndian(image[16..]);
        if (size == 1)
        {
            return 65536;
        }

        return size >= 512 && (size & (size - 1)) == 0 ? size : 0;
    }
}
