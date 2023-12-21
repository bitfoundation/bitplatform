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
        canShare() { return window.navigator.canShare() },
        clearAppBadge() { return window.navigator.clearAppBadge() },
        sendBeacon(url: string, data) { return window.navigator.sendBeacon(url, data) },
        setAppBadge(contents) { return window.navigator.setAppBadge(contents) },
        share(data) { return window.navigator.share(data) },
        vibrate(pattern) { return window.navigator.vibrate(pattern) }
    };
}(BitButil));