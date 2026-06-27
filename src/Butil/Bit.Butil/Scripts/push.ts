var BitButil = BitButil || {};

(function (butil: any) {
    butil.push = {
        isSupported() {
            return 'serviceWorker' in window.navigator && 'PushManager' in window;
        },
        async getSubscription() { return await readSubscription(); },
        async subscribe(applicationServerKey: string, userVisibleOnly: boolean) {
            const reg = await window.navigator.serviceWorker?.getRegistration();
            if (!reg?.pushManager) return inactive();
            try {
                const sub = await reg.pushManager.subscribe({
                    applicationServerKey: urlBase64ToUint8Array(applicationServerKey),
                    userVisibleOnly: !!userVisibleOnly
                });
                return toInfo(sub);
            } catch {
                return inactive();
            }
        },
        async unsubscribe() {
            const reg = await window.navigator.serviceWorker?.getRegistration();
            const sub = await reg?.pushManager?.getSubscription();
            if (!sub) return false;
            try { return await sub.unsubscribe(); } catch { return false; }
        }
    };

    function inactive() {
        return { isActive: false, endpoint: '', expirationTime: null, p256dh: '', auth: '' };
    }

    async function readSubscription() {
        const reg = await window.navigator.serviceWorker?.getRegistration();
        const sub = await reg?.pushManager?.getSubscription();
        return sub ? toInfo(sub) : inactive();
    }

    function toInfo(sub: PushSubscription) {
        const json = sub.toJSON() as any;
        return {
            isActive: true,
            endpoint: json.endpoint,
            expirationTime: typeof json.expirationTime === 'number' ? json.expirationTime : null,
            p256dh: json.keys?.p256dh ?? '',
            auth: json.keys?.auth ?? ''
        };
    }

    /** VAPID keys are base64url; PushManager.subscribe needs a Uint8Array. */
    function urlBase64ToUint8Array(value: string) {
        const padding = '='.repeat((4 - value.length % 4) % 4);
        const base64 = (value + padding).replace(/-/g, '+').replace(/_/g, '/');
        const raw = atob(base64);
        const bytes = new Uint8Array(raw.length);
        for (let i = 0; i < raw.length; i++) bytes[i] = raw.charCodeAt(i);
        return bytes;
    }
}(BitButil));
