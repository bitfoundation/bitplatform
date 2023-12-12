var BitButil = BitButil || {};

(function (butil: any) {
    butil.navigator = {
        deviceMemory,
        hardwareConcurrency,
        language,
        languages,
        maxTouchPoints,
        onLine,
        pdfViewerEnabled,
        userAgent,
        webdriver,
        canShare,
        clearAppBadge,
        sendBeacon,
        setAppBadge,
        share,
        vibrate
    };

    function deviceMemory() {
        return (window.navigator as any).deviceMemory;
    }

    function hardwareConcurrency() {
        return window.navigator.hardwareConcurrency;
    }

    function language() {
        return window.navigator.language;
    }

    function languages() {
        return window.navigator.languages;
    }

    function maxTouchPoints() {
        return window.navigator.maxTouchPoints;
    }

    function onLine() {
        return window.navigator.onLine;
    }

    function pdfViewerEnabled() {
        return window.navigator.pdfViewerEnabled;
    }

    function userAgent() {
        return window.navigator.userAgent;
    }

    function webdriver() {
        return window.navigator.webdriver;
    }

    function canShare() {
        return window.navigator.canShare();
    }

    function clearAppBadge() {
        return window.navigator.clearAppBadge();
    }

    function sendBeacon(url: string, data) {
        return window.navigator.sendBeacon(url, data)
    }

    function setAppBadge(contents) {
        return window.navigator.setAppBadge(contents);
    }

    function share(data) {
        return window.navigator.share(data);
    }

    function vibrate(pattern) {
        return window.navigator.vibrate(pattern);
    }
}(BitButil));