var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // What a database says about itself. Its own module because schema inspection is a tooling
    // and migration concern, not something a data path calls.
    butil.indexedDbInfo = {
        info,
        storeInfo,
        indexInfo
    };

    function info(id: string) {
        const db = butil.indexedDb.dbOf(id);
        return { name: db.name, version: db.version, storeNames: Array.from(db.objectStoreNames) };
    }

    function storeInfo(id: string, store: string) {
        const db = butil.indexedDb.dbOf(id);
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
        const db = butil.indexedDb.dbOf(id);
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
}(BitButil));
