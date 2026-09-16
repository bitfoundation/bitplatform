var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Paged cursor walks.
    butil.indexedDbCursor = {
        getPage,
        getKeyPage,
        getPageByIndex,
        getKeyPageByIndex
    };

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
        const s = butil.indexedDb.txStore(id, store, 'readonly');
        return cursorPage(s, s.transaction, butil.indexedDb.toQuery(query), direction, skip, take, false);
    }

    function getKeyPage(id: string, store: string, query: any, direction: IDBCursorDirection, skip: number, take: number) {
        const s = butil.indexedDb.txStore(id, store, 'readonly');
        return cursorPage(s, s.transaction, butil.indexedDb.toQuery(query), direction, skip, take, true);
    }

    function getPageByIndex(id: string, store: string, indexName: string, query: any, direction: IDBCursorDirection, skip: number, take: number) {
        const s = butil.indexedDb.txStore(id, store, 'readonly');
        return cursorPage(s.index(indexName), s.transaction, butil.indexedDb.toQuery(query), direction, skip, take, false);
    }

    function getKeyPageByIndex(id: string, store: string, indexName: string, query: any, direction: IDBCursorDirection, skip: number, take: number) {
        const s = butil.indexedDb.txStore(id, store, 'readonly');
        return cursorPage(s.index(indexName), s.transaction, butil.indexedDb.toQuery(query), direction, skip, take, true);
    }
}(BitButil));
