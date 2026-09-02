var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Configured sanitizers kept by id so one configuration can be built once and reused for every
    // fragment - building a Sanitizer per call is the expensive half of sanitizing a list of items.
    const _sanitizers: { [id: string]: any } = {};

    function build(config: any) {
        const ctor = (window as any).Sanitizer;
        if (typeof ctor !== 'function') return null;
        if (!config) return new ctor();

        // Only the keys the caller actually set are passed on: a runtime that doesn't know a key
        // rejects the whole configuration, so sending nulls for everything unset would fail on the
        // very browsers this has to work in.
        const cleaned: any = {};
        for (const key of Object.keys(config)) {
            const value = config[key];
            if (value !== null && value !== undefined) cleaned[key] = value;
        }

        try { return new ctor(cleaned); } catch { /* fall through to the older spelling */ }

        // Chrome shipped `allowElements`/`allowAttributes` before the names were settled; retrying
        // under the old spelling keeps a configuration working on those builds instead of silently
        // falling back to the default (more permissive) sanitizer.
        const legacy: any = {};
        const renamed: { [key: string]: string } = {
            elements: 'allowElements',
            attributes: 'allowAttributes',
            comments: 'allowComments',
            dataAttributes: 'allowDataAttributes'
        };
        for (const key of Object.keys(cleaned)) legacy[renamed[key] ?? key] = cleaned[key];

        try { return new ctor(legacy); } catch { return null; }
    }

    function resolve(id: string | null) {
        if (!id) return build(null);
        return _sanitizers[id] ?? null;
    }

    // setHTML is the only sanitizing entry point in the platform, and it writes into an element -
    // so sanitizing to a string means writing into a detached one and reading it back. The element
    // is never in the document, so nothing it contains loads, runs or is announced.
    function into(element: Element, html: string, sanitizer: any) {
        // Only the sanitizing sink is ever used here. setHTMLUnsafe would also accept a configuration,
        // but it drops the baseline that removes scripts and event handlers - and a service called
        // "sanitizer" handing back markup that was not sanitized is the one outcome worth ruling out.
        const set = (element as any).setHTML;
        if (typeof set !== 'function') return false;
        set.call(element, html ?? '', sanitizer ? { sanitizer } : undefined);
        return true;
    }

    butil.sanitizer = {
        isSupported() { return typeof (window as any).Sanitizer === 'function'; },
        // The sanitizing sink can be there without the configurable Sanitizer object being there,
        // and the no-configuration case only needs the sink.
        isSetHtmlSupported() { return typeof (document.createElement('div') as any).setHTML === 'function'; },

        create(id: string, config: any) {
            const sanitizer = build(config);
            if (!sanitizer) return false;
            _sanitizers[id] = sanitizer;
            return true;
        },
        dispose(id: string) { delete _sanitizers[id]; },
        disposeAll() {
            for (const id in _sanitizers) delete _sanitizers[id];
        },

        // What a configuration actually expands to once the browser has merged it with its baseline -
        // the answer to "is this element really allowed", which a hand-written config never gives.
        getConfig(id: string | null) {
            const sanitizer = resolve(id);
            if (!sanitizer?.get) return null;
            try { return sanitizer.get(); } catch { return null; }
        },

        sanitize(html: string, id: string | null) {
            const sanitizer = resolve(id);
            const container = document.createElement('div');
            try {
                if (into(container, html, sanitizer) === false) return null;
            } catch {
                return null;
            }
            return container.innerHTML;
        },
        sanitizeInto(element: Element, html: string, id: string | null) {
            if (!element) return false;
            const sanitizer = resolve(id);
            try { return into(element, html, sanitizer); } catch { return false; }
        }
    };
}(BitButil));
