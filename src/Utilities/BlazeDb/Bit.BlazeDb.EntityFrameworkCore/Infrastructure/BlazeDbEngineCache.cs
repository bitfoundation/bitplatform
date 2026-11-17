using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bit.BlazeDb;

/// <summary>
/// Holds opened engines keyed by store identity so scoped contexts share one in-memory dataset,
/// the way a SQLite file outlives any one connection.
/// <para>
/// There is one cache per process, not one per EF service provider. EF builds a separate internal
/// provider for options that differ in ways unrelated to the store - a context from
/// <c>AddDbContext</c> and one built by hand, say - and a cache per provider would open a second
/// engine on the same store: a second copy of an in-memory database, or worse, a second writer and
/// flusher on one persistent log. The store's identity is the name or the storage instance, and
/// neither belongs to any one provider.
/// </para>
/// <para>
/// Each store has its own gate, held for the whole of an open: two contexts racing to be first -
/// an <c>EnsureCreatedAsync</c> overlapping a synchronous first query, say - must not both recover
/// the same storage, which would put two engines and two flushers on one log. The loser waits and
/// gets the winner's engine.
/// </para>
/// </summary>
internal sealed class BlazeDbEngineCache
{
    /// <summary>The process-wide cache every provider instance resolves.</summary>
    public static BlazeDbEngineCache Shared { get; } = new();

    private readonly Dictionary<object, Slot> _slots = [];
    private readonly object _gate = new();
    private int _version;

    /// <summary>
    /// Changes whenever an engine is released, so a context that cached one - see
    /// <see cref="BlazeDbTableCache"/> - can tell that it has to look the store up again rather
    /// than keep using an engine another context has disposed with <c>EnsureDeleted</c>.
    /// </summary>
    public int Version => Volatile.Read(ref _version);

    /// <summary>The store's engine. <c>created</c> is true when this call opened it, false when it was already open.</summary>
    public BlazeDbDatabase GetOrCreate(BlazeDbOptionsExtension extension, IModel model, out bool created)
    {
        created = false;
        if (extension.ExistingDatabase is { } existing)
        {
            EnsureTables(existing, model);
            return existing;
        }

        var key = extension.StoreKey;
        while (true)
        {
            var slot = SlotFor(key);
            if (!slot.Gate.WaitAsync(0).GetAwaiter().GetResult())
            {
                // Someone else is opening this store. Where this thread cannot block - the browser's
                // only thread, or one with a synchronization context - waiting here would wait for a
                // continuation that can only run on this thread, so the caller is told how to start
                // asynchronously instead - or, when the holder is closing the store, to await that first.
                ThrowIfCannotBlock(slot.Releasing ? ClosingMessage : OpeningMessage);
                slot.Gate.Wait();
            }
            try
            {
                if (slot.Removed)
                {
                    continue; // Released while we waited; the dictionary has a fresh slot by now.
                }
                if (slot.Database is { IsDisposed: true } disposed)
                {
                    // Disposed by the application rather than released here: it counts as closed, and
                    // the store opens again - once its final flush is done, so that two engines never
                    // work on one storage.
                    if (!disposed.Closed.IsCompleted)
                    {
                        ThrowIfCannotBlock(ClosingMessage);
                    }
                    disposed.Closed.GetAwaiter().GetResult();
                    slot.Database = null;
                }
                if (slot.Database is null)
                {
                    slot.Database = Open(slot, extension, model);
                    created = true;
                }
                else
                {
                    EnsureTables(slot.Database, model);
                }
                return slot.Database;
            }
            finally
            {
                slot.Gate.Release();
            }
        }
    }

