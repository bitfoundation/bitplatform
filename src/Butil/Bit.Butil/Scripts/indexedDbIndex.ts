var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The same reads, but through a store's index.
    butil.indexedDbIndex = {
        getByIndex,
        getKeyByIndex,
        getAllByIndex,
        getAllKeysByIndex,
        countByIndex,
        deleteByIndex
    };

    function index(id: string, store: string, indexName: string, mode: IDBTransactionMode) {
        return butil.indexedDb.txStore(id, store, mode).index(indexName);
    }

    function getByIndex(id: string, store: string, indexName: string, query: any) {
        return butil.indexedDb.awaitRequest(index(id, store, indexName, 'readonly').get(butil.indexedDb.toQuery(query))).then(v => v ?? null);
    }

    function getKeyByIndex(id: string, store: string, indexName: string, query: any) {
        return butil.indexedDb.awaitRequest(index(id, store, indexName, 'readonly').getKey(butil.indexedDb.toQuery(query))).then(v => v ?? null);
    }

    function getAllByIndex(id: string, store: string, indexName: string, query: any, count: number | null) {
        const idx = index(id, store, indexName, 'readonly');
        return butil.indexedDb.awaitRequest(count != null ? idx.getAll(butil.indexedDb.toQuery(query), count) : idx.getAll(butil.indexedDb.toQuery(query)));
    }

    function getAllKeysByIndex(id: string, store: string, indexName: string, query: any, count: number | null) {
        const idx = index(id, store, indexName, 'readonly');
        return butil.indexedDb.awaitRequest(count != null ? idx.getAllKeys(butil.indexedDb.toQuery(query), count) : idx.getAllKeys(butil.indexedDb.toQuery(query)));
    }

    function countByIndex(id: string, store: string, indexName: string, query: any) {
        return butil.indexedDb.awaitRequest(index(id, store, indexName, 'readonly').count(butil.indexedDb.toQuery(query)));
    }

    // An index has no delete() of its own, so this walks a key cursor and deletes each matching
    // record by its primary key - all inside one transaction, so it's all-or-nothing.
    function deleteByIndex(id: string, store: string, indexName: string, query: any): Promise<number> {
        const s = butil.indexedDb.txStore(id, store, 'readwrite');
        const tx = s.transaction;
        return new Promise<number>((resolve, reject) => {
            let deleted = 0;
            const req = s.index(indexName).openKeyCursor(butil.indexedDb.toQuery(query));
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
}(BitButil));
