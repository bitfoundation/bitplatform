var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // A batch of writes in one transaction.
    butil.indexedDbTransaction = {
        transact
    };

    // The whole batch runs in one transaction spanning every store it touches, so a failure
    // anywhere rolls back the lot. Same reason as cursors: the transaction can't survive an
    // interop round-trip, so the operations arrive together rather than being issued one by one.
    function transact(id: string, operations: any[], mode: IDBTransactionMode, durability: string | null): Promise<any[]> {
        const db = butil.indexedDb.dbOf(id);
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
                        req = s.delete(butil.indexedDb.toQuery(op.query));
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