    public async ValueTask<(BlazeDbDatabase Database, bool Created)> GetOrCreateAsync(
        BlazeDbOptionsExtension extension, IModel model, CancellationToken cancellationToken)
    {
        if (extension.ExistingDatabase is { } existing)
        {
            EnsureTables(existing, model);
            return (existing, false);
        }

        var key = extension.StoreKey;
        while (true)
        {
            var slot = SlotFor(key);
            await slot.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (slot.Removed)
                {
                    continue;
                }
                if (slot.Database is { IsDisposed: true } disposed)
                {
                    // Disposed by the application rather than released here; see GetOrCreate.
                    await disposed.Closed.WaitAsync(cancellationToken).ConfigureAwait(false);
                    slot.Database = null;
                }
                if (slot.Database is null)
                {
                    // An open a synchronous caller had to leave running is finished here rather than
                    // started a second time. The open itself is not tied to this caller's token: a
                    // cancelled wait leaves it running for the next caller to pick up, instead of
                    // abandoning it half way through recovery.
                    var open = slot.PendingOpen ??= OpenAsync(extension, model, CancellationToken.None).AsTask();
                    BlazeDbDatabase database;
                    try
                    {
                        database = await open.WaitAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch when (open.IsFaulted || open.IsCanceled)
                    {
                        slot.PendingOpen = null; // The open failed; the next caller starts afresh.
                        throw;
                    }
                    slot.PendingOpen = null;
                    slot.Database = database;
                    EnsureTables(database, model);
                    return (database, true);
                }
                EnsureTables(slot.Database, model);
                return (slot.Database, false);
            }
            finally
            {
                slot.Gate.Release();
            }
        }
    }

    /// <summary>
    /// Disposes the store's engine - its final flush included - and forgets it, so the next access
    /// opens afresh. <paramref name="afterClose"/>, when given, runs once the engine is closed and
    /// before anyone can open the store again: <c>EnsureDeleted</c> deletes the storage there.
    /// With <paramref name="keepInMemory"/>, an open engine without storage is left alone: closing it
    /// would only throw its rows away. Returns whether an engine was closed.
    /// </summary>
    public async ValueTask<bool> ReleaseAsync(object key, Func<ValueTask>? afterClose = null, bool keepInMemory = false)
    {
        while (true)
        {
            // A slot even for a store this process never opened: the delete that may follow is then
            // made under its gate too, where no open can start on the storage until it is done.
            var slot = SlotFor(key);

            // Taken so an open in flight finishes first and is then disposed, rather than escaping
            // into a slot nobody can find any more. The slot stays registered, and its gate held,
            // until the engine is closed and afterClose has run: a context that asks for the store
            // meanwhile waits here instead of opening a second engine on storage the first is still
            // flushing to, or that is about to be deleted under it.
            await slot.Gate.WaitAsync().ConfigureAwait(false);
            if (slot.Removed)
            {
                // Another release finished with this slot while we waited, and a context may already
                // have opened the store again in a fresh one. That engine is the one to close - and
                // the delete, if any, has to wait for it - so the release starts over on the slot the
                // dictionary holds now, rather than deleting storage a live engine is writing to.
                slot.Gate.Release();
                continue;
            }
            if (keepInMemory && slot.PendingOpen is null && slot.Database is { IsDisposed: false, IsPersistent: false })
            {
                slot.Gate.Release();
                return false;
            }
            return await ReleaseCoreAsync(key, slot, afterClose).ConfigureAwait(false);
        }
    }

    private async ValueTask<bool> ReleaseCoreAsync(object key, Slot slot, Func<ValueTask>? afterClose)
    {
        // Under the gate: a synchronous caller that finds it taken can tell a close from an open.
        slot.Releasing = true;
        var closed = false;
        try
        {
            // Bumped first, so contexts that cached the engine look it up again - and wait at the
            // gate - rather than keep writing to an engine that is being disposed.
            Interlocked.Increment(ref _version);
            if (slot.PendingOpen is { } pending)
            {
                // An open a synchronous caller left running is still an engine on this storage, with
                // a flusher of its own; it has to be finished and closed like any other.
                slot.PendingOpen = null;
                try
                {
                    slot.Database = await pending.ConfigureAwait(false);
                }
                catch
                {
                    // It never opened, so there is nothing to close. The failure belongs to the
                    // caller that started it, which has already been told to retry asynchronously.
                }
            }
            if (slot.Database is { } database)
            {
                slot.Database = null;
                closed = true;
                // Disposing twice does nothing, so an engine the application disposed itself is
                // waited for instead: its final flush is still going on.
                await database.DisposeAsync().ConfigureAwait(false);
                await database.Closed.ConfigureAwait(false);
            }
            if (afterClose is not null)
            {
                await afterClose().ConfigureAwait(false);
            }
            return closed;
        }
        finally
        {
            lock (_gate)
            {
                // Only this slot: one registered after it belongs to an engine opened since.
                if (_slots.TryGetValue(key, out var current) && ReferenceEquals(current, slot))
                {
                    _slots.Remove(key);
                }
                slot.Removed = true;
            }
            Interlocked.Increment(ref _version);
            slot.Gate.Release();
        }
    }

