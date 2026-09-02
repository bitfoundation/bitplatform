var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.navigator = {
        deviceMemory() { return (window.navigator as any).deviceMemory },
        hardwareConcurrency() { return window.navigator.hardwareConcurrency },
        language() { return window.navigator.language },
        languages() { return window.navigator.languages },
        maxTouchPoints() { return window.navigator.maxTouchPoints },
        onLine() { return window.navigator.onLine },
        pdfViewerEnabled() { return window.navigator.pdfViewerEnabled },
        userAgent() { return window.navigator.userAgent },
        webdriver() { return window.navigator.webdriver },
        cookieEnabled() { return window.navigator.cookieEnabled },
        doNotTrack() { return (window.navigator as any).doNotTrack ?? null },
        // Sticky = the user has interacted at some point (unlocks autoplay, storage access);
        // transient = an interaction is still "fresh" enough to open a popup or read the clipboard.
        userActivation() {
            const ua = (window.navigator as any).userActivation;
            if (!ua) return null;
            return { hasBeenActive: !!ua.hasBeenActive, isActive: !!ua.isActive };
        },
        canShare(data?: ShareData) { return data ? window.navigator.canShare(data) : window.navigator.canShare() },
        clearAppBadge() { return (window.navigator as any).clearAppBadge?.() },
        sendBeacon(url: string, data?: any) { return window.navigator.sendBeacon(url, data ?? undefined) },
        setAppBadge(contents?: number) { return (window.navigator as any).setAppBadge?.(contents ?? undefined) },
        share(data) { return window.navigator.share(data) },
        async shareFiles(title?: string, text?: string, url?: string, files?: any[]) {
            if (typeof window.navigator.share !== 'function' || !files?.length) return false;
            const fileObjects = files.map(f => new File([butil.utils.arrayToBuffer(f.data)], f.name, { type: f.mimeType || 'application/octet-stream' }));
            const data: any = { files: fileObjects };
            if (title) data.title = title;
            if (text) data.text = text;
            if (url) data.url = url;

            // canShare is a quick gate: rejected sets cause share() to throw on some browsers.
            if (typeof window.navigator.canShare === 'function' && !window.navigator.canShare(data)) return false;

            try {
                await window.navigator.share(data);
                return true;
            } catch {
                // AbortError when the user cancels, NotAllowedError if files were forbidden.
                return false;
            }
        },
        vibrate(pattern) { return window.navigator.vibrate(pattern) },
        canRegisterProtocolHandler() { return typeof window.navigator.registerProtocolHandler === 'function' },
        registerProtocolHandler(scheme: string, url: string) {
            if (typeof window.navigator.registerProtocolHandler !== 'function') return false;
            try {
                window.navigator.registerProtocolHandler(scheme, url);
                return true;
            } catch {
                // A disallowed scheme, a url outside the origin, or a url with no %s placeholder.
                return false;
            }
        },
        unregisterProtocolHandler(scheme: string, url: string) {
            // Non-standard and Chromium-only; a browser without it simply leaves the handler in
            // place, which the user can still remove from site settings.
            const unregister = (window.navigator as any).unregisterProtocolHandler;
            if (typeof unregister !== 'function') return false;
            try {
                unregister.call(window.navigator, scheme, url);
                return true;
            } catch {
                return false;
            }
        },
        canGetInstalledRelatedApps() { return typeof (window.navigator as any).getInstalledRelatedApps === 'function' },
        async getInstalledRelatedApps() {
            const get = (window.navigator as any).getInstalledRelatedApps;
            if (typeof get !== 'function') return [];
            try {
                const apps = await get.call(window.navigator);
                return (apps ?? []).map((a: any) => ({
                    id: a.id ?? '',
                    platform: a.platform ?? '',
                    url: a.url ?? '',
                    version: a.version ?? ''
                }));
            } catch {
                // Not a secure context, or the manifest declares no related applications.
                return [];
            }
        }
    };
}(BitButil));