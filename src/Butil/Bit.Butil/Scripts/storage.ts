var BitButil = BitButil || {};

(function (butil: any) {
    butil.storage = {
        length(storage: string) { return (window[storage] as Storage).length },
        key(storage: string, index: number) { return (window[storage] as Storage).key(index) },
        getItem(storage: string, key: string) { return (window[storage] as Storage).getItem(key) },
        setItem(storage: string, key: string, value: string) { (window[storage] as Storage).setItem(key, value) },
        removeItem(storage: string, key: string) { (window[storage] as Storage).removeItem(key) },
        clear(storage: string) { (window[storage] as Storage).clear() },
    };
}(BitButil));