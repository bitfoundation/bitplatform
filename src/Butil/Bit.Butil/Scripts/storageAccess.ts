var BitButil = BitButil || {};

(function (butil: any) {
    butil.storageAccess = {
        isSupported() { return typeof (document as any).requestStorageAccess === 'function'; },
        async hasAccess() {
            const d = document as any;
            if (typeof d.hasStorageAccess !== 'function') return true;   // no partitioning here
            try { return await d.hasStorageAccess(); } catch { return false; }
        },
        async request() {
            const d = document as any;
            if (typeof d.requestStorageAccess !== 'function') return false;
            try {
                await d.requestStorageAccess();
                return true;
            } catch {
                // Rejects when the user declines, when there was no user gesture, or when the
                // embedder's permissions policy forbids it. None is distinguishable from the
                // others by design - the spec deliberately gives no reason.
                return false;
            }
        },
        isRequestForSupported() { return typeof (document as any).requestStorageAccessFor === 'function'; },
        // The top-level counterpart: a first-party page asks on behalf of an origin it embeds, so
        // the embedded frame doesn't need its own user gesture. Chromium only.
        async requestFor(origin: string) {
            const d = document as any;
            if (typeof d.requestStorageAccessFor !== 'function') return false;
            try {
                await d.requestStorageAccessFor(origin);
                return true;
            } catch {
                return false;
            }
        }
    };
}(BitButil));
