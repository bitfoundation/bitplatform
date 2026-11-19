namespace Bit.Bcrdt;

/// <summary>Settings for a <see cref="BcrdtBlazeDbDocumentStore"/>.</summary>
public sealed class BcrdtBlazeDbDocumentStoreOptions
{
    /// <summary>Whether a write completes only once the database has flushed it to its storage.</summary>
    /// <remarks>
    /// <para>
    /// On by default, so an update the store has accepted survives a crash, as it does in every store that
    /// ships with bit Bcrdt. A BlazeDb commit lands in memory at once and reaches storage on the database's
    /// background flusher, within its <c>FlushInterval</c>; with this on, the store waits for it.
    /// </para>
    /// <para>
    /// Turn it off to make an append cost one in-memory commit and leave durability to the flusher: a crash
    /// then loses at most what was committed in the last flush interval, which a peer or server still holds
    /// and resends. A database without storage has nothing to flush, and the setting changes nothing there.
    /// </para>
    /// </remarks>
    public bool WaitForDurability { get; init; } = true;

    /// <summary>The lock the store takes around each of its operations on the database.</summary>
    /// <remarks>
    /// <para>
    /// BlazeDb assumes a single logical writer and no concurrent readers - which Blazor WebAssembly, with its
    /// one thread, gives by construction - and a multi-threaded host has to serialize every access itself.
    /// The store's writes run on a background loop, so on such a host (Blazor Server, MAUI, a desktop app)
    /// pass the lock the application already takes around its own reads and writes of the database here,
    /// and the store takes it too.
    /// </para>
    /// <para>
    /// Left unset, the store uses a lock of its own, shared by every store on the same database: the stores
    /// are serialized with each other, but not with the application.
    /// </para>
    /// </remarks>
    public SemaphoreSlim? Gate { get; init; }
}
