var BitButil = (window as any).BitButil = (window as any).BitButil || {};

(function (butil: any) {
    // Custom element states live on ElementInternals, and internals can only be attached from
    // inside a custom element's own constructor. So Butil defines the element for you: a minimal
    // class that does nothing but attach its internals and remember them here.
    const _internals = new WeakMap<HTMLElement, any>();

    butil.customElements = {
        isSupported() {
            return typeof (window as any).ElementInternals !== 'undefined'
                && 'states' in ((window as any).ElementInternals?.prototype ?? {});
        },
        isDefined(tagName: string) { return !!window.customElements?.get(tagName); },
        define(tagName: string) {
            if (!window.customElements || typeof (window as any).ElementInternals === 'undefined') return false;
            // A definition is permanent and cannot be replaced, so re-registering is a no-op rather
            // than an error - a component that mounts twice must not throw the second time.
            if (window.customElements.get(tagName)) return true;

            try {
                window.customElements.define(tagName, class extends HTMLElement {
                    constructor() {
                        super();
                        try { _internals.set(this, (this as any).attachInternals()); }
                        catch { /* runtime without ElementInternals */ }
                    }
                });
                return true;
            } catch {
                // An invalid name (no dash), or a name already taken by another script.
                return false;
            }
        },
        addState(element: HTMLElement, state: string) {
            const states = _internals.get(element)?.states;
            if (!states) return false;
            try {
                // Matched in CSS as :state(<name>) - the supported way for a component to expose its
                // own state without inventing a class or a data attribute that page CSS can clash with.
                states.add(state);
                return true;
            } catch {
                return false;
            }
        },
        deleteState(element: HTMLElement, state: string) {
            const states = _internals.get(element)?.states;
            if (!states) return false;
            return states.delete(state);
        },
        hasState(element: HTMLElement, state: string) {
            const states = _internals.get(element)?.states;
            return states ? states.has(state) : false;
        },
        getStates(element: HTMLElement) {
            const states = _internals.get(element)?.states;
            return states ? Array.from(states) : [];
        },
        clearStates(element: HTMLElement) {
            const states = _internals.get(element)?.states;
            if (!states) return false;
            states.clear();
            return true;
        },
        setAria(element: HTMLElement, property: string, value: string | null) {
            const internals = _internals.get(element);
            if (!internals) return false;
            try {
                // ARIA set through internals is a *default*: an author attribute on the element still
                // wins, which is what makes it safe for a component to state its own semantics.
                internals[property] = value;
                return true;
            } catch {
                return false;
            }
        },
        hasInternals(element: HTMLElement) { return _internals.has(element); }
    };
}(BitButil));
