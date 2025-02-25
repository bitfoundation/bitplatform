var BitButil = BitButil || {};

(function (butil: any) {
    let _refs = {};

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
        atob(data: string) { return window.atob(data) },
        alert(message?: string) { window.alert(message) },
        blur() { window.blur() },
        btoa(data: string) { return window.btoa(data) },
        close,
        confirm(message?: string) { return window.confirm(message) },
        find,
        focus() { window.focus() },
        getSelection() { return window.getSelection().toString() }, // TODO: needs to be improved and not only return the toString
        matchMedia,
        open,
        print() { window.print() },
        prompt(message?: string, defaultValue?: string) { return window.prompt(message, defaultValue) },
        scroll,
        scrollBy,
        stop() { window.stop() },
        dispose
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

    function close(id: string | undefined) {
        if (!id) return window.close();

        const ref = _refs[id];
        if (!ref) return;
        delete _refs[id];
        return ref.close();
    }

    function find(text?: string,
        caseSensitive?: boolean,
        backward?: boolean,
        wrapAround?: boolean,
        wholeWord?: boolean,
        searchInFrame?: boolean) {
        return (window as any).find(text, caseSensitive, backward, wrapAround, wholeWord, searchInFrame);
    }

    function matchMedia(query: string) {
        const media = window.matchMedia(query);
        return {
            matches: media.matches,
            media: media.media
        };
    }

    function open(id: string, url?: string, target?: string, windowFeatures?: string) {
        const ref = window.open(url, target, windowFeatures);
        if (!ref) return;
        _refs[id] = ref;
        return id;
    }

    function scroll(options?: ScrollToOptions, x?: number, y?: number) {
        if (options) {
            window.scroll(options);
        } else {
            window.scroll(x, y);
        }
    }

    function scrollBy(options?: ScrollToOptions, x?: number, y?: number) {
        if (options) {
            window.scrollBy(options);
        } else {
            window.scrollBy(x, y);
        }
    }

    function dispose() {
        _refs = {};
    }
}(BitButil));