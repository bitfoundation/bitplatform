using Bit.BlazeDb.Storage;

namespace Bit.BlazeDb.Tests;

/// <summary>
/// Finds a database's files on a storage. A database names them <c>wal-{id}-{generation}.blz</c> and
/// <c>snapshot-{id}-{generation}.blz</c>, with an id it picks at random, so a test looks them up rather
/// than spelling them out.
/// </summary>
internal static class StorageFileNames
{
    /// <summary>The log of <paramref name="generation"/>; there has to be exactly one.</summary>
    public static string Wal(this BlazeDbInMemoryStorage storage, int generation = 1) =>
        storage.FindFile("wal", generation) ?? throw new AssertFailedException($"There is no generation-{generation} log.");

    /// <summary>The snapshot of <paramref name="generation"/>; there has to be exactly one.</summary>
    public static string Snapshot(this BlazeDbInMemoryStorage storage, int generation) =>
        storage.FindFile("snapshot", generation) ?? throw new AssertFailedException($"There is no generation-{generation} snapshot.");

    /// <summary>The <paramref name="kind"/> file ("wal" or "snapshot") of <paramref name="generation"/>, or null.</summary>
    public static string? FindFile(this BlazeDbInMemoryStorage storage, string kind, int generation) =>
        storage.FileNames.SingleOrDefault(name =>
            name.StartsWith(kind + "-", StringComparison.Ordinal) && name.EndsWith($"-{generation}.blz", StringComparison.Ordinal));
}
