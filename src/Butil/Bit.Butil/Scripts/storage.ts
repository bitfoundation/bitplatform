var BitButil = BitButil || {};

(function (butil: any) {
    const _handlers: { [id: string]: (e: StorageEvent) => void } = {};

    butil.storage = {
        length(storage: string) { return (window[storage] as Storage).length },
        key(storage: string, index: number) { return (window[storage] as Storage).key(index) },
        containsKey(storage: string, key: string) { return (window[storage] as Storage).getItem(key) !== null },
        getItem(storage: string, key: string) { return (window[storage] as Storage).getItem(key) },
        setItem(storage: string, key: string, value: string) { (window[storage] as Storage).setItem(key, value) },
        removeItem(storage: string, key: string) { (window[storage] as Storage).removeItem(key) },
        clear(storage: string) { (window[storage] as Storage).clear() },
        subscribe(dotNetRef: any, listenerId: string) {
            const handler = (e: StorageEvent) => {
                const area = e.storageArea === window.localStorage ? 'localStorage'
                    : e.storageArea === window.sessionStorage ? 'sessionStorage'
                        : '';
                butil.utils.dispatch(dotNetRef, 'InvokeStorageEvent', listenerId, {
                    key: e.key,
                    oldValue: e.oldValue,
                    newValue: e.newValue,
                    url: e.url,
                    storageArea: area
                });
            };
            _handlers[listenerId] = handler;
            window.addEventListener('storage', handler);
        },
        unsubscribe(listenerId: string) {
            const handler = _handlers[listenerId];
            if (!handler) return;
            delete _handlers[listenerId];
            window.removeEventListener('storage', handler);
        }
    };
}(BitButil));