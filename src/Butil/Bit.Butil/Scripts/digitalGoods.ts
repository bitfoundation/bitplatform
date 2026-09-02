var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const DEFAULT_PROVIDER = 'https://play.google.com/billing';

    // One service object per provider, kept for the life of the page: getDigitalGoodsService() is a
    // connection to the store, not a lookup, and reconnecting on every call would put a round trip
    // in front of each one.
    const _services: { [provider: string]: any } = {};

    butil.digitalGoods = {
        isSupported() { return typeof (window as any).getDigitalGoodsService === 'function'; },
        connect,
        getDetails,
        listPurchases,
        listPurchaseHistory,
        consume
    };

    async function service(provider: string) {
        const W = window as any;
        if (typeof W.getDigitalGoodsService !== 'function') return null;

        const key = provider || DEFAULT_PROVIDER;
        if (_services[key]) return _services[key];

        try {
            const connected = await W.getDigitalGoodsService(key);
            if (connected) _services[key] = connected;
            return connected ?? null;
        } catch {
            // Not installed from this store - the ordinary answer in a browser tab.
            return null;
        }
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
            return [];
        }
    }

    async function listPurchases(provider: string) {
        const store = await service(provider);
        if (!store?.listPurchases) return [];

        try { return ((await store.listPurchases()) || []).map(toPurchase); }
        catch { return []; }
    }

    async function listPurchaseHistory(provider: string) {
        const store = await service(provider);
        if (!store?.listPurchaseHistory) return [];

        try { return ((await store.listPurchaseHistory()) || []).map(toPurchase); }
        catch { return []; }
    }

    async function consume(purchaseToken: string, provider: string) {
        const store = await service(provider);
        if (!store?.consume || !purchaseToken) return false;

        try { await store.consume(purchaseToken); return true; }
        catch { return false; }
    }
}(BitButil));
