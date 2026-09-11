var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Reads and writes against an object store by primary key.
    butil.indexedDbStore = {
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
        count
    };

    // put/add resolve with the record's key, which is the only way to learn the value an
    // autoIncrement store generated.
    function put(id: string, store: string, value: any, key: any) {
        const s = butil.indexedDb.txStore(id, store, 'readwrite');
        return butil.indexedDb.awaitWrite(s.transaction, (key !== null && key !== undefined) ? s.put(value, key) : s.put(value));
    }

    function add(id: string, store: string, value: any, key: any) {
        const s = butil.indexedDb.txStore(id, store, 'readwrite');
        return butil.indexedDb.awaitWrite(s.transaction, (key !== null && key !== undefined) ? s.add(value, key) : s.add(value));
    }

    // Stored as an ArrayBuffer so the structured clone keeps it binary; JSON interop would other-
    // wise turn the bytes into a base64 string (or fail outright for large payloads).
    function putBytes(id: string, store: string, data: Uint8Array, key: any) {
        const buffer = butil.utils.arrayToBuffer(data) ?? new ArrayBuffer(0);
        const s = butil.indexedDb.txStore(id, store, 'readwrite');
        return butil.indexedDb.awaitWrite(s.transaction, (key !== null && key !== undefined) ? s.put(buffer, key) : s.put(buffer));
    }

    async function getBytes(id: string, store: string, query: any) {
        const value = await butil.indexedDb.awaitRequest(butil.indexedDb.txStore(id, store, 'readonly').get(butil.indexedDb.toQuery(query)));
        if (value === null || value === undefined) return null;
        if (value instanceof ArrayBuffer) return new Uint8Array(value);
        if (ArrayBuffer.isView(value)) return new Uint8Array(value.buffer, value.byteOffset, value.byteLength);
        if (value instanceof Blob) return new Uint8Array(await value.arrayBuffer());
        return null;    // stored value isn't binary
    }

    function get(id: string, store: string, query: any) {
        return butil.indexedDb.awaitRequest(butil.indexedDb.txStore(id, store, 'readonly').get(butil.indexedDb.toQuery(query))).then(v => v ?? null);
    }

    function getKey(id: string, store: string, query: any) {
        return butil.indexedDb.awaitRequest(butil.indexedDb.txStore(id, store, 'readonly').getKey(butil.indexedDb.toQuery(query))).then(v => v ?? null);
    }

    function getAll(id: string, store: string, query: any, count: number | null) {
        const s = butil.indexedDb.txStore(id, store, 'readonly');
        return butil.indexedDb.awaitRequest(count != null ? s.getAll(butil.indexedDb.toQuery(query), count) : s.getAll(butil.indexedDb.toQuery(query)));
    }

    function getAllKeys(id: string, store: string, query: any, count: number | null) {
        const s = butil.indexedDb.txStore(id, store, 'readonly');
        return butil.indexedDb.awaitRequest(count != null ? s.getAllKeys(butil.indexedDb.toQuery(query), count) : s.getAllKeys(butil.indexedDb.toQuery(query)));
    }

    function del(id: string, store: string, query: any) {
        const s = butil.indexedDb.txStore(id, store, 'readwrite');
        return butil.indexedDb.awaitWrite(s.transaction, s.delete(butil.indexedDb.toQuery(query)));
    }

    function clear(id: string, store: string) {
        const s = butil.indexedDb.txStore(id, store, 'readwrite');
        return butil.indexedDb.awaitWrite(s.transaction, s.clear());
    }

    function count(id: string, store: string, query: any) {
        return butil.indexedDb.awaitRequest(butil.indexedDb.txStore(id, store, 'readonly').count(butil.indexedDb.toQuery(query)));
    }
}(BitButil));
