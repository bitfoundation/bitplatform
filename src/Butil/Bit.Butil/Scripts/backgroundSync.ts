var BitButil = BitButil || {};

(function (butil: any) {
    butil.backgroundSync = {
        async isSupported() {
            const reg = await window.navigator.serviceWorker?.getRegistration();
            return !!(reg && (reg as any).sync);
        },
        async isPeriodicSupported() {
            const reg = await window.navigator.serviceWorker?.getRegistration();
            return !!(reg && (reg as any).periodicSync);
        },
        async register(tag: string) {
            const reg: any = await window.navigator.serviceWorker?.getRegistration();
            if (!reg?.sync) return false;
            try { await reg.sync.register(tag); return true; }
            catch { return false; }
        },
        async getTags() {
            const reg: any = await window.navigator.serviceWorker?.getRegistration();
            if (!reg?.sync?.getTags) return [];
            try { return await reg.sync.getTags(); }
            catch { return []; }
        },
        async registerPeriodic(tag: string, minInterval: number) {
            const reg: any = await window.navigator.serviceWorker?.getRegistration();
            if (!reg?.periodicSync) return false;
            try { await reg.periodicSync.register(tag, { minInterval }); return true; }
            catch { return false; }
        },
        async getPeriodicTags() {
            const reg: any = await window.navigator.serviceWorker?.getRegistration();
            if (!reg?.periodicSync?.getTags) return [];
            try { return await reg.periodicSync.getTags(); }
            catch { return []; }
        },
        async unregisterPeriodic(tag: string) {
            const reg: any = await window.navigator.serviceWorker?.getRegistration();
            if (!reg?.periodicSync?.unregister) return false;
            try { await reg.periodicSync.unregister(tag); return true; }
            catch { return false; }
        }
    };
}(BitButil));
