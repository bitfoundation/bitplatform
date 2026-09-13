var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    butil.cookie = {
        get() { return document.cookie },
        set(cookie: string) { document.cookie = cookie },
    };
}(BitButil));