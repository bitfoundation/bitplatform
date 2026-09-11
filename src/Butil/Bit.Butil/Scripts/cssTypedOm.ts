var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    const CSSNS = () => (window as any).CSS;

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

    // The Typed OM: reading and writing an element's styles as values rather than as strings. It
    // ships separately from getComputedStyle in the engines, and it is used by code that is doing
    // something quite different from the rest of this API - so it is its own module too.
    butil.cssTypedOm = {
        // The Typed OM's unit factories (CSS.px and friends) are what the style-map half of this
        // module needs, and they ship separately from getComputedStyle - hence its own probe.
        isTypedOmAvailable() { return typeof CSSNS()?.px === 'function'; },

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
    };
}(BitButil));
