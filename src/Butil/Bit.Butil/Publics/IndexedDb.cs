using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Lightweight wrapper over <see href="https://developer.mozilla.org/en-US/docs/Web/API/IndexedDB_API">IndexedDB</see>.
/// </summary>
/// <remarks>
/// The wrapper deliberately surfaces the most commonly needed CRUD shape rather than the
/// full IDB transaction/cursor API; complex graph queries should drop down to interop.
/// Each <see cref="Open"/> call returns an <see cref="IndexedDbHandle"/> that owns the JS
/// <c>IDBDatabase</c> reference - dispose it when you're done so the connection closes.
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
    /// Opens (and upgrades if needed) the named database. Stores listed in <paramref name="stores"/>
    /// that don't yet exist are created during the upgrade transaction; existing stores are left
    /// alone (no automatic schema migration).
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbStoreSchema))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbIndexSchema))]
    public async ValueTask<IndexedDbHandle> Open(string name, int version = 1, IndexedDbStoreSchema[]? stores = null)
    {
        var id = Guid.NewGuid();
        await js.InvokeVoid("BitButil.indexedDb.open", id, name, version, stores ?? []);
        return new IndexedDbHandle(js, id);
    }

    /// <summary>Deletes the named database. Resolves once the deletion completes.</summary>
    public ValueTask DeleteDatabase(string name) => js.InvokeVoid("BitButil.indexedDb.deleteDatabase", name);
}
