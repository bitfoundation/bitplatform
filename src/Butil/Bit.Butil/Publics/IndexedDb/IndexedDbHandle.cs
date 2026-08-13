using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using static Bit.Butil.LinkerFlags;

namespace Bit.Butil;

/// <summary>
/// Live handle to an IndexedDB database. Operations are forwarded to JS, which keeps the
/// underlying <c>IDBDatabase</c> alive until <see cref="DisposeAsync"/> closes it.
/// </summary>
/// <remarks>
/// Every method taking an <c>object query</c> accepts either a plain key or an
/// <see cref="IndexedDbKeyRange"/>. Writes resolve once their transaction commits, not merely once
/// the request succeeds, so a completed call means the data is durable.
/// </remarks>
// DotNetObjectReference.Create demands every public method of this type be preserved for trimming, and
// this type's public surface includes [RequiresUnreferencedCode] JSON APIs (Put<T>, Get<T>, ...). The
// interop ref only ever dispatches the [JSInvokable] callbacks, never the JSON generics, and those keep
// their own RUC/RDC attributes so a trimming/AOT consumer is still warned at the real call site.
// Scoped to this type (not assembly-wide).
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "DotNetObjectReference.Create preserves all public methods; the RUC JSON APIs it pulls in are never invoked through this ref and stay annotated for consumers.")]
public sealed class IndexedDbHandle : IAsyncDisposable
{
    internal const string VersionChangeMethodName = nameof(InvokeIndexedDbVersionChange);
    internal const string CloseMethodName = nameof(InvokeIndexedDbClose);
    internal const string BlockedMethodName = nameof(InvokeIndexedDbBlocked);

    private readonly IJSRuntime _js;
    private readonly Guid _id;
    private readonly Action? _onVersionChange;
    private readonly Action? _onClose;
    private readonly Action? _onBlocked;
    private DotNetObjectReference<IndexedDbHandle>? _dotNetRef;
    private bool _disposed;

