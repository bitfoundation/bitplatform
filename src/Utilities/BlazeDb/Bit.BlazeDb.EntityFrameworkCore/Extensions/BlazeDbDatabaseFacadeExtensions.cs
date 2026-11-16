using Bit.BlazeDb.EntityFrameworkCore.Infrastructure;
using Bit.BlazeDb.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.EntityFrameworkCore;

/// <summary>Provider-specific members on <see cref="DatabaseFacade"/>, analogous to <c>GetSqliteConnection</c>.</summary>
public static class BlazeDbDatabaseFacadeExtensions
{
    /// <summary>
    /// The engine instance this context is running against. Needed only for BlazeDb-specific
    /// operations such as <see cref="Bit.BlazeDb.BlazeDbDatabase.FlushAsync"/>; ordinary CRUD goes through
    /// the context.
    /// </summary>
    public static Bit.BlazeDb.BlazeDbDatabase GetBlazeDb(this DatabaseFacade database)
    {
        ArgumentNullException.ThrowIfNull(database);
        return database.GetService<IBlazeDbTableCache>().Database;
    }

    /// <summary>
    /// Closes the engine this context's store is open on - flushing everything committed - without
    /// deleting anything. The engine is shared by every context on the store, so they all see it closed;
    /// the next access opens the store again. Call it when the application stops
    /// (<c>IHostApplicationLifetime.ApplicationStopping</c>, a MAUI window's <c>Destroying</c>): otherwise
    /// the provider keeps an engine open for the life of the process, and what was committed within the
    /// last <c>FlushInterval</c> is not flushed when the process ends.
    /// <para>
    /// The storage itself is not closed: a storage passed to <c>UseBlazeDb</c> belongs to the application,
    /// and the lock it holds on its files - an OPFS tab's writer lock, <c>BlazeDbFileStorage</c>'s
    /// <c>writer.lock</c> - is released when the application disposes it, after this call.
    /// </para>
    /// <para>
    /// An in-memory store has nothing to flush, and closing it would throw its rows away, so it is left
    /// open and the call returns false; <c>EnsureDeletedAsync</c> is what drops one. Returns false as well
    /// when no engine was open, and for a store passed to <c>UseBlazeDb</c> as an open
    /// <see cref="Bit.BlazeDb.BlazeDbDatabase"/>, which is the application's to dispose.
    /// </para>
    /// </summary>
    public static async Task<bool> CloseBlazeDbAsync(this DatabaseFacade database)
    {
        ArgumentNullException.ThrowIfNull(database);
        var extension = database.GetService<IDbContextOptions>().FindExtension<BlazeDbOptionsExtension>()
            ?? throw new InvalidOperationException("No BlazeDb store was configured. Call optionsBuilder.UseBlazeDb().");
        if (!extension.OwnsEngine)
        {
            return false;
        }
        var closed = await database.GetService<BlazeDbEngineCache>().ReleaseAsync(extension.StoreKey, keepInMemory: true).ConfigureAwait(false);
        database.GetService<IBlazeDbTableCache>().Reset();
        return closed;
    }
}
