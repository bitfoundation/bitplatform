var BitButil = BitButil || {};

(function (butil: any) {
    butil.window = {
        addBeforeUnload,
        removeBeforeUnload,
        innerHeight() { return window.innerHeight },
        innerWidth() { return window.innerWidth },
        isSecureContext() { return window.isSecureContext },
        locationbar() { return window.locationbar },
        getName() { return window.name },
        setName(value: string) { window.name = value },
        origin() { return window.origin },
        outerHeight() { return window.outerHeight },
        outerWidth() { return window.outerWidth },
        screenX() { return window.screenX },
        screenY() { return window.screenY },
        scrollX() { return window.scrollX },
        scrollY() { return window.scrollY },
    };

    function addBeforeUnload() {
        window.onbeforeunload = e => {
            e.preventDefault();
            e.returnValue = true;
            return true;
        };
    }

    function removeBeforeUnload() {
        window.onbeforeunload = null;
    }
}(BitButil));