    private Slot SlotFor(object key)
    {
        lock (_gate)
        {
            if (!_slots.TryGetValue(key, out var slot))
            {
                _slots[key] = slot = new Slot();
            }
            return slot;
        }
    }

    /// <summary>
    /// Whether the current thread may wait synchronously for asynchronous work. Not in the browser,
    /// where there is one thread and a wait that blocks it fails outright ("Cannot wait on
    /// monitors") or would wait forever for I/O that resolves on that same thread; and not under a
    /// synchronization context, whose continuations need the very thread that would be blocked.
    /// </summary>
    internal static bool CanBlock => !OperatingSystem.IsBrowser() && SynchronizationContext.Current is null;

    private const string OpeningMessage =
        "This BlazeDb store is being opened asynchronously. Await " +
        "context.Database.EnsureCreatedAsync() before the first synchronous query.";

    private const string ClosingMessage =
        "This BlazeDb store is being closed - an EnsureDeletedAsync or CloseBlazeDbAsync is still flushing or " +
        "deleting it, or its engine was disposed and is finishing its final flush - and this thread cannot " +
        "wait for that. Await that call, or context.Database.EnsureCreatedAsync(), before querying the store again.";

    private static void ThrowIfCannotBlock(string message)
    {
        if (!CanBlock)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>
    /// Opens synchronously, or as far as the thread allows. Storage whose reads complete at once -
    /// in memory - opens right here. Anything else is waited for only where waiting is possible;
    /// elsewhere the open keeps running in the slot, where the next caller - the
    /// <c>EnsureCreatedAsync</c> the exception asks for - finishes it rather than starting another
    /// on the same storage, and a release closes it.
    /// </summary>
    private static BlazeDbDatabase Open(Slot slot, BlazeDbOptionsExtension extension, IModel model)
    {
        var open = slot.PendingOpen ??= OpenAsync(extension, model, CancellationToken.None).AsTask();
        if (!open.IsCompleted && !CanBlock)
        {
            throw new InvalidOperationException(
                "Opening this BlazeDb storage requires an asynchronous start. Call " +
                "await context.Database.EnsureCreatedAsync() before the first synchronous query.");
        }

        try
        {
            var database = open.GetAwaiter().GetResult();
            EnsureTables(database, model);
            return database;
        }
        finally
        {
            slot.PendingOpen = null;
        }
    }

    private static ValueTask<BlazeDbDatabase> OpenAsync(
        BlazeDbOptionsExtension extension, IModel model, CancellationToken cancellationToken)
    {
        var tables = TablesFrom(model);
        return BlazeDbDatabase.OpenAsync(extension.CreateEngineOptions(tables), cancellationToken);
    }

    internal static void EnsureTables(BlazeDbDatabase database, IModel model)
    {
        foreach (var table in TablesFrom(model))
        {
            database.EnsureTable(table);
        }
    }

    internal static List<BlazeDbTableDescriptor> TablesFrom(IModel model)
    {
        var tables = new List<BlazeDbTableDescriptor>();
        foreach (var entityType in model.GetEntityTypes())
        {
            if (entityType.IsOwned())
            {
                continue;
            }
            tables.Add(BlazeDbTableResolver.Resolve(entityType));
        }
        return tables;
    }

    private sealed class Slot
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public BlazeDbDatabase? Database;

        /// <summary>An open a synchronous caller could not wait for, left for the next caller to finish.</summary>
        public Task<BlazeDbDatabase>? PendingOpen;

        public bool Removed;

        /// <summary>Set while a release holds the gate, so a caller that cannot wait is told what it waits for.</summary>
        public volatile bool Releasing;
    }
}
