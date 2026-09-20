var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _dbs: { [id: string]: { db: IDBDatabase, ref: any } } = {};

    // The connection and its schema. Reads and writes (indexedDbStore), index queries
    // (indexedDbIndex), paged cursors (indexedDbCursor), metadata (indexedDbInfo) and batched
    // transactions (indexedDbTransaction) are each their own module and reach the connection
    // through the members below - so an app that only reads a key never downloads the cursor walk
    // or the transaction batcher.
    butil.indexedDb = {
        isSupported() { return 'indexedDB' in window; },
        open,
        close,
        deleteDatabase,
        databases,
        cmp,

        // For the modules layered on this one.
        dbOf: getDb,
        txStore,
        awaitRequest,
        awaitWrite,
        toQuery
    };

    // ─── Lifecycle ──────────────────────────────────────────────────────────────

    function open(id: string, name: string, version: number | null, stores: any[], dotNetRef: any): Promise<any> {
        return new Promise((resolve, reject) => {
            // A null version means "open whatever is on disk" (IDBFactory.open with one argument).
            // Passing an explicit lower number than the stored version is a VersionError, so this is
            // the safe way to attach to a database whose schema someone else owns.
            const req = (version === null || version === undefined) ? indexedDB.open(name) : indexedDB.open(name, version);

            let oldVersion = 0;
            let newVersion = 0;
            let upgraded = false;
            // Set when we've already rejected via onblocked; a late onsuccess must then close the
            // connection instead of registering it, otherwise JS holds a handle .NET never received.
            let abandoned = false;

            req.onupgradeneeded = (e: IDBVersionChangeEvent) => {
                upgraded = true;
                oldVersion = e.oldVersion;
                newVersion = e.newVersion ?? 0;
                applySchema(req.result, req.transaction!, stores);
            };
            req.onsuccess = () => {
                const db = req.result;
                if (abandoned) {
                    try { db.close(); } catch { /* already closed */ }
                    return;
                }

                const prev = _dbs[id];
                if (prev && prev.db !== db) {
                    try { prev.db.close(); } catch { /* already closed */ }
                }

                // Close this connection if another tab requests a version change, otherwise that
                // tab's upgrade would be blocked by this open handle. .NET is told so it can
                // re-open rather than discover the closed handle on its next call.
                db.onversionchange = () => {
                    butil.utils.dispatch(dotNetRef, 'InvokeIndexedDbVersionChange', id);
                    try { db.close(); } catch { /* already closed */ }
                    if (_dbs[id]?.db === db) delete _dbs[id];
                };
                // Fires when the connection dies without close() being called - storage evicted,
                // or the underlying database was forcibly deleted.
                db.onclose = () => {
                    butil.utils.dispatch(dotNetRef, 'InvokeIndexedDbClose', id);
                    if (_dbs[id]?.db === db) delete _dbs[id];
                };

                _dbs[id] = { db, ref: dotNetRef };
                resolve({
                    name: db.name,
                    version: db.version,
                    storeNames: Array.from(db.objectStoreNames),
                    oldVersion,
                    newVersion: newVersion || db.version,
                    upgraded
                });
            };
            req.onerror = () => reject(req.error);
            req.onblocked = () => {
                // With a .NET onBlocked handler registered the caller has opted into waiting, so we
                // notify and let the open complete once the blocking connection closes. Without one
                // we reject rather than hang forever.
                if (dotNetRef) {
                    butil.utils.dispatch(dotNetRef, 'InvokeIndexedDbBlocked', id);
                    return;
                }
                abandoned = true;
                reject(new Error('IndexedDB open is blocked by another tab.'));
            };
        });
    }

    function applySchema(db: IDBDatabase, tx: IDBTransaction, stores: any[]) {
        for (const s of stores || []) {
            if (!s?.name) continue;

            if (s.drop) {
                if (db.objectStoreNames.contains(s.name)) db.deleteObjectStore(s.name);
                continue;
            }

            let store: IDBObjectStore;
            if (db.objectStoreNames.contains(s.name)) {
                store = tx.objectStore(s.name);
            } else {
                const params: IDBObjectStoreParameters = {};
                const keyPath = keyPathOf(s);
                if (keyPath !== undefined) params.keyPath = keyPath;
                if (s.autoIncrement) params.autoIncrement = true;
                store = db.createObjectStore(s.name, params);
            }

            for (const idx of s.indexes || []) {
                if (!idx?.name) continue;
                const exists = store.indexNames.contains(idx.name);

                if (idx.drop) {
                    if (exists) store.deleteIndex(idx.name);
                    continue;
                }

                const keyPath = keyPathOf(idx);
                if (keyPath === undefined) continue;

                if (exists) {
                    // IDB has no "alter index", so a changed definition means drop and re-create.
                    // Leaving it alone would silently keep the old keypath/uniqueness.
                    const current = store.index(idx.name);
                    if (samePath(current.keyPath, keyPath)
                        && current.unique === !!idx.unique
                        && current.multiEntry === !!idx.multiEntry) continue;
                    store.deleteIndex(idx.name);
                }

                store.createIndex(idx.name, keyPath, { unique: !!idx.unique, multiEntry: !!idx.multiEntry });
            }
        }
    }

    // KeyPaths (compound) wins over the single KeyPath so callers can set either.
    function keyPathOf(schema: any): string | string[] | undefined {
        if (schema.keyPaths && schema.keyPaths.length) return schema.keyPaths;
        if (schema.keyPath) return schema.keyPath;
        return undefined;
    }

    function samePath(a: string | string[], b: string | string[]) {
        const flat = (v: any) => JSON.stringify(Array.isArray(v) ? v : [v]);
        return flat(a) === flat(b);
    }

    function close(id: string) {
        const entry = _dbs[id];
        if (!entry) return;
        delete _dbs[id];
        try { entry.db.close(); } catch { /* already closed */ }
    }

    function deleteDatabase(name: string): Promise<void> {
        return new Promise((resolve, reject) => {
            const req = indexedDB.deleteDatabase(name);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
            req.onblocked = () => reject(new Error('IndexedDB delete is blocked by another tab.'));
        });
    }

    async function databases() {
        // Not implemented everywhere (Firefox only shipped it in 126); an empty list is a truer
        // answer than throwing, since "can't enumerate" and "none exist" are indistinguishable here.
        if (typeof indexedDB.databases !== 'function') return [];
        try {
            const list = await indexedDB.databases();
            return (list || []).map(d => ({ name: d.name ?? '', version: d.version ?? 0, storeNames: [] }));
        } catch {
            return [];
        }
    }

    function cmp(first: any, second: any) { return indexedDB.cmp(first, second); }

    // ─── Queries ────────────────────────────────────────────────────────────────

    // Every read/delete accepts either a plain key or a key-range descriptor built on the .NET
    // side. Object keys aren't valid in IDB, so the marker property can never collide with a
    // real key.
    function toQuery(query: any): any {
        if (query === null || query === undefined) return undefined;
        if (typeof query !== 'object' || Array.isArray(query) || query.isKeyRange !== true) return query;

        const hasLower = query.lower !== null && query.lower !== undefined;
        const hasUpper = query.upper !== null && query.upper !== undefined;

        if (query.isOnly) return IDBKeyRange.only(query.lower);
        if (hasLower && hasUpper) return IDBKeyRange.bound(query.lower, query.upper, !!query.lowerOpen, !!query.upperOpen);
        if (hasLower) return IDBKeyRange.lowerBound(query.lower, !!query.lowerOpen);
        if (hasUpper) return IDBKeyRange.upperBound(query.upper, !!query.upperOpen);
        return undefined;   // an unbounded range is the same as no query at all
    }

    function getDb(id: string) {
        const entry = _dbs[id];
        if (!entry) throw new Error('IndexedDB handle is not open.');
        return entry.db;
    }

    function txStore(id: string, store: string, mode: IDBTransactionMode) {
        return getDb(id).transaction(store, mode).objectStore(store);
    }

    // Reads only need the request to succeed.
    function awaitRequest<T>(req: IDBRequest<T>): Promise<T> {
        return new Promise<T>((resolve, reject) => {
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    // Writes resolve on transaction completion, not on request success: a request can succeed and
    // still be rolled back if the transaction later aborts, and resolving early would report a
    // write that never hit disk.
    function awaitWrite<T>(tx: IDBTransaction, req: IDBRequest<T>): Promise<T> {
        return new Promise<T>((resolve, reject) => {
            let result: any = null;
            let failed = false;
            req.onsuccess = () => { result = req.result; };
            req.onerror = () => { failed = true; reject(req.error); };
            tx.oncomplete = () => { if (!failed) resolve(result); };
            tx.onabort = () => { if (!failed) reject(tx.error ?? new Error('IndexedDB transaction aborted.')); };
        });
    }
}(BitButil));
