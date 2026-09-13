var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const CSSNS = () => (window as any).CSS;

    // Computed styles and the CSS namespace's own queries. The stylesheets a page builds are
    // cssStyleSheet, the Houdini worklets cssWorklet, the Custom Highlight API cssHighlight and the
    // Typed OM cssTypedOm - four features that share nothing with this one but the CSS object.
    butil.css = {
        isSupported() { return typeof window.getComputedStyle === 'function'; },
        isSupportsAvailable() { return typeof CSSNS()?.supports === 'function'; },
        isRegisterPropertyAvailable() { return typeof CSSNS()?.registerProperty === 'function'; },
        // The resolved value of each named property: what the element is actually rendered with,
        // after the cascade, inheritance, and the browser's own resolution of relative units into
        // pixels. Asking for a list rather than the whole object on purpose - a computed style has
        // some 350 properties on it, and marshalling all of them to answer a question about two is
        // most of the cost of the call.
        computed(element: any, properties: string[], pseudoElement: string | null) {
            if (!element) return null;

            const style = window.getComputedStyle(element, pseudoElement || undefined);
            const result: any = {};
            for (const property of properties ?? []) result[property] = style.getPropertyValue(property);
            return result;
        },

        computedAll(element: any, pseudoElement: string | null) {
            if (!element) return null;

            const style = window.getComputedStyle(element, pseudoElement || undefined);
            const result: any = {};
            for (let i = 0; i < style.length; i++) {
                const property = style.item(i);
                result[property] = style.getPropertyValue(property);
            }
            return result;
        },

        supports(property: string, value: string) {
            const CSSApi = CSSNS();
            if (typeof CSSApi?.supports !== 'function') return false;
            try { return CSSApi.supports(property, value); } catch { return false; }
        },

        supportsCondition(condition: string) {
            const CSSApi = CSSNS();
            if (typeof CSSApi?.supports !== 'function') return false;
            try { return CSSApi.supports(condition); } catch { return false; }
        },

        // Makes a string safe to put in a selector. An id that starts with a digit, or contains a
        // dot, is legal HTML and illegal CSS without this.
        escape(value: string) {
            const CSSApi = CSSNS();
            if (typeof CSSApi?.escape !== 'function') return value;
            try { return CSSApi.escape(value); } catch { return value; }
        },

        // Teaches the browser what a custom property means, which is what lets one be animated or
        // transitioned - an unregistered custom property is just a string, and strings do not
        // interpolate.
        registerProperty(name: string, syntax: string, inherits: boolean, initialValue: string | null) {
            const CSSApi = CSSNS();
            if (typeof CSSApi?.registerProperty !== 'function') return 'not supported';
            try {
                const definition: any = { name, syntax, inherits };
                if (initialValue !== null) definition.initialValue = initialValue;
                CSSApi.registerProperty(definition);
                return null;
            } catch (e: any) {
                // Registering the same name twice throws, and so does a syntax the browser does not
                // understand. Both are answers rather than crashes.
                return e?.message ?? String(e);
            }
        },
    };
}(BitButil));
