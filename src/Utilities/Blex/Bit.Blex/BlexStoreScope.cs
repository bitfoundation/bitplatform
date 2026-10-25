namespace Bit.Blex;

/// <summary>
/// One instance of a store that exists more than once -- a dialog's form, an editor tab, a row's
/// detail panel -- registered with the manager under its own name for as long as the scope lives.
/// Created by <see cref="BlexManager.CreateScope{TStore}(TStore, string)"/>; in Blazor,
/// <c>&lt;BlexScope&gt;</c> creates and disposes one for you.
/// </summary>
/// <typeparam name="TStore">The store type.</typeparam>
/// <remarks>
/// <para>
/// A scoped store is a full citizen of the application state: it appears in the state tree, in
/// DevTools and in <see cref="BlexHistory"/> under <c>"{name}:{key}"</c>, its actions reach every
/// middleware and subscriber, and when it is marked <c>Persist = true</c> it is hydrated as soon as
/// it is registered and saved under its keyed name. Give it a stable <see cref="Key"/> (an order id,
/// a document id) when it persists, so the next instance for the same entity finds its state; an
/// instance whose key the manager assigned is not persisted.
/// </para>
/// <para>
/// Disposing the scope unregisters the store -- writing any debounced state it still owes storage
/// first -- and then disposes it, which cancels its in-flight effects.
/// </para>
/// </remarks>
public sealed class BlexStoreScope<TStore> : IDisposable
    where TStore : BlexStoreBase
{
    private readonly BlexManager _manager;
    private bool _disposed;

    internal BlexStoreScope(BlexManager manager, TStore store, string key)
    {
        _manager = manager;
        Store = store;
        Key = key;
    }

    /// <summary>The scoped store instance.</summary>
    public TStore Store { get; }

    /// <summary>The instance key; the store's <see cref="IBlexStore.Name"/> is <c>"{name}:{Key}"</c>.</summary>
    public string Key { get; }

    /// <summary>Whether the scope has been disposed (and the store unregistered).</summary>
    public bool IsDisposed => _disposed;

    /// <summary>Unregisters the store from the manager and disposes it. Idempotent.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Unregister first: persistence flushes the store's pending write when it leaves, and it can
        // only serialize a store that has not been torn down yet.
        _manager.Unregister(Store);
        Store.Dispose();
    }
}
