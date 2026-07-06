var BitButil = BitButil || {};

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
        vibrate(pattern) { return window.navigator.vibrate(pattern) }
    };
}(BitButil));