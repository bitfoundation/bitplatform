var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Configured sanitizers kept by id so one configuration can be built once and reused for every
    // fragment - building a Sanitizer per call is the expensive half of sanitizing a list of items.
    const _sanitizers: { [id: string]: any } = {};

    // Reading a sanitizer's configuration back, under either of the two names the method has
    // carried. It is also how a configuration is confirmed to have been understood at all.
    function configOf(sanitizer: any) {
        for (const name of ['get', 'getConfiguration']) {
            if (typeof sanitizer?.[name] !== 'function') continue;
            try { return sanitizer[name](); } catch { return null; }
        }
        return null;
    }

    // A configuration is a WebIDL dictionary, so a member the build does not know is *ignored*, not
    // rejected: constructing under the wrong spelling succeeds and quietly yields the permissive
    // default sanitizer. Catching an exception therefore proves nothing - the only evidence that a
    // spelling was understood is the sanitizer echoing those keys back in its own configuration.
    function applied(sanitizer: any, keys: string[]) {
        const config = configOf(sanitizer);
        // Nothing to read the configuration back from: construction is the only signal there is.
        if (!config || Object.keys(config).length === 0) return true;
        return keys.some(key => key in config);
    }

    function construct(config: any) {
        try { return new ((window as any).Sanitizer)(config); } catch { return null; }
    }

    function build(config: any) {
        const ctor = (window as any).Sanitizer;
        if (typeof ctor !== 'function') return null;
        if (!config) return new ctor();

        // Only the keys the caller actually set are passed on: null is not a value any of these
        // members accepts, so sending one for everything left unset would be rejected outright.
        const cleaned: any = {};
        for (const key of Object.keys(config)) {
            const value = config[key];
            if (value !== null && value !== undefined) cleaned[key] = value;
        }

        // An empty configuration object is not "no configuration": it is an allow-list allowing
        // nothing, which strips every element. A caller who set no property means the default.
        if (Object.keys(cleaned).length === 0) return new ctor();

        const candidate = construct(cleaned);
        if (candidate && applied(candidate, Object.keys(cleaned))) return candidate;

        // Chrome shipped `allowElements`/`allowAttributes` before the names were settled; retrying
        // under the old spelling keeps a configuration working on those builds.
        const renamed: { [key: string]: string } = {
            elements: 'allowElements',
            attributes: 'allowAttributes',
            comments: 'allowComments',
            dataAttributes: 'allowDataAttributes'
        };
        const legacy: any = {};
        for (const key of Object.keys(cleaned)) legacy[renamed[key] ?? key] = cleaned[key];

        const fallback = construct(legacy);
        if (fallback && applied(fallback, Object.keys(legacy))) return fallback;

        // Neither spelling took. Handing back a sanitizer built from no configuration at all would
        // let through exactly the markup the caller's allow-list excludes, so this is a failure.
        return null;
    }

    // An id that isn't in the registry - disposed, or never created because the browser has no
    // configurable Sanitizer - is not the same as "no configuration": sanitizing under the default
    // instead would quietly ignore the configuration the caller asked for. It is reported as the
    // failure the API documents rather than substituted for.
    const MISSING = {};

    function resolve(id: string | null) {
        if (!id) return build(null);
        return _sanitizers[id] ?? MISSING;
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
            if (sanitizer === MISSING || !sanitizer?.get) return null;
            try { return sanitizer.get(); } catch { return null; }
        },

        sanitize(html: string, id: string | null) {
            const sanitizer = resolve(id);
            if (sanitizer === MISSING) return null;
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
            if (sanitizer === MISSING) return false;
            try { return into(element, html, sanitizer); } catch { return false; }
        }
    };
}(BitButil));
