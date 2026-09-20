var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const DEFAULT_PROVIDER = 'https://play.google.com/billing';

    // One connection per provider, kept for the life of the page: getDigitalGoodsService() is a
    // connection to the store, not a lookup, and reconnecting on every call would put a round trip
    // in front of each one. What is kept is the pending promise, not the resolved service, so
    // concurrent first calls - listing purchases and fetching the catalogue together on launch - share
    // one connection attempt instead of each opening a connection and orphaning all but the last.
    const _services: { [provider: string]: Promise<any> } = {};

    butil.digitalGoods = {
        isSupported() { return typeof (window as any).getDigitalGoodsService === 'function'; },
        connect,
        getDetails,
        listPurchases,
        listPurchaseHistory,
        consume
    };

    function service(provider: string): Promise<any> {
        const W = window as any;
        if (typeof W.getDigitalGoodsService !== 'function') return Promise.resolve(null);

        const key = provider || DEFAULT_PROVIDER;
        return _services[key] || (_services[key] = openService(W, key));
    }

    async function openService(W: any, key: string) {
        try {
            const connected = await W.getDigitalGoodsService(key);
            if (connected) return connected;
        } catch {
            // Not installed from this store - the ordinary answer in a browser tab.
        }

        delete _services[key];
        return null;
    }

    // A store call that rejects may mean the connection behind it is gone (the store app restarted),
    // and a cached dead connection would answer every later call with nothing until a reload. Dropping
    // it makes the next call reconnect.
    function forget(provider: string) {
        delete _services[provider || DEFAULT_PROVIDER];
    }

    function toAmount(amount: any) {
        return amount ? { currency: amount.currency ?? '', value: amount.value ?? '' } : null;
    }

    function toPurchase(purchase: any) {
        return { itemId: purchase.itemId ?? '', purchaseToken: purchase.purchaseToken ?? '' };
    }

    async function connect(provider: string) {
        return !!(await service(provider));
    }

    async function getDetails(itemIds: string[], provider: string) {
        const store = await service(provider);
        if (!store?.getDetails || !itemIds?.length) return [];

        try {
            const items = await store.getDetails(itemIds);
            return (items || []).map((item: any) => ({
                itemId: item.itemId ?? '',
                title: item.title ?? '',
                description: item.description ?? null,
                price: toAmount(item.price),
                type: item.type ?? null,
                // iconURLs in the specification; Chrome shipped iconUrls first and still answers to it.
                iconUrls: item.iconURLs ?? item.iconUrls ?? [],
                subscriptionPeriod: item.subscriptionPeriod ?? null,
                freeTrialPeriod: item.freeTrialPeriod ?? null,
                introductoryPrice: toAmount(item.introductoryPrice),
                introductoryPricePeriod: item.introductoryPricePeriod ?? null,
                introductoryPriceCycles: item.introductoryPriceCycles ?? null
            }));
        } catch {
            forget(provider);
            return [];
        }
    }

    async function listPurchases(provider: string) {
        const store = await service(provider);
        if (!store?.listPurchases) return [];

        try { return ((await store.listPurchases()) || []).map(toPurchase); }
        catch { forget(provider); return []; }
    }

    async function listPurchaseHistory(provider: string) {
        const store = await service(provider);
        if (!store?.listPurchaseHistory) return [];

        try { return ((await store.listPurchaseHistory()) || []).map(toPurchase); }
        catch { forget(provider); return []; }
    }

    async function consume(purchaseToken: string, provider: string) {
        const store = await service(provider);
        if (!store?.consume || !purchaseToken) return false;

        try { await store.consume(purchaseToken); return true; }
        catch { forget(provider); return false; }
    }
}(BitButil));
