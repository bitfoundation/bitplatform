var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _dbs: { [id: string]: { db: IDBDatabase, ref: any } } = {};

    butil.indexedDb = {
        isSupported() { return 'indexedDB' in window; },
        open,
        close,
        deleteDatabase,
        databases,
        cmp,
        info,
        storeInfo,
        indexInfo,
        put,
        add,
        putBytes,
        getBytes,
        get,
        getKey,
        getAll,
        getAllKeys,
        delete: del,
        clear,
        count,
        getByIndex,
        getKeyByIndex,
        getAllByIndex,
        getAllKeysByIndex,
        countByIndex,
        deleteByIndex,
        getPage,
        getKeyPage,
        getPageByIndex,
        getKeyPageByIndex,
        transact
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

    // ─── Metadata ───────────────────────────────────────────────────────────────

    function info(id: string) {
        const db = getDb(id);
        return { name: db.name, version: db.version, storeNames: Array.from(db.objectStoreNames) };
    }

    function storeInfo(id: string, store: string) {
        const db = getDb(id);
        if (!db.objectStoreNames.contains(store)) return null;
        const s = db.transaction(store, 'readonly').objectStore(store);
        return {
            name: s.name,
            keyPath: normalizeKeyPath(s.keyPath),
            autoIncrement: s.autoIncrement,
            indexNames: Array.from(s.indexNames)
        };
    }

    function indexInfo(id: string, store: string, indexName: string) {
        const db = getDb(id);
        if (!db.objectStoreNames.contains(store)) return null;
        const s = db.transaction(store, 'readonly').objectStore(store);
        if (!s.indexNames.contains(indexName)) return null;
        const idx = s.index(indexName);
        return {
            name: idx.name,
            keyPath: normalizeKeyPath(idx.keyPath),
            unique: idx.unique,
            multiEntry: idx.multiEntry
        };
    }

    // A keypath is string | string[] | null; flatten to an array so .NET sees one shape
    // (empty meaning out-of-line keys).
    function normalizeKeyPath(keyPath: any): string[] {
        if (keyPath === null || keyPath === undefined) return [];
        return Array.isArray(keyPath) ? keyPath.slice() : [keyPath];
    }

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

    // ─── CRUD ───────────────────────────────────────────────────────────────────

    // put/add resolve with the record's key, which is the only way to learn the value an
    // autoIncrement store generated.
    function put(id: string, store: string, value: any, key: any) {
        const s = txStore(id, store, 'readwrite');
        return awaitWrite(s.transaction, (key !== null && key !== undefined) ? s.put(value, key) : s.put(value));
    }

    function add(id: string, store: string, value: any, key: any) {
        const s = txStore(id, store, 'readwrite');
        return awaitWrite(s.transaction, (key !== null && key !== undefined) ? s.add(value, key) : s.add(value));
    }

    // Stored as an ArrayBuffer so the structured clone keeps it binary; JSON interop would other-
    // wise turn the bytes into a base64 string (or fail outright for large payloads).
    function putBytes(id: string, store: string, data: Uint8Array, key: any) {
        const buffer = butil.utils.arrayToBuffer(data) ?? new ArrayBuffer(0);
        const s = txStore(id, store, 'readwrite');
        return awaitWrite(s.transaction, (key !== null && key !== undefined) ? s.put(buffer, key) : s.put(buffer));
    }

    async function getBytes(id: string, store: string, query: any) {
        const value = await awaitRequest(txStore(id, store, 'readonly').get(toQuery(query)));
        if (value === null || value === undefined) return null;
        if (value instanceof ArrayBuffer) return new Uint8Array(value);
        if (ArrayBuffer.isView(value)) return new Uint8Array(value.buffer, value.byteOffset, value.byteLength);
        if (value instanceof Blob) return new Uint8Array(await value.arrayBuffer());
        return null;    // stored value isn't binary
    }

    function get(id: string, store: string, query: any) {
        return awaitRequest(txStore(id, store, 'readonly').get(toQuery(query))).then(v => v ?? null);
    }

    function getKey(id: string, store: string, query: any) {
        return awaitRequest(txStore(id, store, 'readonly').getKey(toQuery(query))).then(v => v ?? null);
    }

    function getAll(id: string, store: string, query: any, count: number | null) {
        const s = txStore(id, store, 'readonly');
        return awaitRequest(count != null ? s.getAll(toQuery(query), count) : s.getAll(toQuery(query)));
    }

    function getAllKeys(id: string, store: string, query: any, count: number | null) {
        const s = txStore(id, store, 'readonly');
        return awaitRequest(count != null ? s.getAllKeys(toQuery(query), count) : s.getAllKeys(toQuery(query)));
    }

    function del(id: string, store: string, query: any) {
        const s = txStore(id, store, 'readwrite');
        return awaitWrite(s.transaction, s.delete(toQuery(query)));
    }

    function clear(id: string, store: string) {
        const s = txStore(id, store, 'readwrite');
        return awaitWrite(s.transaction, s.clear());
    }

    function count(id: string, store: string, query: any) {
        return awaitRequest(txStore(id, store, 'readonly').count(toQuery(query)));
    }

    // ─── Indexes ────────────────────────────────────────────────────────────────

    function index(id: string, store: string, indexName: string, mode: IDBTransactionMode) {
        return txStore(id, store, mode).index(indexName);
    }

    function getByIndex(id: string, store: string, indexName: string, query: any) {
        return awaitRequest(index(id, store, indexName, 'readonly').get(toQuery(query))).then(v => v ?? null);
    }

    function getKeyByIndex(id: string, store: string, indexName: string, query: any) {
        return awaitRequest(index(id, store, indexName, 'readonly').getKey(toQuery(query))).then(v => v ?? null);
    }

    function getAllByIndex(id: string, store: string, indexName: string, query: any, count: number | null) {
        const idx = index(id, store, indexName, 'readonly');
        return awaitRequest(count != null ? idx.getAll(toQuery(query), count) : idx.getAll(toQuery(query)));
    }

    function getAllKeysByIndex(id: string, store: string, indexName: string, query: any, count: number | null) {
        const idx = index(id, store, indexName, 'readonly');
        return awaitRequest(count != null ? idx.getAllKeys(toQuery(query), count) : idx.getAllKeys(toQuery(query)));
    }

    function countByIndex(id: string, store: string, indexName: string, query: any) {
        return awaitRequest(index(id, store, indexName, 'readonly').count(toQuery(query)));
    }

    // An index has no delete() of its own, so this walks a key cursor and deletes each matching
    // record by its primary key - all inside one transaction, so it's all-or-nothing.
    function deleteByIndex(id: string, store: string, indexName: string, query: any): Promise<number> {
        const s = txStore(id, store, 'readwrite');
        const tx = s.transaction;
        return new Promise<number>((resolve, reject) => {
            let deleted = 0;
            const req = s.index(indexName).openKeyCursor(toQuery(query));
            req.onsuccess = () => {
                const cursor = req.result;
                if (!cursor) return;    // exhausted; the transaction completes on its own
                s.delete(cursor.primaryKey);
                deleted++;
                cursor.continue();
            };
            req.onerror = () => reject(req.error);
            tx.oncomplete = () => resolve(deleted);
            tx.onabort = () => reject(tx.error ?? new Error('IndexedDB transaction aborted.'));
        });
    }

    // ─── Cursors ────────────────────────────────────────────────────────────────

    // A cursor can't be stepped from .NET one record at a time: an IDB transaction goes inactive
    // as soon as control returns to the event loop, and every interop round-trip does exactly
    // that. So the walk happens here in one task and hands back a materialized page - which is
    // what skip/take/direction are for.
    function cursorPage(source: IDBObjectStore | IDBIndex, tx: IDBTransaction, query: any,
        direction: IDBCursorDirection, skip: number, take: number, keysOnly: boolean): Promise<any[]> {
        return new Promise((resolve, reject) => {
            const out: any[] = [];
            let advanced = false;
            const req = keysOnly
                ? source.openKeyCursor(query, direction)
                : source.openCursor(query, direction);

            req.onsuccess = () => {
                const cursor = req.result;
                if (!cursor) { resolve(out); return; }

                if (!advanced) {
                    advanced = true;
                    if (skip > 0) { cursor.advance(skip); return; }     // advance(0) throws
                }

                out.push(keysOnly
                    ? { key: cursor.key, primaryKey: cursor.primaryKey }
                    : { key: cursor.key, primaryKey: cursor.primaryKey, value: (cursor as IDBCursorWithValue).value });

                if (take > 0 && out.length >= take) { resolve(out); return; }
                cursor.continue();
            };
            req.onerror = () => reject(req.error);
            tx.onabort = () => reject(tx.error ?? new Error('IndexedDB transaction aborted.'));
        });
    }

    function getPage(id: string, store: string, query: any, direction: IDBCursorDirection, skip: number, take: number) {
        const s = txStore(id, store, 'readonly');
        return cursorPage(s, s.transaction, toQuery(query), direction, skip, take, false);
    }

    function getKeyPage(id: string, store: string, query: any, direction: IDBCursorDirection, skip: number, take: number) {
        const s = txStore(id, store, 'readonly');
        return cursorPage(s, s.transaction, toQuery(query), direction, skip, take, true);
    }

    function getPageByIndex(id: string, store: string, indexName: string, query: any, direction: IDBCursorDirection, skip: number, take: number) {
        const s = txStore(id, store, 'readonly');
        return cursorPage(s.index(indexName), s.transaction, toQuery(query), direction, skip, take, false);
    }

    function getKeyPageByIndex(id: string, store: string, indexName: string, query: any, direction: IDBCursorDirection, skip: number, take: number) {
        const s = txStore(id, store, 'readonly');
        return cursorPage(s.index(indexName), s.transaction, toQuery(query), direction, skip, take, true);
    }

    // ─── Transactions ───────────────────────────────────────────────────────────

    // The whole batch runs in one transaction spanning every store it touches, so a failure
    // anywhere rolls back the lot. Same reason as cursors: the transaction can't survive an
    // interop round-trip, so the operations arrive together rather than being issued one by one.
    function transact(id: string, operations: any[], mode: IDBTransactionMode, durability: string | null): Promise<any[]> {
        const db = getDb(id);
        const ops = operations || [];
        const stores = Array.from(new Set(ops.map(o => o?.store).filter(Boolean)));
        if (stores.length === 0) return Promise.resolve([]);

        let tx: IDBTransaction;
        try {
            tx = db.transaction(stores, mode, durability ? { durability: durability as IDBTransactionDurability } : undefined);
        } catch {
            tx = db.transaction(stores, mode);  // browsers predating the options argument
        }

        return new Promise<any[]>((resolve, reject) => {
            const results: any[] = new Array(ops.length).fill(null);

            for (let i = 0; i < ops.length; i++) {
                const op = ops[i];
                const s = tx.objectStore(op.store);
                let req: IDBRequest;
                switch (op.type) {
                    case 'put':
                        req = (op.key !== null && op.key !== undefined) ? s.put(op.value, op.key) : s.put(op.value);
                        break;
                    case 'add':
                        req = (op.key !== null && op.key !== undefined) ? s.add(op.value, op.key) : s.add(op.value);
                        break;
                    case 'delete':
                        req = s.delete(toQuery(op.query));
                        break;
                    case 'clear':
                        req = s.clear();
                        break;
                    default:
                        try { tx.abort(); } catch { /* not started */ }
                        reject(new Error(`Unknown IndexedDB operation '${op.type}'.`));
                        return;
                }
                const slot = i;
                req.onsuccess = () => { results[slot] = req.result ?? null; };
            }

            tx.oncomplete = () => resolve(results);
            tx.onabort = () => reject(tx.error ?? new Error('IndexedDB transaction aborted.'));
        });
    }
}(BitButil));
