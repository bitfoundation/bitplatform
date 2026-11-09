// Bit.Bheadless - strict Content-Security-Policy style bridge.
//
// A Content-Security-Policy whose style-src has no 'unsafe-inline' refuses every style attribute: the ones in the
// markup and the ones set with setAttribute('style', …) alike. It does not restrict the CSSOM
// (element.style.setProperty), so with BitBhOptions.StrictCsp on, the components put the styles they need in
// data-bit-bh-style instead and this script applies them through the CSSOM, and keeps them applied as the attribute
// changes, appears and goes away.
//
// It is a classic script with no imports or exports, so it loads both ways:
//   - from the page's <head>: <script src="_content/Bit.Bheadless/bit-bheadless-csp.js"></script> - it then applies
//     the styles as the parser inserts the elements, before the prerendered page is first painted;
//   - imported by the runtime (styles.enable), once a page shows it needs it.
// Both copies share one bridge, kept on globalThis.
//
// Nothing in the markup tells the components' attributes from others: every [data-bit-bh-style] on the page is applied,
// so markup that is not the application's own (user content rendered as a MarkupString) must not carry the attribute.
// A sanitizer that cleans such markup has to remove data-bit-bh-style as it removes style.

interface BitBhStyleBridge {
    /** Applies the declarations of [data-bit-bh-style] on the element (and drops the ones it no longer lists). */
    apply(el: Element): void;
    /** Applies every [data-bit-bh-style] in the subtree. */
    scan(root: ParentNode): void;
    /** Takes the styles off the element while revealed (a focused visually hidden skip link), and back on. */
    reveal(el: Element, revealed: boolean): void;
    /** Applies the changes the observer has queued but not yet delivered. */
    flush(): void;
}

(() => {
    const host = globalThis as { __bitBheadlessStyles?: BitBhStyleBridge };
    if (host.__bitBheadlessStyles || typeof document === 'undefined' || typeof MutationObserver === 'undefined') return;

    const ATTRIBUTE = 'data-bit-bh-style';
    const SELECTOR = `[${ATTRIBUTE}]`;
    // The runtime positions an anchored surface from its first placement until it stops ([data-anchor] meanwhile): the
    // properties it sets then are its own (ANCHOR_STYLE_PROPS in bit-bheadless.ts, as the CSSOM lists them).
    const ANCHORED = 'data-anchor';
    const POSITIONING = new Set(['position', 'left', 'top', 'margin-top', 'margin-right', 'margin-bottom', 'margin-left', 'visibility', 'min-width']);

    type Declarations = Map<string, { value: string; priority: string }>;
    interface Applied {
        source: string;
        /** What the source declares, parsed. */
        declared: Declarations;
        /** What is on the element now: the declared ones, or none while revealed. */
        active: Declarations;
    }

    const NONE: Declarations = new Map();
    const applied = new WeakMap<Element, Applied>();
    const revealed = new WeakSet<Element>();
    let parser: CSSStyleDeclaration | null = null;

    /**
     * The longhand declarations of a declaration list, through a detached element's CSSOM - which also expands the
     * shorthands (inset, flex, overflow …) into the longhands an element's style reports.
     */
    function parse(source: string): Declarations {
        parser ??= document.createElement('div').style;
        parser.cssText = source;
        const declarations: Declarations = new Map();
        for (let i = 0; i < parser.length; i++) {
            const name = parser.item(i);
            declarations.set(name, { value: parser.getPropertyValue(name), priority: parser.getPropertyPriority(name) });
        }
        parser.cssText = '';
        return declarations;
    }

    function styleOf(el: Element) {
        return (el as HTMLElement).style as CSSStyleDeclaration | undefined;
    }

    function apply(el: Element) {
        const style = styleOf(el);
        if (!style) return;

        const source = el.getAttribute(ATTRIBUTE);
        const previous = applied.get(el);
        const declared = source === null ? NONE : previous && previous.source === source ? previous.declared : parse(source);
        const active = revealed.has(el) ? NONE : declared;
        // Re-asserted on every change of the style the runtime makes as it moves the surface, they would undo it.
        const anchored = el.hasAttribute(ANCHORED);

        // Off the element: what it set before and no longer declares - unless something else (the runtime, the
        // consumer's script) changed that property since, which then is theirs.
        if (previous) {
            for (const [name, { value }] of previous.active) {
                if (anchored && POSITIONING.has(name)) continue;
                if (!active.has(name) && style.getPropertyValue(name) === value) style.removeProperty(name);
            }
        }

        // On: only what differs, so a re-assertion after an unrelated style change writes nothing.
        for (const [name, { value, priority }] of active) {
            if (anchored && POSITIONING.has(name)) continue;
            if (style.getPropertyValue(name) !== value || style.getPropertyPriority(name) !== priority) {
                style.setProperty(name, value, priority);
            }
        }

        if (source === null) applied.delete(el);
        else applied.set(el, { source, declared, active });
    }

    function scan(root: ParentNode) {
        if (root instanceof Element && root.hasAttribute(ATTRIBUTE)) apply(root);
        root.querySelectorAll(SELECTOR).forEach(apply);
    }

    function onMutations(records: MutationRecord[]) {
        for (const r of records) {
            if (r.type === 'attributes') {
                const el = r.target as Element;
                // data-bit-bh-style changed (or went away), or the style attribute was replaced - by the renderer
                // updating a consumer's style without a policy in the way - and took the declarations with it.
                if (r.attributeName === ATTRIBUTE || el.hasAttribute(ATTRIBUTE)) apply(el);
            } else {
                r.addedNodes.forEach((node) => {
                    if (node.nodeType === 1) scan(node as Element);
                });
            }
        }
    }

    const observer = new MutationObserver(onMutations);

    function flush() {
        const pending = observer.takeRecords();
        if (pending.length) onMutations(pending);
    }

    function reveal(el: Element, on: boolean) {
        if (on) revealed.add(el);
        else revealed.delete(el);
        apply(el);
    }

    host.__bitBheadlessStyles = { apply, scan, reveal, flush };

    observer.observe(document.documentElement, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeFilter: [ATTRIBUTE, 'style'],
    });
    scan(document);
})();
