using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Wrapper over <see href="https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API">IndexedDB</see>.
/// </summary>
/// <remarks>
/// Each <see cref="Open"/> call returns an <see cref="IndexedDbHandle"/> that owns the JS
/// <c>IDBDatabase</c> reference - dispose it when you're done so the connection closes.
/// <br/>
/// Two shapes of the native API can't cross the interop boundary and are surfaced differently here.
/// An IDB transaction goes inactive the moment control returns to the event loop, and every call
/// into JS does exactly that, so a transaction cannot be held open across .NET calls: cursors are
/// exposed as paged reads (<see cref="IndexedDbHandle.GetPage{T}"/>) and multi-step transactions as
/// a submitted batch (<see cref="IndexedDbHandle.Transact"/>). Both run their whole walk or batch
/// inside a single real transaction on the JS side.
/// </remarks>
public class IndexedDb(IJSRuntime js)
{
    /// <summary>True when the runtime exposes <c>indexedDB</c>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<bool> IsSupported() => js.Invoke<bool>("BitButil.indexedDb.isSupported");

    /// <summary>
    /// Opens (and upgrades if needed) the named database, applying <paramref name="stores"/> inside
    /// the version-change transaction.
    /// </summary>
    /// <param name="name">Database name.</param>
    /// <param name="version">
    /// Version to open at. Pass <c>null</c> to open whatever version is already on disk, which is the
    /// safe choice when another part of the app owns the schema - opening with a number lower than the
    /// stored version fails with a <c>VersionError</c>.
    /// </param>
    /// <param name="stores">
    /// Schema to apply. Only used when an upgrade actually runs, i.e. when the database is new or
    /// <paramref name="version"/> is higher than the stored version. See <see cref="IndexedDbStoreSchema"/>
    /// for how existing stores and indexes are reconciled.
    /// </param>
    /// <param name="onVersionChange">
    /// Called when another tab wants to upgrade the database. This connection is closed first (otherwise
    /// it would block that upgrade), so the handle is unusable by the time the callback runs - re-open
    /// from here if you still need it.
    /// </param>
    /// <param name="onClose">Called when the connection dies unexpectedly - storage evicted, or the database deleted.</param>
    /// <param name="onBlocked">
    /// Called when the open is waiting on another tab's still-open connection. Supplying this changes
    /// the behavior of a blocked open: it waits (and eventually completes once the other connection
    /// closes) instead of failing. Without it, a blocked open throws rather than hanging.
    /// </param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbStoreSchema))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbIndexSchema))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbOpenInfo))]
    [DynamicDependency(nameof(IndexedDbHandle.InvokeIndexedDbVersionChange), typeof(IndexedDbHandle))]
    [DynamicDependency(nameof(IndexedDbHandle.InvokeIndexedDbClose), typeof(IndexedDbHandle))]
    [DynamicDependency(nameof(IndexedDbHandle.InvokeIndexedDbBlocked), typeof(IndexedDbHandle))]
    public async ValueTask<IndexedDbHandle> Open(string name,
        int? version = 1,
        IndexedDbStoreSchema[]? stores = null,
        Action? onVersionChange = null,
        Action? onClose = null,
        Action? onBlocked = null)
    {
        var id = Guid.NewGuid();
        var handle = new IndexedDbHandle(js, id, onVersionChange, onClose, onBlocked);
        try
        {
            var info = await js.Invoke<IndexedDbOpenInfo?>("BitButil.indexedDb.open", id, name, version, stores ?? [], handle.CallbackRef);
            handle.Initialize(info);
        }
        catch
        {
            // Nothing is registered on the JS side when the open fails, but the handle may already
            // hold a DotNetObjectReference - dispose it rather than leaking it to the GC.
            await handle.DisposeAsync();
            throw;
        }
        return handle;
    }

    /// <summary>Deletes the named database. Resolves once the deletion completes.</summary>
    public ValueTask DeleteDatabase(string name) => js.InvokeVoid("BitButil.indexedDb.deleteDatabase", name);

    /// <summary>
    /// Lists the databases this origin has. See
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IDBFactory/databases">IDBFactory.databases()</see>.
    /// </summary>
    /// <remarks>
    /// Returns an empty array where the browser doesn't implement enumeration (Firefox before 126)
    /// rather than throwing - so an empty result doesn't prove there are no databases.
    /// <see cref="IndexedDbDatabaseInfo.StoreNames"/> is always empty here; use
    /// <see cref="IndexedDbHandle.GetInfo"/> on an open handle for the store list.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbDatabaseInfo))]
    public ValueTask<IndexedDbDatabaseInfo[]> Databases()
        => js.Invoke<IndexedDbDatabaseInfo[]>("BitButil.indexedDb.databases");

    /// <summary>
    /// Orders two keys the way IndexedDB itself does: negative when <paramref name="first"/> sorts
    /// before <paramref name="second"/>, zero when equal, positive otherwise. See
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/IDBFactory/cmp">IDBFactory.cmp()</see>.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<int> Compare(object first, object second)
        => js.Invoke<int>("BitButil.indexedDb.cmp", first, second);
}
