using System.Buffers;

namespace Bit.Bsqlite.Interop;

/// <summary>What a database name may hold where the browser keeps it.</summary>
internal static class BsqliteDatabaseNames
{
    // Every browser VFS, and the admin modules next to them, turn a name into a storage key the way a URL path
    // is read (new URL(name, 'file://').pathname): '#' and '?' end the path, so 'a#1.db' and its journal
    // 'a#1.db-journal' would both be the file 'a' - the journal written over the database - and 'a#2.db' would
    // be that same file too; '%' starts an escape, so a name could stand for another one.
    private static readonly SearchValues<char> Reserved = SearchValues.Create("#?%");

    /// <summary>Throws when a browser backend cannot keep a database under <paramref name="databaseName"/>.</summary>
    public static void ThrowIfInvalidInBrowser(string databaseName, string? paramName = null)
    {
        var at = databaseName.AsSpan().IndexOfAny(Reserved);
        if (at >= 0)
        {
            throw new ArgumentException(
                $"The database name '{databaseName}' contains '{databaseName[at]}', which a browser backend cannot keep: "
                + "'#', '?' and '%' have a meaning of their own in the storage paths the browser VFSes use, so two "
                + "databases, or a database and its journal, would end up in one file. Choose a name without them.",
                paramName);
        }
    }
}
