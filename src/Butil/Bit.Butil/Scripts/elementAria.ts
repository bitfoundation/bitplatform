var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // The accessibility surface of an element.
    butil.elementAria = {
        ariaNotify,
        getAria(element: HTMLElement, name: string) { return (element as any)[name] ?? null },
        setAria(element: HTMLElement, name: string, value: string) { (element as any)[name] = value },
    };

    // Experimental and Chromium-only: a no-op elsewhere rather than a throw, because an
    // announcement that does not happen is not a failure of the page that asked for it.
    function ariaNotify(element: HTMLElement, message: string, options?: any) {
        const notify = (element as any).ariaNotify;
        if (typeof notify === 'function') notify.call(element, message, options ?? undefined);
    }
}(BitButil));
