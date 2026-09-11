var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const _rules: { [id: string]: HTMLScriptElement } = {};
    const _listeners: { [id: string]: () => void } = {};

    function navigationEntry(): any {
        try { return performance.getEntriesByType('navigation')[0] ?? null; } catch { return null; }
    }

    butil.speculation = {
        isSupported() {
            return typeof (HTMLScriptElement as any).supports === 'function'
                && (HTMLScriptElement as any).supports('speculationrules');
        },

        // True while this document is being prerendered in the background - nothing it does is
        // visible yet, and side effects (analytics, a play(), a POST) are the ones to hold back.
        isPrerendering() { return (document as any).prerendering === true; },
        // Non-zero once a prerendered document has been activated: the time the prerender started,
        // relative to which every other timestamp on the page was measured.
        activationStart() { return navigationEntry()?.activationStart ?? 0; },
        wasPrerendered() { return (navigationEntry()?.activationStart ?? 0) > 0; },

        // The rules are a script element in the document rather than a header, so an app can add and
        // remove them as the user moves around - which is the whole reason to drive them from C#.
        addRules(id: string, json: string) {
            if (!butil.speculation.isSupported()) return false;
            butil.speculation.removeRules(id);
            try {
                const script = document.createElement('script');
                script.type = 'speculationrules';
                // textContent, not innerHTML: this is a JSON document, and the sanitizing sinks would
                // be the wrong tool for it in either direction.
                script.textContent = json;
                document.head.appendChild(script);
                _rules[id] = script;
                return true;
            } catch {
                return false;
            }
        },
        // Removing the element cancels speculations it started that have not been used yet.
        removeRules(id: string) {
            const script = _rules[id];
            if (!script) return;
            delete _rules[id];
            script.remove();
        },

        onPrerenderingChange(dotNetRef: any, method: string, id: string) {
            const handler = () => butil.utils.dispatch(dotNetRef, method, id, butil.speculation.activationStart());
            _listeners[id] = handler;
            // Fires once, on the document that was prerendered, at the moment the user actually
            // navigates to it. A document that was never prerendered never sees it.
            document.addEventListener('prerenderingchange', handler);
        },
        offPrerenderingChange(id: string) {
            const handler = _listeners[id];
            if (!handler) return;
            delete _listeners[id];
            document.removeEventListener('prerenderingchange', handler);
        },

        disposeAll() {
            for (const id of Object.keys(_rules)) butil.speculation.removeRules(id);
            for (const id in _listeners) {
                document.removeEventListener('prerenderingchange', _listeners[id]);
                delete _listeners[id];
            }
        }
    };
}(BitButil));
