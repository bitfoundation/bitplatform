var BitButil = BitButil || {};

(function (butil: any) {
    const _dbs: { [id: string]: IDBDatabase } = {};

    butil.indexedDb = {
        isSupported() { return 'indexedDB' in window; },
        open,
        close,
        deleteDatabase,
        put,
        add,
        get,
        getAll,
        getAllKeys,
        delete: del,
        clear,
        count,
        getByIndex,
        getAllByIndex
    };

    // ─── Lifecycle ──────────────────────────────────────────────────────────────

    function open(id: string, name: string, version: number, stores: any[]): Promise<void> {
        return new Promise((resolve, reject) => {
            const req = indexedDB.open(name, version);
            req.onupgradeneeded = () => {
                const db = req.result;
                for (const s of stores || []) {
                    if (!s?.name) continue;
                    let store: IDBObjectStore;
                    if (db.objectStoreNames.contains(s.name)) {
                        store = req.transaction!.objectStore(s.name);
                    } else {
                        const params: IDBObjectStoreParameters = {};
                        if (s.keyPath) params.keyPath = s.keyPath;
                        if (s.autoIncrement) params.autoIncrement = true;
                        store = db.createObjectStore(s.name, params);
                    }
                    for (const idx of s.indexes || []) {
                        if (!idx?.name || !idx.keyPath) continue;
                        if (!store.indexNames.contains(idx.name)) {
                            store.createIndex(idx.name, idx.keyPath, { unique: !!idx.unique, multiEntry: !!idx.multiEntry });
                        }
                    }
                }
            };
            req.onsuccess = () => {
                const prev = _dbs[id];
                if (prev && prev !== req.result) {
                    try { prev.close(); } catch { /* already closed */ }
                }
                const db = req.result;
                // Close this connection if another tab requests a version change,
                // otherwise that tab's upgrade would be blocked by this open handle.
                db.onversionchange = () => {
                    try { db.close(); } catch { /* already closed */ }
                    if (_dbs[id] === db) delete _dbs[id];
                };
                _dbs[id] = db;
                resolve();
            };
            req.onerror = () => reject(req.error);
            req.onblocked = () => reject(new Error('IndexedDB open is blocked by another tab.'));
        });
    }

    function close(id: string) {
        const db = _dbs[id];
        if (!db) return;
        delete _dbs[id];
        try { db.close(); } catch { /* already closed */ }
    }

    function deleteDatabase(name: string): Promise<void> {
        return new Promise((resolve, reject) => {
            const req = indexedDB.deleteDatabase(name);
            req.onsuccess = () => resolve();
            req.onerror = () => reject(req.error);
            req.onblocked = () => reject(new Error('IndexedDB delete is blocked by another tab.'));
        });
    }

    // ─── CRUD ───────────────────────────────────────────────────────────────────

    function txStore(id: string, store: string, mode: IDBTransactionMode) {
        const db = _dbs[id];
        if (!db) throw new Error('IndexedDB handle is not open.');
        return db.transaction(store, mode).objectStore(store);
    }

    function awaitRequest<T>(req: IDBRequest<T>): Promise<T> {
        return new Promise<T>((resolve, reject) => {
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    function put(id: string, store: string, value: any, key: any) {
        return awaitRequest(key !== null && key !== undefined
            ? txStore(id, store, 'readwrite').put(value, key)
            : txStore(id, store, 'readwrite').put(value));
    }

    function add(id: string, store: string, value: any, key: any) {
        return awaitRequest(key !== null && key !== undefined
            ? txStore(id, store, 'readwrite').add(value, key)
            : txStore(id, store, 'readwrite').add(value));
    }

    function get(id: string, store: string, key: any) {
        return awaitRequest(txStore(id, store, 'readonly').get(key)).then(v => v ?? null);
    }

    function getAll(id: string, store: string, count: number | null) {
        const s = txStore(id, store, 'readonly');
        return awaitRequest(count != null ? s.getAll(undefined as any, count) : s.getAll());
    }

    function getAllKeys(id: string, store: string, count: number | null) {
        const s = txStore(id, store, 'readonly');
        return awaitRequest(count != null ? s.getAllKeys(undefined as any, count) : s.getAllKeys());
    }

    function del(id: string, store: string, key: any) {
        return awaitRequest(txStore(id, store, 'readwrite').delete(key));
    }

    function clear(id: string, store: string) {
        return awaitRequest(txStore(id, store, 'readwrite').clear());
    }

    function count(id: string, store: string) {
        return awaitRequest(txStore(id, store, 'readonly').count());
    }

    function getByIndex(id: string, store: string, indexName: string, key: any) {
        const idx = txStore(id, store, 'readonly').index(indexName);
        return awaitRequest(idx.get(key)).then(v => v ?? null);
    }

    function getAllByIndex(id: string, store: string, indexName: string, key: any, c: number | null) {
        const idx = txStore(id, store, 'readonly').index(indexName);
        return awaitRequest(c != null ? idx.getAll(key, c) : idx.getAll(key));
    }
}(BitButil));