    internal IndexedDbHandle(IJSRuntime js, Guid id, Action? onVersionChange, Action? onClose, Action? onBlocked)
    {
        _js = js;
        _id = id;
        _onVersionChange = onVersionChange;
        _onClose = onClose;
        _onBlocked = onBlocked;

        // Only pay for an interop reference when someone is actually listening; a handle with no
        // callbacks passes null to JS and nothing is ever dispatched back.
        if (onVersionChange is not null || onClose is not null || onBlocked is not null)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
        }
    }

    internal DotNetObjectReference<IndexedDbHandle>? CallbackRef => _dotNetRef;

    internal void Initialize(IndexedDbOpenInfo? info)
    {
        if (info is null) return;   // prerender/SSR: no JS runtime ran, so there's nothing to record

        DatabaseName = info.Name;
        Version = info.Version;
        StoreNames = info.StoreNames;
        OldVersion = info.OldVersion;
        NewVersion = info.NewVersion;
        WasUpgraded = info.Upgraded;
    }

    /// <summary>Internal handle id (database is keyed by this in JS).</summary>
    public Guid Id => _id;

    /// <summary>Name of the opened database.</summary>
    public string DatabaseName { get; private set; } = string.Empty;

    /// <summary>Version the database is actually at, which may exceed the requested one.</summary>
    public int Version { get; private set; }

    /// <summary>Object stores present when the database was opened.</summary>
    public string[] StoreNames { get; private set; } = [];

    /// <summary>Version the database was at before this open upgraded it; 0 when it was newly created.</summary>
    public int OldVersion { get; private set; }

    /// <summary>Version the database was upgraded to. Equals <see cref="OldVersion"/> when no upgrade ran.</summary>
    public int NewVersion { get; private set; }

    /// <summary>
    /// True when this open ran a version-change transaction. Branch on it (together with
    /// <see cref="OldVersion"/>) to backfill or reshape data after a schema migration.
    /// </summary>
    /// <remarks>
    /// The backfill runs <em>after</em> the upgrade transaction has committed, not inside it - there's
    /// no way to hold that transaction open across an interop call. A crash between the two leaves the
    /// new schema in place with the old data, so make the backfill idempotent.
    /// </remarks>
    public bool WasUpgraded { get; private set; }

    /// <summary>Invoked from JS when another tab requests a version change. See <see cref="IndexedDb.Open"/>.</summary>
    [JSInvokable(VersionChangeMethodName)]
    public void InvokeIndexedDbVersionChange(Guid id) => _onVersionChange?.Invoke();

    /// <summary>Invoked from JS when the connection closes unexpectedly. See <see cref="IndexedDb.Open"/>.</summary>
    [JSInvokable(CloseMethodName)]
    public void InvokeIndexedDbClose(Guid id) => _onClose?.Invoke();

    /// <summary>Invoked from JS when an open is blocked by another connection. See <see cref="IndexedDb.Open"/>.</summary>
    [JSInvokable(BlockedMethodName)]
    public void InvokeIndexedDbBlocked(Guid id) => _onBlocked?.Invoke();


    // ─── Metadata ───────────────────────────────────────────────────────────────

    /// <summary>Reads the database's current name, version and store list from JS.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbDatabaseInfo))]
    public ValueTask<IndexedDbDatabaseInfo?> GetInfo()
        => _js.Invoke<IndexedDbDatabaseInfo?>("BitButil.indexedDb.info", _id);

    /// <summary>Reads a store's keypath, key generation setting and index list. Null when the store doesn't exist.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbStoreInfo))]
    public ValueTask<IndexedDbStoreInfo?> GetStoreInfo(string store)
        => _js.Invoke<IndexedDbStoreInfo?>("BitButil.indexedDb.storeInfo", _id, store);

    /// <summary>Reads an index's keypath and flags. Null when the store or index doesn't exist.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbIndexInfo))]
    public ValueTask<IndexedDbIndexInfo?> GetIndexInfo(string store, string index)
        => _js.Invoke<IndexedDbIndexInfo?>("BitButil.indexedDb.indexInfo", _id, store, index);


    // ─── Writes ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inserts or updates a value. Pass <paramref name="key"/> for stores without a keypath.
    /// Returns the record's key - the only way to read back what an autoIncrement store generated.
    /// </summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<JsonElement> Put<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, T value, object? key = null)
        => _js.Invoke<JsonElement>("BitButil.indexedDb.put", _id, store, value, key);

    /// <summary>Inserts a new value, returning its key. Throws on duplicate key.</summary>
    [RequiresUnreferencedCode("JSON serialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON serialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<JsonElement> Add<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, T value, object? key = null)
        => _js.Invoke<JsonElement>("BitButil.indexedDb.add", _id, store, value, key);

    /// <summary>
    /// Stores raw bytes, keeping them binary on the JS side instead of routing them through JSON.
    /// Read them back with <see cref="GetBytes"/>.
    /// </summary>
    /// <remarks>
    /// The stored record is an <c>ArrayBuffer</c>, so <see cref="Get{T}"/> can't deserialize it -
    /// pair every <see cref="PutBytes"/> with <see cref="GetBytes"/>. Keeping binary payloads out of
    /// JSON avoids the ~33% base64 overhead and the string-length ceiling on large blobs.
    /// </remarks>
    public ValueTask<JsonElement> PutBytes(string store, byte[] data, object? key = null)
        => _js.Invoke<JsonElement>("BitButil.indexedDb.putBytes", _id, store, data, key);

    /// <summary>
    /// Reads a record written by <see cref="PutBytes"/>. Null when the key is absent or the stored
    /// value isn't binary.
    /// </summary>
    public ValueTask<byte[]?> GetBytes(string store, object query)
        => _js.Invoke<byte[]?>("BitButil.indexedDb.getBytes", _id, store, query);

    /// <summary>Deletes the record(s) matching <paramref name="query"/> (a key or an <see cref="IndexedDbKeyRange"/>).</summary>
    public ValueTask Delete(string store, object query)
        => _js.InvokeVoid("BitButil.indexedDb.delete", _id, store, query);

    /// <summary>Empties the store.</summary>
    public ValueTask Clear(string store) => _js.InvokeVoid("BitButil.indexedDb.clear", _id, store);


    // ─── Reads ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads a value by key (or the first value in a range). Returns default when nothing matches.
    /// </summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T?> Get<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, object query)
        => _js.Invoke<T?>("BitButil.indexedDb.get", _id, store, query);

    /// <summary>Reads a value as a <see cref="JsonElement"/> (no static type required).</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<JsonElement> GetRaw(string store, object query)
        => _js.Invoke<JsonElement>("BitButil.indexedDb.get", _id, store, query);

    /// <summary>
    /// Reads the key of the first record matching <paramref name="query"/> without fetching its value.
    /// </summary>
    public ValueTask<JsonElement> GetKey(string store, object query)
        => _js.Invoke<JsonElement>("BitButil.indexedDb.getKey", _id, store, query);

    /// <summary>Reads all values in a store, optionally limited to <paramref name="count"/>.</summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T[]> GetAll<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, int? count = null)
        => _js.Invoke<T[]>("BitButil.indexedDb.getAll", _id, store, null, count);

    /// <summary>
    /// Reads every value whose key falls in <paramref name="range"/>, optionally limited to
    /// <paramref name="count"/>. Use <see cref="IndexedDbKeyRange.Only"/> for an exact-key lookup.
    /// </summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T[]> GetAll<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, IndexedDbKeyRange range, int? count = null)
        => _js.Invoke<T[]>("BitButil.indexedDb.getAll", _id, store, range, count);

    /// <summary>Lists every key in a store.</summary>
    public ValueTask<JsonElement[]> GetAllKeys(string store, int? count = null)
        => _js.Invoke<JsonElement[]>("BitButil.indexedDb.getAllKeys", _id, store, null, count);

    /// <summary>Lists the keys falling in <paramref name="range"/>.</summary>
    public ValueTask<JsonElement[]> GetAllKeys(string store, IndexedDbKeyRange range, int? count = null)
        => _js.Invoke<JsonElement[]>("BitButil.indexedDb.getAllKeys", _id, store, range, count);

    /// <summary>Counts records in a store, or just those matching <paramref name="query"/>.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<int> Count(string store, object? query = null)
        => _js.Invoke<int>("BitButil.indexedDb.count", _id, store, query);


    // ─── Indexes ────────────────────────────────────────────────────────────────

    /// <summary>Reads the first value matching <paramref name="query"/> through an index.</summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T?> GetByIndex<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, string index, object query)
        => _js.Invoke<T?>("BitButil.indexedDb.getByIndex", _id, store, index, query);

    /// <summary>
    /// Reads the <em>primary</em> key of the first record matching an index query, without its value.
    /// </summary>
    public ValueTask<JsonElement> GetKeyByIndex(string store, string index, object query)
        => _js.Invoke<JsonElement>("BitButil.indexedDb.getKeyByIndex", _id, store, index, query);

    /// <summary>Reads every value matching <paramref name="query"/> through an index.</summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<T[]> GetAllByIndex<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store, string index, object query, int? count = null)
        => _js.Invoke<T[]>("BitButil.indexedDb.getAllByIndex", _id, store, index, query, count);

    /// <summary>Lists the primary keys of every record matching <paramref name="query"/> through an index.</summary>
    public ValueTask<JsonElement[]> GetAllKeysByIndex(string store, string index, object query, int? count = null)
        => _js.Invoke<JsonElement[]>("BitButil.indexedDb.getAllKeysByIndex", _id, store, index, query, count);

    /// <summary>Counts the records matching <paramref name="query"/> through an index.</summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the read to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<int> CountByIndex(string store, string index, object? query = null)
        => _js.Invoke<int>("BitButil.indexedDb.countByIndex", _id, store, index, query);

    /// <summary>
    /// Deletes every record matching an index query and returns how many were removed. An index has
    /// no delete of its own, so this walks a cursor - all inside one transaction, so it either
    /// removes all matches or none.
    /// </summary>
    /// <remarks>
    /// During prerender/SSR (no JS runtime) this returns <c>default</c> (e.g. <c>false</c>/<c>0</c>)
    /// rather than throwing, so the result can't be distinguished from a genuine value. If you
    /// branch on it, defer the call to <c>OnAfterRenderAsync</c>.
    /// </remarks>
    public ValueTask<int> DeleteByIndex(string store, string index, object query)
        => _js.Invoke<int>("BitButil.indexedDb.deleteByIndex", _id, store, index, query);


    // ─── Cursors ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Walks a store with a cursor and returns one page of records: the values plus the keys the
    /// cursor saw. This is how you paginate, iterate in reverse, or skip - none of which
    /// <see cref="GetAll{T}(string, int?)"/> can express.
    /// </summary>
    /// <param name="store">Object store to walk.</param>
    /// <param name="query">A key or <see cref="IndexedDbKeyRange"/> to restrict the walk; null walks everything.</param>
    /// <param name="direction">Order to walk in. <see cref="IndexedDbCursorDirection.Previous"/> reads newest-first.</param>
    /// <param name="skip">Records to skip before collecting. Cheaper than fetching and discarding, but still O(skip).</param>
    /// <param name="take">Maximum records to return; 0 means no limit.</param>
    /// <remarks>
    /// The cursor is walked and closed inside a single JS task, because an IndexedDB transaction goes
    /// inactive as soon as control returns to the event loop - which every interop call does. That's
    /// why the cursor can't be stepped one record at a time from .NET.
    /// </remarks>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<IndexedDbRecord<T>[]> GetPage<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store,
        object? query = null,
        IndexedDbCursorDirection direction = IndexedDbCursorDirection.Next,
        int skip = 0,
        int take = 0)
        => _js.Invoke<IndexedDbRecord<T>[]>("BitButil.indexedDb.getPage", _id, store, query, ToName(direction), skip, take);

    /// <summary>
    /// Walks a store with a key-only cursor - same paging as <see cref="GetPage{T}"/> without
    /// deserializing the values.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbKeyRecord))]
    public ValueTask<IndexedDbKeyRecord[]> GetKeyPage(string store,
        object? query = null,
        IndexedDbCursorDirection direction = IndexedDbCursorDirection.Next,
        int skip = 0,
        int take = 0)
        => _js.Invoke<IndexedDbKeyRecord[]>("BitButil.indexedDb.getKeyPage", _id, store, query, ToName(direction), skip, take);

    /// <summary>
    /// Walks an index with a cursor, returning records in index-key order. Each record carries the
    /// index key it matched on alongside its primary key, and
    /// <see cref="IndexedDbCursorDirection.NextUnique"/> collapses duplicates to one per key.
    /// </summary>
    [RequiresUnreferencedCode("JSON deserialization may require types that cannot be statically analyzed.")]
    [RequiresDynamicCode("JSON deserialization may use reflection-based code paths that aren't AOT-safe; use a source generator for native AOT.")]
    public ValueTask<IndexedDbRecord<T>[]> GetPageByIndex<[DynamicallyAccessedMembers(JsonSerialized)] T>(string store,
        string index,
        object? query = null,
        IndexedDbCursorDirection direction = IndexedDbCursorDirection.Next,
        int skip = 0,
        int take = 0)
        => _js.Invoke<IndexedDbRecord<T>[]>("BitButil.indexedDb.getPageByIndex", _id, store, index, query, ToName(direction), skip, take);

    /// <summary>
    /// Walks an index with a key-only cursor. Pair with <see cref="IndexedDbCursorDirection.NextUnique"/>
    /// to enumerate the distinct values an index holds.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbKeyRecord))]
    public ValueTask<IndexedDbKeyRecord[]> GetKeyPageByIndex(string store,
        string index,
        object? query = null,
        IndexedDbCursorDirection direction = IndexedDbCursorDirection.Next,
        int skip = 0,
        int take = 0)
        => _js.Invoke<IndexedDbKeyRecord[]>("BitButil.indexedDb.getKeyPageByIndex", _id, store, index, query, ToName(direction), skip, take);


    // ─── Transactions ───────────────────────────────────────────────────────────

    /// <summary>
    /// Runs <paramref name="operations"/> as one atomic transaction spanning every store they touch.
    /// If any of them fails the whole batch rolls back. Returns each operation's resulting key
    /// (null for deletes and clears), in order.
    /// </summary>
    /// <param name="operations">Writes to apply, built with the <see cref="IndexedDbOperation"/> factories.</param>
    /// <param name="mode">Access level to request. Leave as readwrite unless the batch only reads.</param>
    /// <param name="durability">
    /// How hard to try flushing to disk before reporting completion. Ignored by browsers that
    /// predate the option.
    /// </param>
    /// <remarks>
    /// The batch is submitted in one call rather than issued step by step, because an IndexedDB
    /// transaction goes inactive the moment control returns to the event loop and so cannot be held
    /// open across interop calls. Passing operations together is what makes the atomicity real.
    /// <br/>
    /// During prerender/SSR (no JS runtime) this returns an empty array without writing anything.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IndexedDbOperation))]
    public ValueTask<JsonElement[]> Transact(IndexedDbOperation[] operations,
        IndexedDbTransactionMode mode = IndexedDbTransactionMode.ReadWrite,
        IndexedDbDurability durability = IndexedDbDurability.Default)
        => _js.Invoke<JsonElement[]>("BitButil.indexedDb.transact", _id, operations, ToName(mode), ToName(durability));


    // Enums cross the wire as the exact strings the DOM expects; Blazor's JSON options would other-
    // wise send the numeric value, which IndexedDB rejects.
    private static string ToName(IndexedDbCursorDirection direction) => direction switch
    {
        IndexedDbCursorDirection.NextUnique => "nextunique",
        IndexedDbCursorDirection.Previous => "prev",
        IndexedDbCursorDirection.PreviousUnique => "prevunique",
        _ => "next"
    };

    private static string ToName(IndexedDbTransactionMode mode)
        => mode == IndexedDbTransactionMode.ReadOnly ? "readonly" : "readwrite";

    // Null lets the browser apply its own default rather than us naming one for it.
    private static string? ToName(IndexedDbDurability durability) => durability switch
    {
        IndexedDbDurability.Relaxed => "relaxed",
        IndexedDbDurability.Strict => "strict",
        _ => null
    };

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { await _js.InvokeVoid("BitButil.indexedDb.close", _id); }
        catch (Exception ex) when (ex.IsIgnorableDisposalException()) { } // teardown: circuit gone, cancelled, or already disposed
        finally
        {
            _dotNetRef?.Dispose();
            _dotNetRef = null;
        }
    }
}
