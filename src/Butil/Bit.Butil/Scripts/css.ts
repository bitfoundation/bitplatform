var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const CSSNS = () => (window as any).CSS;

    // Stylesheets this module made, so .NET can go on editing one after it is in the document.
    const _sheets: { [id: string]: { sheet?: any; element?: HTMLStyleElement } } = {};

    // A <style> element's CSSStyleSheet is not stable: setting its text content discards the sheet
    // it had and builds a new one, so an element-backed entry has to be read through the element
    // every time rather than cached at creation.
    function sheetOf(entry: { sheet?: any; element?: HTMLStyleElement } | undefined) {
        if (!entry) return null;
        return entry.element ? entry.element.sheet : entry.sheet;
    }

    // A CSSStyleValue does not cross interop, so every value is flattened to the same shape: the
    // numeric part and its unit when it has one, plus the text the value serializes to.
    function toValue(value: any) {
        if (value === null || value === undefined) return null;
        const isNumeric = typeof value.value === 'number';
        return {
            value: isNumeric ? value.value : 0,
            unit: isNumeric ? (value.unit ?? '') : '',
            text: typeof value.toString === 'function' ? value.toString() : String(value),
            isNumeric
        };
    }

    butil.css = {
        isSupported() { return typeof window.getComputedStyle === 'function'; },
        isSupportsAvailable() { return typeof CSSNS()?.supports === 'function'; },
        isRegisterPropertyAvailable() { return typeof CSSNS()?.registerProperty === 'function'; },
        isConstructableStyleSheetAvailable() {
            try { return typeof CSSStyleSheet === 'function' && 'replaceSync' in CSSStyleSheet.prototype; }
            catch { return false; }
        },
        isHighlightAvailable() { return typeof (window as any).Highlight === 'function' && !!CSSNS()?.highlights; },

        // The Typed OM's unit factories (CSS.px and friends) are what the style-map half of this
        // module needs, and they ship separately from getComputedStyle - hence its own probe.
        isTypedOmAvailable() { return typeof CSSNS()?.px === 'function'; },
        supportsPaintWorklet() { return !!CSSNS()?.paintWorklet; },
        supportsLayoutWorklet() { return !!CSSNS()?.layoutWorklet; },

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

        // Houdini's paint worklet: a script that draws a custom paint() image the way a canvas does,
        // but as a live CSS value. It runs in its own global scope with no DOM, which is what makes
        // it fast and what makes it unable to reach anything in the page.
        async addPaintWorklet(url: string) {
            const worklet = CSSNS()?.paintWorklet;
            if (!worklet?.addModule) return false;
            try {
                await worklet.addModule(url);
                return true;
            } catch {
                // The module 404'd, or threw while registering its paint class.
                return false;
            }
        },

        async addLayoutWorklet(url: string) {
            const worklet = CSSNS()?.layoutWorklet;
            if (!worklet?.addModule) return false;
            try {
                await worklet.addModule(url);
                return true;
            } catch {
                return false;
            }
        },

        // A stylesheet of your own. Constructable sheets are adopted by the document without an
        // element in the markup; where they are missing a <style> element behaves the same from
        // .NET's side.
        createSheet(id: string) {
            if (butil.css.isConstructableStyleSheetAvailable()) {
                const sheet = new CSSStyleSheet();
                (document as any).adoptedStyleSheets = [...((document as any).adoptedStyleSheets ?? []), sheet];
                _sheets[id] = { sheet };
                return true;
            }

            const element = document.createElement('style');
            document.head.appendChild(element);
            _sheets[id] = { element };
            return true;
        },

        insertRule(id: string, rule: string, index: number) {
            const sheet = sheetOf(_sheets[id]);
            if (!sheet) return -1;
            try {
                return sheet.insertRule(rule, index >= 0 ? index : sheet.cssRules.length);
            } catch {
                // A rule the parser rejects throws rather than being ignored - which is the useful
                // behaviour, and why this answers -1 instead of pretending it worked.
                return -1;
            }
        },

        deleteRule(id: string, index: number) {
            const sheet = sheetOf(_sheets[id]);
            if (!sheet) return false;
            try { sheet.deleteRule(index); return true; } catch { return false; }
        },

        rules(id: string) {
            const sheet = sheetOf(_sheets[id]);
            if (!sheet) return [];
            try { return Array.from(sheet.cssRules).map((rule: any) => rule.cssText); }
            catch { return []; }   // a cross-origin sheet refuses to be read
        },

        replaceSheet(id: string, css: string) {
            const entry = _sheets[id];
            if (!entry) return false;
            try {
                // The element path replaces the sheet rather than editing it, which is exactly why
                // nothing here holds on to the old one.
                if (entry.element) { entry.element.textContent = css; return true; }
                if (typeof entry.sheet?.replaceSync === 'function') { entry.sheet.replaceSync(css); return true; }
                return false;
            } catch { return false; }
        },

        removeSheet(id: string) {
            const entry = _sheets[id];
            if (!entry) return;
            delete _sheets[id];

            if (entry.element) { entry.element.remove(); return; }

            const adopted = ((document as any).adoptedStyleSheets ?? []) as any[];
            (document as any).adoptedStyleSheets = adopted.filter(sheet => sheet !== entry.sheet);
        },

        // The Custom Highlight API: ranges the browser paints through ::highlight(name), without a
        // single element being added to the document. The reason it exists - wrapping matches in
        // <mark> mutates the DOM, which breaks a Blazor diff and any layout measured around it.
        highlightText(name: string, element: any, search: string, caseSensitive: boolean) {
            if (!element || !search) return 0;
            const HighlightCtor = (window as any).Highlight;
            const highlights = CSSNS()?.highlights;
            if (typeof HighlightCtor !== 'function' || !highlights) return -1;

            const needle = caseSensitive ? search : search.toLowerCase();
            const ranges: Range[] = [];
            const walker = document.createTreeWalker(element, NodeFilter.SHOW_TEXT);

            while (walker.nextNode()) {
                const node = walker.currentNode as Text;
                const haystack = caseSensitive ? node.data : node.data.toLowerCase();

                let index = haystack.indexOf(needle);
                while (index >= 0) {
                    const range = document.createRange();
                    range.setStart(node, index);
                    range.setEnd(node, index + needle.length);
                    ranges.push(range);
                    index = haystack.indexOf(needle, index + needle.length);
                }
            }

            highlights.set(name, new HighlightCtor(...ranges));
            return ranges.length;
        },

        clearHighlight(name: string) {
            CSSNS()?.highlights?.delete(name);
        },

        // -- The per-element half of the Typed OM ------------------------------------------------

        computedValue(element: HTMLElement, property: string) {
            const map = (element as any)?.computedStyleMap?.();
            if (!map) return null;
            try { return toValue(map.get(property)); } catch { return null; }
        },

        computedProperties(element: HTMLElement) {
            const map = (element as any)?.computedStyleMap?.();
            if (!map) return [];
            // A StylePropertyMapReadOnly is iterable as [property, values]; only the names are worth
            // flattening here, since the full computed set is hundreds of entries.
            return Array.from(map, ([property]: any) => property);
        },

        styleValue(element: HTMLElement, property: string) {
            const map = (element as any)?.attributeStyleMap;
            if (!map) return null;
            try { return toValue(map.get(property)); } catch { return null; }
        },

        setStyleValue(element: HTMLElement, property: string, value: number, unit: string) {
            const map = (element as any)?.attributeStyleMap;
            const css = CSSNS();
            if (!map || !css) return false;
            try {
                // A typed number goes in as a CSSUnitValue, which is the point of the API: no string
                // parsing on the way in, no string parsing on the way back out.
                const factory = unit ? css[unit] : css.number;
                map.set(property, typeof factory === 'function' ? factory.call(css, value) : value);
                return true;
            } catch {
                return false;
            }
        },

        setStyleText(element: HTMLElement, property: string, value: string) {
            const map = (element as any)?.attributeStyleMap;
            if (!map) return false;
            try {
                map.set(property, value);
                return true;
            } catch {
                return false;
            }
        },

        deleteStyleValue(element: HTMLElement, property: string) {
            const map = (element as any)?.attributeStyleMap;
            if (!map) return false;
            try {
                map.delete(property);
                return true;
            } catch {
                return false;
            }
        },

        clearStyleValues(element: HTMLElement) {
            const map = (element as any)?.attributeStyleMap;
            if (!map) return false;
            map.clear();
            return true;
        },

        styleProperties(element: HTMLElement) {
            const map = (element as any)?.attributeStyleMap;
            if (!map) return [];
            return Array.from(map, ([property]: any) => property);
        },

        disposeAll() {
            for (const id of Object.keys(_sheets)) butil.css.removeSheet(id);
        }
    };
}(BitButil));
