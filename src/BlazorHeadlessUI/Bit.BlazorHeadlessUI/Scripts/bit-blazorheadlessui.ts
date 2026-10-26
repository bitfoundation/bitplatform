// Bit.BlazorHeadlessUI - JS runtime.
//
// The C# side imports this module on first use (BitBhInterop) and the JS
// initializer (Bit.BlazorHeadlessUI.lib.module.js) imports it at startup to install
// the global, declarative behaviours (key handling, interaction attributes).
//
// Every exported namespace is a small, self-contained primitive:
//
//   keys        - declarative preventDefault for keys listed in [data-bit-bh-keys]
//   interaction - data-hover / data-focus(-visible) / data-active on [data-bit-bh-interactive]
//   anchor      - floating element positioning (flip, shift, size, arrow, hide)
//   layers      - dismissable layer stack (Escape, pointer-down outside, focus outside)
//   focus       - focus scopes (trap / loop / restore), tabbable discovery, helpers
//   modal       - hide-others (inert + aria-hidden) and stack-aware scroll lock
//   portal      - DOM teleportation that keeps Blazor's component tree intact
//   floating    - orchestrates the above for popovers, menus, listboxes, dialogs
//   presence    - wait for CSS exit animations / transitions
//   collection  - document-order resolution for registered collection items
//   announcer   - polite / assertive live-region announcements
//   transition  - class-based enter / leave transitions
//   sortable    - pointer-driven drag & drop reordering
//   slider      - pointer-driven slider thumbs
//   dom         - misc helpers (scrollIntoView, scroll to index, element rects)
//
// All of it lives in one runtime object kept on globalThis: the module can be
// evaluated twice (the initializer's import and the interop's import resolve the
// same URL in every normal setup, but a custom bundler or base href can make them
// differ), and two copies with their own stacks would each see only half of the
// open layers, scopes and scroll locks.

interface DotNetRef {
    invokeMethodAsync<T = unknown>(methodIdentifier: string, ...args: unknown[]): Promise<T>;
}

type ElementLike = string | Element | null | undefined;

interface Point { x: number; y: number; }

interface AnchorOptions {
    to?: string;
    gap?: number;
    offset?: number;
    padding?: number;
    flip?: boolean;
    shift?: boolean;
    matchWidth?: boolean;
    hideWhenDetached?: boolean;
    arrowId?: string;
    arrowPadding?: number;
}

interface AnchorState {
    reference: Element | string | Point;
    floating: HTMLElement;
    options: AnchorOptions;
    frame: number;
    update?: () => void;
    observer?: ResizeObserver;
    saved: Record<string, string>;
}

interface LayerOptions {
    escape?: boolean;
    outside?: boolean;
    focusOutside?: boolean;
    modal?: boolean;
    hover?: boolean;
    parent?: number;
    parentFloating?: number;
    ids?: string[];
}

interface Layer {
    id: number;
    dotnet: DotNetRef | null;
    ids: string[];
    options: LayerOptions;
    parent: number;
    dismissed?: boolean;
}

interface ScopeOptions {
    trap?: boolean;
    loop?: boolean;
    autoFocus?: string | Element;
    restore?: boolean;
    returnFocusId?: string | Element | null;
    layer?: number;
}

interface Scope {
    id: number;
    containerId: string;
    trap: boolean;
    loop: boolean;
    restore: boolean;
    returnFocusId: string | Element | null;
    previous: Element | null;
    layer: number;
    lastFocused: Element | null;
}

interface FloatingOptions {
    portalIds?: string[];
    portal?: true | string;
    reference?: string | Point;
    anchor?: AnchorOptions;
    layer?: LayerOptions;
    modal?: { hideOthers?: boolean; scrollLock?: boolean };
    focus?: ScopeOptions;
}

interface FloatingState {
    id: number;
    contentId: string;
    opts: FloatingOptions;
    dotnet: DotNetRef | null;
    surfaceIds: string[];
    portals: number[];
    anchor?: number;
    layer?: number;
    hidden?: Element[];
    scrollLocked?: boolean;
    scope?: number;
}

interface TransitionClasses {
    enter?: string;
    enterFrom?: string;
    enterTo?: string;
    entered?: string;
    leave?: string;
    leaveFrom?: string;
    leaveTo?: string;
}

function createRuntime() {
    const byId = (idOrEl: ElementLike): HTMLElement | null =>
        (typeof idOrEl === 'string' ? document.getElementById(idOrEl) : (idOrEl as HTMLElement) || null);
    const isElement = (n: unknown): n is HTMLElement => !!n && (n as Node).nodeType === 1;

    // An IME composition's keys belong to the composition: a Space or an Enter that commits a
    // candidate (or the legacy 229 key code some engines still send) is never a component's key.
    const isComposing = (e: KeyboardEvent) => e.isComposing || e.keyCode === 229;

    // ─── keys ────────────────────────────────────────────────────────────────────
    //
    // Blazor cannot conditionally call preventDefault() based on which key was
    // pressed. Components declare the keys they own with a space-separated list:
    //
    //     <button data-bit-bh-keys="ArrowDown ArrowUp Enter Space">
    //
    // Tokens may carry modifiers ("Ctrl+a", "Shift+Tab"). A token without Shift
    // also matches the shifted key (so "ArrowDown" covers Shift+ArrowDown), while
    // Ctrl / Meta / Alt must be listed explicitly.

    function keyName(e: KeyboardEvent) {
        if (e.key === ' ' || e.key === 'Spacebar') return 'Space';
        return e.key;
    }

    function matchesKeyToken(e: KeyboardEvent, token: string) {
        // Autofill and some virtual keyboards dispatch keydown events without a key.
        if (!e.key) return false;
        const parts = token.split('+');
        const key = parts.pop();
        const mods = new Set(parts.map((p) => p.toLowerCase()));
        const name = keyName(e);
        if (key.length === 1 ? name.toLowerCase() !== key.toLowerCase() : name !== key) return false;
        if (e.ctrlKey !== mods.has('ctrl')) return false;
        if (e.metaKey !== mods.has('meta')) return false;
        if (e.altKey !== mods.has('alt')) return false;
        if (mods.has('shift') && !e.shiftKey) return false;
        return true;
    }

    function onGlobalKeyDown(e: KeyboardEvent) {
        const target = e.target;
        if (!isElement(target) || isComposing(e)) return;
        const spec = target.getAttribute('data-bit-bh-keys');
        if (!spec) return;
        for (const token of spec.split(/\s+/)) {
            if (token && matchesKeyToken(e, token)) {
                e.preventDefault();
                return;
            }
        }
    }

    const keys = {
        matches: matchesKeyToken,
    };

    // ─── interaction ─────────────────────────────────────────────────────────────
    //
    // Tracks the last input modality (keyboard vs pointer) and maintains
    // data-hover / data-active / data-focus / data-focus-visible on any element
    // marked [data-bit-bh-interactive]. Running in JS keeps hover feedback instant and
    // avoids a server round-trip per pointer movement in Blazor Server.

    let lastModality = 'keyboard';

    function interactiveRoot(node: EventTarget | null): HTMLElement | null {
        return isElement(node) ? node.closest<HTMLElement>('[data-bit-bh-interactive]') : null;
    }

    function isDisabledInteractive(el: HTMLElement) {
        return el.hasAttribute('data-disabled') || el.getAttribute('aria-disabled') === 'true' || (el as HTMLButtonElement).disabled === true;
    }

    function onPointerOverInteractive(e: PointerEvent) {
        if (e.pointerType === 'touch') return;
        const el = interactiveRoot(e.target);
        if (el && !isDisabledInteractive(el)) el.setAttribute('data-hover', '');
    }

    function onPointerOutInteractive(e: PointerEvent) {
        const el = interactiveRoot(e.target);
        if (!el) return;
        if (e.relatedTarget && el.contains(e.relatedTarget as Node)) return;
        el.removeAttribute('data-hover');
        el.removeAttribute('data-active');
    }

    function onPointerDownInteractive(e: PointerEvent) {
        lastModality = 'pointer';
        const el = interactiveRoot(e.target);
        if (el && !isDisabledInteractive(el)) el.setAttribute('data-active', '');
    }

    function clearActive() {
        for (const el of Array.from(document.querySelectorAll('[data-bit-bh-interactive][data-active]'))) el.removeAttribute('data-active');
    }

    function onFocusInInteractive(e: FocusEvent) {
        const el = interactiveRoot(e.target);
        if (!el || isDisabledInteractive(el)) return;
        el.setAttribute('data-focus', '');
        let visible = lastModality === 'keyboard';
        try { visible = (e.target as Element).matches(':focus-visible'); } catch { /* old browsers */ }
        if (visible) el.setAttribute('data-focus-visible', '');
    }

    function onFocusOutInteractive(e: FocusEvent) {
        const el = interactiveRoot(e.target);
        if (!el) return;
        if (e.relatedTarget && el.contains(e.relatedTarget as Node)) return;
        el.removeAttribute('data-focus');
        el.removeAttribute('data-focus-visible');
    }

    const interaction = {
        get modality() { return lastModality; },
    };

    // ─── install ─────────────────────────────────────────────────────────────────

    let installed = false;

    /** Installs the global listeners. Idempotent; called by the JS initializer and on first import. */
    function install() {
        if (installed || typeof document === 'undefined') return;
        installed = true;

        document.addEventListener('keydown', (e) => {
            if (!e.metaKey && !e.altKey && !e.ctrlKey) lastModality = 'keyboard';
            onGlobalKeyDown(e);
        }, true);
        document.addEventListener('keydown', onLayerKeyDownCapture, true);
        document.addEventListener('keydown', onLayerKeyDown, false);
        document.addEventListener('pointerdown', onLayerPointerDown, true);
        document.addEventListener('click', onLayerClick, true);
        document.addEventListener('focusin', onLayerFocusIn, true);

        document.addEventListener('pointerover', onPointerOverInteractive, true);
        document.addEventListener('pointerout', onPointerOutInteractive, true);
        document.addEventListener('pointerdown', onPointerDownInteractive, true);
        document.addEventListener('pointerup', clearActive, true);
        document.addEventListener('pointercancel', clearActive, true);
        document.addEventListener('dragend', clearActive, true);
        document.addEventListener('focusin', onFocusInInteractive, true);
        document.addEventListener('focusout', onFocusOutInteractive, true);

        installPresence();

        // Create the shared live regions up front: screen readers often ignore a region
        // that is inserted at the same moment as its first message.
        if (document.body) liveRegion('polite');
        else document.addEventListener('DOMContentLoaded', () => liveRegion('polite'), { once: true });

        // <BitBhVisuallyHidden Focusable>: reveal while keyboard focus is inside (skip links).
        document.addEventListener('focusin', (e) => {
            const el = isElement(e.target) ? e.target.closest('[data-bit-bh-visually-hidden="focusable"]') : null;
            if (el && el.hasAttribute('style')) {
                el.setAttribute('data-bit-bh-clip', el.getAttribute('style'));
                el.removeAttribute('style');
            }
        }, true);
        document.addEventListener('focusout', (e) => {
            const el = isElement(e.target) ? e.target.closest('[data-bit-bh-visually-hidden="focusable"]') : null;
            if (!el || (e.relatedTarget && el.contains(e.relatedTarget as Node))) return;
            const clip = el.getAttribute('data-bit-bh-clip');
            if (clip !== null) {
                el.setAttribute('style', clip);
                el.removeAttribute('data-bit-bh-clip');
            }
        }, true);

        // hidden="until-found" panels: open them (by clicking their trigger) when
        // the browser's find-in-page reveals a match inside.
        document.addEventListener('beforematch', (e) => {
            const panel = isElement(e.target) ? e.target.closest('[data-bit-bh-trigger]') : null;
            const trigger = panel && document.getElementById(panel.getAttribute('data-bit-bh-trigger'));
            if (trigger && trigger.getAttribute('aria-expanded') !== 'true') trigger.click();
        }, true);
    }

    /**
     * Enhanced navigation replaces the body's content with the server's, which drops the
     * elements this runtime appended to it. Recreate the live regions right away, for the
     * same reason they are created up front.
     */
    function onEnhancedLoad() {
        liveRegion('polite');
    }

    // ─── anchor ──────────────────────────────────────────────────────────────────
    //
    // A self-contained positioning engine in the spirit of Floating UI's
    // computePosition + flip + shift + size + arrow + hide middleware. Uses
    // position:fixed and compensates for transformed ancestors (which create a new
    // containing block for fixed elements).
    //
    // Everything is measured untransformed: an enter animation usually scales or
    // translates the panel, and getBoundingClientRect() would bake its first frame
    // into the position - a ResizeObserver does not see the animation end.

    const anchorHandles = new Map<number, AnchorState>();
    let anchorSeq = 0;

    const OPPOSITE: Record<string, string> = { top: 'bottom', bottom: 'top', left: 'right', right: 'left' };
    const ARROW_ROTATION: Record<string, number> = { top: 0, right: 90, bottom: 180, left: -90 };
    const ANCHOR_STYLE_PROPS = ['position', 'left', 'top', 'margin', 'visibility', 'minWidth'] as const;
    const ANCHOR_VARIABLES = ['--bit-bh-anchor-width', '--bit-bh-anchor-height', '--button-width', '--anchor-gap', '--anchor-offset',
        '--anchor-padding', '--bit-bh-available-height', '--bit-bh-available-width', '--bit-bh-transform-origin'];

    function parsePlacement(to: string | undefined) {
        const parts = String(to || 'bottom').trim().toLowerCase().split(/\s+/);
        let side = parts[0] || 'bottom';
        let align = parts[1] || 'center';
        if (!OPPOSITE[side]) side = 'bottom';
        if (align !== 'start' && align !== 'end') align = 'center';
        return { side, align };
    }

    function viewportRect() {
        const vv = window.visualViewport;
        const width = vv ? vv.width : (window.innerWidth || document.documentElement.clientWidth);
        const height = vv ? vv.height : (window.innerHeight || document.documentElement.clientHeight);
        return { left: 0, top: 0, right: width, bottom: height, width, height };
    }

    function resolveReferenceRect(reference: Element | string | Point) {
        if (!reference) return null;
        if (typeof (reference as Point).x === 'number' && typeof (reference as Point).y === 'number' && !isElement(reference)) {
            const p = reference as Point;
            return { left: p.x, top: p.y, right: p.x, bottom: p.y, width: 0, height: 0 };
        }
        const el = byId(reference as ElementLike);
        return el ? el.getBoundingClientRect() : null;
    }

    function isRtl(el: Element) {
        try { return getComputedStyle(el).direction === 'rtl'; } catch { return false; }
    }

    /** The element's layout size, which - unlike its bounding rect - ignores its own and its ancestors' transforms. */
    function layoutSize(el: Element) {
        if (el instanceof HTMLElement) return { width: el.offsetWidth, height: el.offsetHeight };
        const cs = getComputedStyle(el);
        const width = parseFloat(cs.width), height = parseFloat(cs.height);
        if (!isNaN(width) && !isNaN(height)) return { width, height };
        const r = el.getBoundingClientRect();
        return { width: r.width, height: r.height };
    }

    /**
     * Where (0, 0) of a fixed element inside `floating`'s parent lands in the viewport:
     * a transformed ancestor makes itself the containing block of fixed elements.
     * Measured with an untransformed probe, never with the (possibly animating) panel.
     */
    function containingBlockOrigin(floating: HTMLElement) {
        const parent = floating.parentNode;
        if (!parent) return { left: 0, top: 0 };
        const probe = document.createElement('div');
        probe.style.cssText = 'position:fixed;left:0;top:0;width:0;height:0;margin:0;padding:0;border:0;visibility:hidden;pointer-events:none';
        parent.insertBefore(probe, floating);
        const r = probe.getBoundingClientRect();
        probe.remove();
        return { left: r.left, top: r.top };
    }

    function coordsFor(ref: { left: number; top: number; right: number; bottom: number; width: number; height: number },
        w: number, h: number, side: string, align: string, gap: number, alignOffset: number, rtl: boolean) {
        let x = 0, y = 0;
        const vertical = side === 'top' || side === 'bottom';
        if (vertical) {
            y = side === 'bottom' ? ref.bottom + gap : ref.top - gap - h;
            const startX = rtl ? ref.right - w : ref.left;
            const endX = rtl ? ref.left : ref.right - w;
            x = align === 'start' ? startX : align === 'end' ? endX : ref.left + (ref.width - w) / 2;
            x += rtl && align !== 'center' ? -alignOffset : alignOffset;
        } else {
            x = side === 'right' ? ref.right + gap : ref.left - gap - w;
            y = align === 'start' ? ref.top : align === 'end' ? ref.bottom - h : ref.top + (ref.height - h) / 2;
            y += alignOffset;
        }
        return { x, y };
    }

    function mainAxisOverflow(side: string, pos: Point, w: number, h: number, vp: ReturnType<typeof viewportRect>, padding: number) {
        switch (side) {
            case 'bottom': return pos.y + h - (vp.bottom - padding);
            case 'top': return padding - pos.y;
            case 'right': return pos.x + w - (vp.right - padding);
            default: return padding - pos.x;
        }
    }

    function computePlacement(state: AnchorState) {
        const { floating, options } = state;
        const ref = resolveReferenceRect(state.reference);
        if (!ref) return null;

        const vp = viewportRect();
        const padding = options.padding ?? 8;
        const gap = options.gap ?? 0;
        const alignOffset = options.offset ?? 0;
        const rtl = isRtl(floating);
        const { side: preferredSide, align } = parsePlacement(options.to);

        const { width: w, height: h } = layoutSize(floating);

        let side = preferredSide;
        let pos = coordsFor(ref, w, h, side, align, gap, alignOffset, rtl);

        if (options.flip !== false) {
            const overflow = mainAxisOverflow(side, pos, w, h, vp, padding);
            if (overflow > 0) {
                const alt = OPPOSITE[side];
                const altPos = coordsFor(ref, w, h, alt, align, gap, alignOffset, rtl);
                const altOverflow = mainAxisOverflow(alt, altPos, w, h, vp, padding);
                if (altOverflow < overflow) { side = alt; pos = altPos; }
            }
        }

        if (options.shift !== false) {
            if (side === 'top' || side === 'bottom') {
                pos.x = Math.max(vp.left + padding, Math.min(pos.x, vp.right - padding - w));
            } else {
                pos.y = Math.max(vp.top + padding, Math.min(pos.y, vp.bottom - padding - h));
            }
        }

        // Space left on the chosen side, exposed for max-height / max-width rules.
        const availableHeight = side === 'bottom' ? vp.bottom - padding - ref.bottom - gap
            : side === 'top' ? ref.top - gap - padding
                : vp.height - padding * 2;
        const availableWidth = side === 'right' ? vp.right - padding - ref.right - gap
            : side === 'left' ? ref.left - gap - padding
                : vp.width - padding * 2;

        const referenceHidden = ref.bottom < vp.top || ref.top > vp.bottom || ref.right < vp.left || ref.left > vp.right;

        return { ref, side, align, x: pos.x, y: pos.y, w, h, availableHeight, availableWidth, referenceHidden };
    }

    function applyArrow(state: AnchorState, p: ReturnType<typeof computePlacement>) {
        const arrow = state.options.arrowId ? document.getElementById(state.options.arrowId) : null;
        if (!arrow) return null;
        const arrowSize = layoutSize(arrow);
        const arrowPadding = state.options.arrowPadding ?? 0;
        const vertical = p.side === 'top' || p.side === 'bottom';

        arrow.style.position = 'absolute';
        arrow.style.left = '';
        arrow.style.top = '';
        arrow.style.right = '';
        arrow.style.bottom = '';
        arrow.setAttribute('data-side', p.side);

        let offset: number;
        if (vertical) {
            const center = p.ref.left + p.ref.width / 2 - p.x - arrowSize.width / 2;
            offset = Math.max(arrowPadding, Math.min(center, p.w - arrowSize.width - arrowPadding));
            arrow.style.left = Math.round(offset) + 'px';
            if (p.side === 'bottom') arrow.style.bottom = '100%';
            else arrow.style.top = '100%';
        } else {
            const center = p.ref.top + p.ref.height / 2 - p.y - arrowSize.height / 2;
            offset = Math.max(arrowPadding, Math.min(center, p.h - arrowSize.height - arrowPadding));
            arrow.style.top = Math.round(offset) + 'px';
            if (p.side === 'right') arrow.style.right = '100%';
            else arrow.style.left = '100%';
        }
        // Rotate the default (downward pointing) arrow to face the reference.
        arrow.style.transform = `rotate(${ARROW_ROTATION[p.side]}deg)`;
        arrow.style.transformOrigin = 'center';
        return offset + (vertical ? arrowSize.width / 2 : arrowSize.height / 2);
    }

    function transformOrigin(p: ReturnType<typeof computePlacement>, arrowCenter: number | null) {
        const cross = arrowCenter != null ? `${Math.round(arrowCenter)}px` : 'center';
        switch (p.side) {
            case 'bottom': return `${cross} top`;
            case 'top': return `${cross} bottom`;
            case 'right': return `left ${cross}`;
            default: return `right ${cross}`;
        }
    }

    function applyPosition(state: AnchorState) {
        const { floating, options } = state;
        const refRect = resolveReferenceRect(state.reference);
        if (refRect) {
            floating.style.setProperty('--bit-bh-anchor-width', refRect.width + 'px');
            floating.style.setProperty('--bit-bh-anchor-height', refRect.height + 'px');
            // Headless UI compatible alias.
            floating.style.setProperty('--button-width', refRect.width + 'px');
        }
        floating.style.setProperty('--anchor-gap', (options.gap ?? 0) + 'px');
        floating.style.setProperty('--anchor-offset', (options.offset ?? 0) + 'px');
        floating.style.setProperty('--anchor-padding', (options.padding ?? 8) + 'px');
        if (options.matchWidth && refRect) floating.style.minWidth = refRect.width + 'px';

        floating.style.position = 'fixed';
        floating.style.margin = '0';
        const origin = containingBlockOrigin(floating);

        const p = computePlacement(state);
        if (!p) return;

        floating.style.left = Math.round(p.x - origin.left) + 'px';
        floating.style.top = Math.round(p.y - origin.top) + 'px';
        floating.style.setProperty('--bit-bh-available-height', Math.max(0, Math.floor(p.availableHeight)) + 'px');
        floating.style.setProperty('--bit-bh-available-width', Math.max(0, Math.floor(p.availableWidth)) + 'px');

        const arrowCenter = applyArrow(state, p);
        floating.style.setProperty('--bit-bh-transform-origin', transformOrigin(p, arrowCenter));

        floating.setAttribute('data-side', p.side);
        floating.setAttribute('data-align', p.align);
        floating.setAttribute('data-anchor', p.side + (p.align !== 'center' ? ' ' + p.align : ''));

        if (p.referenceHidden) floating.setAttribute('data-reference-hidden', '');
        else floating.removeAttribute('data-reference-hidden');
        if (options.hideWhenDetached) floating.style.visibility = p.referenceHidden ? 'hidden' : '';
    }

    const anchor = {
        /**
         * Starts positioning `floating` relative to `reference` (an element, an id, or
         * a virtual point {x, y}). Auto-updates on scroll, resize and size changes.
         */
        start(reference: Element | string | Point, floatingRef: ElementLike, options: AnchorOptions) {
            const floatingEl = byId(floatingRef);
            if (!floatingEl) return 0;
            if (typeof reference === 'string') reference = document.getElementById(reference) || reference;
            const id = ++anchorSeq;
            // The inline values the positioning overwrites are the consumer's (or Blazor's) until it stops.
            const saved: Record<string, string> = {};
            for (const prop of ANCHOR_STYLE_PROPS) saved[prop] = floatingEl.style[prop];
            const state: AnchorState = { reference, floating: floatingEl, options: options || {}, frame: 0, saved };

            const update = () => {
                if (state.frame) return;
                state.frame = requestAnimationFrame(() => {
                    state.frame = 0;
                    if (anchorHandles.has(id)) applyPosition(state);
                });
            };
            state.update = update;

            applyPosition(state);

            window.addEventListener('scroll', update, { capture: true, passive: true });
            window.addEventListener('resize', update, { passive: true });
            if (window.visualViewport) window.visualViewport.addEventListener('resize', update, { passive: true });
            if (typeof ResizeObserver !== 'undefined') {
                state.observer = new ResizeObserver(update);
                if (isElement(reference)) state.observer.observe(reference);
                state.observer.observe(floatingEl);
            }
            anchorHandles.set(id, state);
            return id;
        },

        stop(handle: number, reset?: boolean) {
            const state = anchorHandles.get(handle);
            if (!state) return;
            anchorHandles.delete(handle);
            if (state.frame) cancelAnimationFrame(state.frame);
            window.removeEventListener('scroll', state.update, { capture: true });
            window.removeEventListener('resize', state.update);
            if (window.visualViewport) window.visualViewport.removeEventListener('resize', state.update);
            state.observer?.disconnect();
            if (reset !== false) {
                const el = state.floating;
                for (const prop of ANCHOR_STYLE_PROPS) el.style[prop] = state.saved[prop] ?? '';
                for (const v of ANCHOR_VARIABLES) el.style.removeProperty(v);
                el.removeAttribute('data-anchor');
                el.removeAttribute('data-reference-hidden');
            }
        },

        update(handle: number, options?: AnchorOptions) {
            const state = anchorHandles.get(handle);
            if (!state) return;
            if (options) state.options = options;
            applyPosition(state);
        },

        /** Moves the virtual reference point of a running handle (context menus). */
        setPoint(handle: number, x: number, y: number) {
            const state = anchorHandles.get(handle);
            if (!state) return;
            state.reference = { x, y };
            applyPosition(state);
        },
    };

    // ─── layers ──────────────────────────────────────────────────────────────────
    //
    // A stack of dismissable layers. Each layer owns a set of elements (its
    // "branch": content, trigger/reference, extra ids and every descendant layer).
    //
    //  - Pointer-down outside a layer's branch dismisses it ("outside"). Touch
    //    input waits for the click so scrolling with a finger doesn't dismiss.
    //  - Focus moving outside the branch dismisses it when focusOutside is on.
    //  - Escape dismisses the top-most layer when focus is not inside any layer
    //    (focused content handles Escape itself, on the C# side).
    //  - A modal layer shields the layers below it from outside interaction.
    //
    // A dismissal is a request: .NET may decline it (OnDismissRequest cancels it,
    // DismissOnEscape is off ...). `dismissed` covers the request in flight and a
    // close .NET accepted, so a declined layer can be dismissed again by the next
    // outside press.

    const layerStack: Layer[] = [];
    let layerSeq = 0;

    function layerElements(layer: Layer) {
        const els: HTMLElement[] = [];
        for (const id of layer.ids) {
            const el = document.getElementById(id);
            if (el) els.push(el);
        }
        return els;
    }

    function isInLayerBranch(layer: Layer, node: EventTarget | null): boolean {
        if (!node) return false;
        if (isElement(node) && node.closest('[data-bit-bh-dismiss-ignore]')) return true;
        for (const el of layerElements(layer)) if (el.contains(node as Node)) return true;
        for (const child of layerStack) {
            if (child.parent === layer.id && isInLayerBranch(child, node)) return true;
        }
        return false;
    }

    function isInsideAnyLayer(node: EventTarget | null) {
        return layerStack.some((l) => isInLayerBranch(l, node));
    }

    function dismissLayer(layer: Layer, reason: string) {
        if (layer.dismissed) return;
        layer.dismissed = true;
        // Children go first so nested content never outlives its parent.
        for (const child of layerStack.filter((l) => l.parent === layer.id)) dismissLayer(child, reason);
        // .NET answers whether the surface is closing; until it is closed for real the layer
        // stays dismissed, so a second press never asks again for the same close.
        const request = layer.dotnet ? layer.dotnet.invokeMethodAsync<boolean>('OnLayerDismiss', reason) : Promise.resolve(false);
        request.then((closing) => { if (closing !== true) layer.dismissed = false; }, () => { layer.dismissed = false; });
    }

    let pendingTouchDismiss: EventTarget | null = null;

    function processOutside(target: EventTarget | null, reason: string) {
        for (let i = layerStack.length - 1; i >= 0; i--) {
            const layer = layerStack[i];
            if (!isInLayerBranch(layer, target) && layer.options[reason === 'focus-outside' ? 'focusOutside' : 'outside']) {
                dismissLayer(layer, reason);
            }
            if (layer.options.modal) break;
        }
    }

    function onLayerPointerDown(e: PointerEvent) {
        if (!layerStack.length) return;
        if (e.pointerType === 'touch') {
            pendingTouchDismiss = e.target;
            return;
        }
        pendingTouchDismiss = null;
        processOutside(e.target, 'outside');
    }

    function onLayerClick(e: MouseEvent) {
        if (pendingTouchDismiss && layerStack.length) {
            const target = pendingTouchDismiss;
            pendingTouchDismiss = null;
            if (target === e.target || (isElement(target) && target.contains(e.target as Node))) processOutside(target, 'outside');
        }
    }

    function onLayerFocusIn(e: FocusEvent) {
        if (!layerStack.length) return;
        processOutside(e.target, 'focus-outside');
    }

    // Hover-driven layers (tooltips, hover cards) must be dismissable with Escape
    // without moving the pointer or focus (WCAG 1.4.13), even when focus sits inside
    // another layer. They are handled first, in the capture phase.
    function onLayerKeyDownCapture(e: KeyboardEvent) {
        if (e.key !== 'Escape' || isComposing(e) || !layerStack.length) return;
        const top = layerStack[layerStack.length - 1];
        if (!top.options.hover || top.dismissed) return;
        e.preventDefault();
        e.stopPropagation();
        dismissLayer(top, 'escape');
    }

    function onLayerKeyDown(e: KeyboardEvent) {
        if (e.key !== 'Escape' || isComposing(e) || e.defaultPrevented || !layerStack.length) return;
        if (isInsideAnyLayer(e.target)) return; // focused content handles Escape itself
        const layer = layerStack[layerStack.length - 1];
        // A press while the top layer's request is in flight is that layer's too: passing it
        // down would close the layer below while the top one may still decline and stay open.
        if (layer.dismissed) { e.preventDefault(); return; }
        if (layer.options.escape !== false) {
            e.preventDefault();
            dismissLayer(layer, 'escape');
        }
    }

    const layers = {
        /**
         * Pushes a layer. ids: element ids that belong to the layer's branch.
         * options: { escape, outside, focusOutside, modal, parent }.
         */
        push(dotnet: DotNetRef | null, ids: string[], options: LayerOptions) {
            const id = ++layerSeq;
            layerStack.push({ id, dotnet, ids: (ids || []).filter(Boolean), options: options || {}, parent: options?.parent || 0 });
            return id;
        },
        remove(handle: number) {
            const i = layerStack.findIndex((l) => l.id === handle);
            if (i >= 0) layerStack.splice(i, 1);
        },
        setIds(handle: number, ids: string[]) {
            const layer = layerStack.find((l) => l.id === handle);
            if (layer) layer.ids = (ids || []).filter(Boolean);
        },
        isTop(handle: number) {
            return layerStack.length > 0 && layerStack[layerStack.length - 1].id === handle;
        },
        contains: isInsideAnyLayer,
    };

    // ─── focus ───────────────────────────────────────────────────────────────────

    const CANDIDATE_SELECTOR = [
        'a[href]', 'area[href]', 'button', 'input', 'select', 'textarea', 'iframe', 'audio[controls]', 'video[controls]',
        'summary', '[contenteditable]:not([contenteditable="false"])', '[tabindex]',
    ].join(',');

    function isVisible(el: HTMLElement) {
        if (!el.isConnected) return false;
        if (el.getClientRects().length === 0) return false;
        const style = getComputedStyle(el);
        return style.visibility !== 'hidden' && style.visibility !== 'collapse';
    }

    function isFocusable(el: Element): el is HTMLElement {
        if (!isElement(el)) return false;
        if ((el as HTMLButtonElement).disabled) return false;
        if (el.closest('[inert]')) return false;
        if (el.tagName === 'INPUT' && (el as HTMLInputElement).type === 'hidden') return false;
        const details = el.closest('details');
        if (details && !details.open && el.tagName !== 'SUMMARY') return false;
        return isVisible(el);
    }

    function isTabbable(el: Element): el is HTMLElement {
        if (!isFocusable(el)) return false;
        const tabindex = el.getAttribute('tabindex');
        if (tabindex !== null && parseInt(tabindex, 10) < 0) return false;
        if (el.tagName === 'INPUT') {
            const input = el as HTMLInputElement;
            if (input.type === 'radio' && input.name && !input.checked) {
                const scope: ParentNode = input.form || document;
                const group = Array.from(scope.querySelectorAll<HTMLInputElement>(`input[type="radio"][name="${CSS.escape(input.name)}"]`));
                if (group.some((r) => r.checked)) return false;
                return group[0] === input;
            }
        }
        return true;
    }

    function tabbables(container: Element | null): HTMLElement[] {
        if (!container) return [];
        const list = Array.from(container.querySelectorAll(CANDIDATE_SELECTOR)).filter(isTabbable);
        // Positive tabindex values come first (in order), then document order.
        const positive = list.filter((el) => el.tabIndex > 0).sort((a, b) => a.tabIndex - b.tabIndex);
        return positive.concat(list.filter((el) => el.tabIndex <= 0));
    }

    function focusElement(el: Element | null, options?: { preventScroll?: boolean; select?: boolean }) {
        if (!el || typeof (el as HTMLElement).focus !== 'function') return false;
        (el as HTMLElement).focus({ preventScroll: options?.preventScroll !== false });
        if (options?.select && el.tagName === 'INPUT' && typeof (el as HTMLInputElement).select === 'function') (el as HTMLInputElement).select();
        return document.activeElement === el;
    }

    const scopeStack: Scope[] = [];
    let scopeSeq = 0;

    function scopeContains(scope: Scope, node: EventTarget | null) {
        const container = byId(scope.containerId);
        if (container && container.contains(node as Node)) return true;
        // Focus inside a layer opened from within this scope (e.g. a listbox
        // popup portalled to <body>) is still considered inside.
        if (scope.layer) {
            const layer = layerStack.find((l) => l.id === scope.layer);
            if (layer && isInLayerBranch(layer, node)) return true;
        }
        return !!(isElement(node) && node.closest('[data-bit-bh-live-region]'));
    }

    // A scope without a trap does not lift the one below it: a plain focus scope inside a
    // dialog, or a non-modal popover opened from it, still leaves the dialog's trap in force.
    function topTrap() {
        for (let i = scopeStack.length - 1; i >= 0; i--) if (scopeStack[i].trap) return scopeStack[i];
        return null;
    }

    /** The scope Tab wraps in: the innermost one holding focus that loops, or else the innermost trap. */
    function tabScope(active: Element | null) {
        for (let i = scopeStack.length - 1; i >= 0; i--) {
            const scope = scopeStack[i];
            if (scope.trap) return scope;
            if (scope.loop && byId(scope.containerId)?.contains(active)) return scope;
        }
        return null;
    }

    function onScopeKeyDown(e: KeyboardEvent) {
        if (e.key !== 'Tab' || e.altKey || e.ctrlKey || e.metaKey) return;
        const active = document.activeElement;
        const scope = tabScope(active);
        if (!scope) return;
        const container = byId(scope.containerId);
        if (!container) return;
        const items = tabbables(container);
        if (!container.contains(active) && !scope.trap) return;
        if (items.length === 0) {
            if (scope.trap) { e.preventDefault(); focusElement(container); }
            return;
        }
        const first = items[0];
        const last = items[items.length - 1];
        if (e.shiftKey && (active === first || active === container || !container.contains(active))) {
            e.preventDefault();
            focusElement(last);
        } else if (!e.shiftKey && (active === last || !container.contains(active))) {
            e.preventDefault();
            focusElement(first);
        }
    }

    function onScopeFocusIn(e: FocusEvent) {
        const scope = topTrap();
        if (!scope) return;
        if (scopeContains(scope, e.target)) {
            scope.lastFocused = e.target as Element;
            return;
        }
        const container = byId(scope.containerId);
        if (!container) return;
        const fallback = scope.lastFocused && container.contains(scope.lastFocused) ? scope.lastFocused : tabbables(container)[0] || container;
        focusElement(fallback);
    }

    let scopeListenersInstalled = false;

    function ensureScopeListeners() {
        if (scopeListenersInstalled) return;
        scopeListenersInstalled = true;
        document.addEventListener('keydown', onScopeKeyDown, true);
        document.addEventListener('focusin', onScopeFocusIn, true);
    }

    const focus = {
        tabbables,
        isTabbable,

        /** Focuses the element with the given id. */
        focus(id: ElementLike, preventScroll?: boolean, select?: boolean) {
            return focusElement(byId(id), { preventScroll, select });
        },

        /** Focuses the first tabbable element inside the container (or the container). */
        focusFirst(id: ElementLike, preventScroll?: boolean) {
            const container = byId(id);
            if (!container) return false;
            return focusElement(tabbables(container)[0] || container, { preventScroll });
        },

        focusLast(id: ElementLike, preventScroll?: boolean) {
            const container = byId(id);
            if (!container) return false;
            const items = tabbables(container);
            return focusElement(items[items.length - 1] || container, { preventScroll });
        },

        /** Moves focus to the next / previous tabbable element after `id` in document order. */
        focusAdjacent(id: ElementLike, backwards?: boolean) {
            const from = byId(id);
            if (!from) return false;
            const all = tabbables(document.body);
            let index = all.indexOf(from);
            if (index < 0) {
                // `from` is not tabbable (e.g. tabindex=-1); find its position by document order.
                index = all.findIndex((el) => from.compareDocumentPosition(el) & Node.DOCUMENT_POSITION_FOLLOWING);
                index = index < 0 ? all.length : index;
                if (!backwards) index -= 1;
            }
            const next = all[index + (backwards ? -1 : 1)];
            return next ? focusElement(next) : false;
        },

        /** Returns true when the focused element is inside the element with the given id. */
        contains(id: ElementLike) {
            const el = byId(id);
            return !!(el && document.activeElement && el.contains(document.activeElement));
        },

        activeElementId() {
            return document.activeElement?.id || null;
        },

        /**
         * Creates a focus scope. options: { trap, loop, autoFocus: 'first' | 'container' | 'none' | <id>,
         * restore, returnFocusId, layer }. Returns a handle for destroyScope.
         */
        createScope(containerId: ElementLike, options: ScopeOptions) {
            ensureScopeListeners();
            options = options || {};
            const container = byId(containerId);
            if (!container) return 0;
            const id = ++scopeSeq;
            const scope: Scope = {
                id,
                containerId: container.id || (containerId as string),
                trap: !!options.trap,
                loop: !!options.loop,
                restore: options.restore !== false,
                returnFocusId: options.returnFocusId || null,
                previous: document.activeElement,
                layer: options.layer || 0,
                lastFocused: null,
            };
            scopeStack.push(scope);

            const autoFocus = options.autoFocus ?? 'first';
            if (autoFocus !== 'none' && !container.contains(document.activeElement)) {
                let target: Element | null = null;
                if (isElement(autoFocus)) target = autoFocus;
                else if (autoFocus === 'container') target = container;
                else if (autoFocus === 'first') target = container.querySelector('[data-bit-bh-autofocus]') || tabbables(container)[0] || container;
                else target = document.getElementById(autoFocus as string) || tabbables(container)[0] || container;
                focusElement(target, { select: target?.tagName === 'INPUT' });
            }
            return id;
        },

        /** Destroys a scope and (optionally) restores focus to where it was before. */
        destroyScope(handle: number, restore?: boolean) {
            const index = scopeStack.findIndex((s) => s.id === handle);
            if (index < 0) return;
            const [scope] = scopeStack.splice(index, 1);
            if (restore === false || !scope.restore) return;
            const container = byId(scope.containerId);
            const active = document.activeElement;
            // Only restore when focus would otherwise be lost (inside the closing
            // scope, or on <body>). Pointer users who clicked elsewhere keep focus.
            const lost = !active || active === document.body || (container && container.contains(active));
            if (!lost) return;
            const explicit = isElement(scope.returnFocusId)
                ? scope.returnFocusId
                : (scope.returnFocusId && document.getElementById(scope.returnFocusId as string));
            const target = explicit || scope.previous;
            if (target && target.isConnected) focusElement(target);
        },
    };

    // ─── modal ───────────────────────────────────────────────────────────────────

    const hiddenCounts = new WeakMap<Element, number>();
    const originalAria = new WeakMap<Element, { ariaHidden: string | null; inert: boolean }>();
    let scrollLockCount = 0;
    let scrollSnapshot: { overflow: string; paddingRight: string } | null = null;

    function hideElement(el: Element) {
        const count = hiddenCounts.get(el) || 0;
        if (count === 0) {
            originalAria.set(el, { ariaHidden: el.getAttribute('aria-hidden'), inert: el.hasAttribute('inert') });
            el.setAttribute('aria-hidden', 'true');
            el.setAttribute('inert', '');
        }
        hiddenCounts.set(el, count + 1);
    }

    function unhideElement(el: Element) {
        const count = hiddenCounts.get(el) || 0;
        if (count <= 1) {
            hiddenCounts.delete(el);
            const original = originalAria.get(el);
            originalAria.delete(el);
            if (!original || original.ariaHidden === null) el.removeAttribute('aria-hidden');
            else el.setAttribute('aria-hidden', original.ariaHidden);
            if (!original || !original.inert) el.removeAttribute('inert');
        } else {
            hiddenCounts.set(el, count - 1);
        }
    }

    const SKIP_TAGS = new Set(['SCRIPT', 'STYLE', 'TEMPLATE', 'LINK', 'META', 'NOSCRIPT']);
    // Blazor's own recovery UI must stay usable while a modal is open: the error bar's
    // Reload and the reconnect dialog's Retry are the only way out of a dead circuit.
    const SKIP_IDS = new Set(['blazor-error-ui', 'components-reconnect-modal']);

    function isKeptVisible(el: Element) {
        return SKIP_TAGS.has(el.tagName)
            || SKIP_IDS.has(el.id)
            || el.tagName === 'COMPONENTS-RECONNECT-MODAL'
            || el.hasAttribute('data-bit-bh-live-region');
    }

    /** Hides everything except `keep` elements (and their ancestors) from AT and interaction. */
    function hideOthers(keepEls: (Element | null)[]) {
        const keep = keepEls.filter(Boolean);
        const keepAncestors = new Set<Element>();
        for (const el of keep) {
            let node: Element | null = el;
            while (node && node !== document.body) { keepAncestors.add(node); node = node.parentElement; }
        }
        const hidden: Element[] = [];
        const visit = (parent: Element) => {
            for (const child of Array.from(parent.children)) {
                if (isKeptVisible(child)) continue;
                if (keep.includes(child)) continue;
                if (keepAncestors.has(child)) { visit(child); continue; }
                hideElement(child);
                hidden.push(child);
            }
        };
        visit(document.body);
        return hidden;
    }

    function lockScroll() {
        if (scrollLockCount++ > 0) return;
        const body = document.body;
        const html = document.documentElement;
        const scrollbar = window.innerWidth - html.clientWidth;
        scrollSnapshot = { overflow: body.style.overflow, paddingRight: body.style.paddingRight };
        body.style.overflow = 'hidden';
        if (scrollbar > 0) {
            const current = parseFloat(getComputedStyle(body).paddingRight) || 0;
            body.style.paddingRight = current + scrollbar + 'px';
        }
        body.setAttribute('data-bit-bh-scroll-locked', '');
    }

    function unlockScroll() {
        if (scrollLockCount === 0) return;
        if (--scrollLockCount > 0) return;
        const body = document.body;
        if (scrollSnapshot) {
            body.style.overflow = scrollSnapshot.overflow;
            body.style.paddingRight = scrollSnapshot.paddingRight;
        }
        scrollSnapshot = null;
        body.removeAttribute('data-bit-bh-scroll-locked');
    }

    const modal = {
        hideOthers(ids: ElementLike[]) {
            return hideOthers((ids || []).map((id) => byId(id)));
        },
        unhide(elements: Element[]) {
            for (const el of elements || []) unhideElement(el);
        },
        lockScroll,
        unlockScroll,
    };

    // ─── portal ──────────────────────────────────────────────────────────────────
    //
    // Moves a Blazor-rendered element to a container elsewhere in the document.
    // Blazor keeps direct references to the nodes it renders and removes them via
    // node.parentNode, so a component whose only root element is teleported keeps
    // working: cascading values, events and re-renders are unaffected.

    const portalHandles = new Map<number, { el: HTMLElement; placeholder: Comment }>();
    let portalSeq = 0;

    function portalRoot(target: true | string | Element | null | undefined) {
        if (target && target !== true) {
            const el = typeof target === 'string' ? (document.getElementById(target) || document.querySelector(target)) : target;
            if (el) return el;
        }
        let root = document.querySelector('[data-bit-bh-portal-root]');
        if (!root) {
            root = document.createElement('div');
            root.setAttribute('data-bit-bh-portal-root', '');
            root.id = 'bit-bh-portal-root';
            document.body.appendChild(root);
        }
        return root;
    }

    const portal = {
        move(element: ElementLike, target?: true | string) {
            const el = byId(element);
            if (!el || !el.parentNode) return 0;
            // Already teleported (a second move of the same element while the first is in force):
            // hand back the handle in force, so a restore returns the element to its original place.
            for (const [handle, state] of portalHandles) if (state.el === el) return handle;
            const placeholder = document.createComment('bit-bh-portal');
            el.parentNode.insertBefore(placeholder, el);
            portalRoot(target).appendChild(el);
            const id = ++portalSeq;
            portalHandles.set(id, { el, placeholder });
            return id;
        },
        restore(handle: number) {
            const state = portalHandles.get(handle);
            if (!state) return;
            portalHandles.delete(handle);
            const { el, placeholder } = state;
            if (placeholder.parentNode) {
                if (el.isConnected) placeholder.parentNode.insertBefore(el, placeholder);
                placeholder.parentNode.removeChild(placeholder);
            }
        },
    };

    // ─── presence ────────────────────────────────────────────────────────────────
    //
    // Exit animations without server round-trips. Components render `hidden` the
    // moment they close (so closing is instant when there is nothing to animate).
    // Elements marked [data-bit-bh-presence] are watched: when `hidden` is added, the
    // element is shown again for as long as a CSS animation / transition triggered
    // by its closed state runs, then hidden for real - with the value Blazor rendered
    // (`until-found` included). Reopening cancels the exit.
    //
    // While visible, --bit-bh-content-height / --bit-bh-content-width hold the element's
    // natural size so collapsibles can animate height: 0 ↔ auto.

    function parseDurations(value: string) {
        return String(value || '0s').split(',').map((s) => {
            s = s.trim();
            const n = parseFloat(s);
            if (isNaN(n)) return 0;
            return s.endsWith('ms') ? n : n * 1000;
        });
    }

    function longestMotion(el: Element) {
        const cs = getComputedStyle(el);
        let max = 0;
        if (cs.animationName && cs.animationName !== 'none') {
            const d = parseDurations(cs.animationDuration);
            const delay = parseDurations(cs.animationDelay);
            d.forEach((v, i) => { max = Math.max(max, v + (delay[i % delay.length] || 0)); });
        }
        const td = parseDurations(cs.transitionDuration);
        const tdel = parseDurations(cs.transitionDelay);
        td.forEach((v, i) => { max = Math.max(max, v + (tdel[i % tdel.length] || 0)); });
        return max;
    }

    /** Finite animations / transitions currently running on the element itself. */
    function runningAnimations(el: Element) {
        if (typeof el.getAnimations !== 'function') return null;
        getComputedStyle(el).getPropertyValue('opacity'); // flush style so new animations start
        const subtree = el.getAttribute('data-bit-bh-presence') === 'subtree';
        return el.getAnimations({ subtree }).filter((a) => {
            if (a.playState === 'finished' || a.playState === 'idle') return false;
            const end = a.effect?.getComputedTiming?.().endTime;
            return end !== Infinity;
        });
    }

    function waitForMotion(el: Element | null): Promise<void> {
        if (!el || !el.isConnected) return Promise.resolve();
        const anims = runningAnimations(el);
        if (anims) {
            return anims.length === 0
                ? Promise.resolve()
                : Promise.allSettled(anims.map((a) => a.finished)).then(() => { });
        }
        // Fallback for browsers without the Web Animations API.
        return new Promise<void>((resolve) => {
            const ms = longestMotion(el);
            if (ms <= 0) { resolve(); return; }
            const finish = () => {
                clearTimeout(timer);
                el.removeEventListener('animationend', onEnd);
                el.removeEventListener('transitionend', onEnd);
                resolve();
            };
            const onEnd = (e: Event) => { if (e.target === el) finish(); };
            const timer = setTimeout(finish, ms + 50);
            el.addEventListener('animationend', onEnd);
            el.addEventListener('transitionend', onEnd);
        });
    }

    const presenceExits = new WeakMap<Element, { promise?: Promise<void> }>();
    let presenceObserver: MutationObserver | null = null;

    function setContentSize(el: HTMLElement) {
        el.style.setProperty('--bit-bh-content-height', el.scrollHeight + 'px');
        el.style.setProperty('--bit-bh-content-width', el.scrollWidth + 'px');
    }

    function startExit(el: HTMLElement) {
        if (presenceExits.has(el)) return;
        const hiddenValue = el.getAttribute('hidden') ?? '';
        el.removeAttribute('hidden');
        const anims = runningAnimations(el);
        const hasMotion = anims ? anims.length > 0 : longestMotion(el) > 0;
        if (!hasMotion) {
            el.setAttribute('hidden', hiddenValue);
            presenceObserver?.takeRecords();
            return;
        }
        el.setAttribute('data-exiting', '');
        const entry: { promise?: Promise<void> } = {};
        entry.promise = waitForMotion(el).then(() => {
            if (presenceExits.get(el) !== entry) return;
            presenceExits.delete(el);
            el.removeAttribute('data-exiting');
            el.setAttribute('hidden', hiddenValue);
            presenceObserver?.takeRecords();
        });
        presenceExits.set(el, entry);
        presenceObserver?.takeRecords();
    }

    function cancelExit(el: HTMLElement) {
        if (!presenceExits.has(el)) return;
        presenceExits.delete(el);
        el.removeAttribute('data-exiting');
    }

    function onPresenceMutations(records: MutationRecord[]) {
        for (const r of records) {
            const el = r.target;
            if (!isElement(el) || !el.hasAttribute('data-bit-bh-presence')) continue;
            if (r.attributeName === 'hidden') {
                if (el.hasAttribute('hidden')) {
                    if (r.oldValue === null) startExit(el);
                } else {
                    cancelExit(el);
                    setContentSize(el);
                }
            } else if (r.attributeName === 'data-state') {
                const state = el.getAttribute('data-state');
                if (state === 'open' || state === 'active' || state === 'checked') {
                    cancelExit(el);
                    if (!el.hasAttribute('hidden')) setContentSize(el);
                } else if (!el.hasAttribute('hidden') && !presenceExits.has(el)) {
                    setContentSize(el);
                }
            }
        }
    }

    function installPresence() {
        if (typeof MutationObserver === 'undefined') return;
        presenceObserver = new MutationObserver(onPresenceMutations);
        presenceObserver.observe(document.documentElement, {
            subtree: true,
            attributes: true,
            attributeOldValue: true,
            attributeFilter: ['hidden', 'data-state'],
        });
    }

    /** Resolves when the element's presence exit (if any) has finished. */
    function presenceDone(el: HTMLElement | null) {
        if (!el) return Promise.resolve();
        const exit = presenceExits.get(el);
        if (exit) return exit.promise;
        return el.hasAttribute('hidden') ? Promise.resolve() : waitForMotion(el);
    }

    const presence = {
        waitForExit(id: ElementLike) {
            return presenceDone(byId(id));
        },
        measure(id: ElementLike) {
            const el = byId(id);
            if (el) setContentSize(el);
        },
    };

    // ─── floating ────────────────────────────────────────────────────────────────
    //
    // One controller per open floating surface. open() performs, in order:
    // portal → reveal → position → layer → modal → focus. close() undoes it, waits
    // for the exit animation, and returns the element to its original place.

    const floatingHandles = new Map<number, FloatingState>();
    let floatingSeq = 0;

    const floating = {
        /**
         * opts: {
         *   portalIds: [ids moved to the portal root, in order], portal (true | selector | id),
         *   reference (id | {x, y}), anchor (anchor options),
         *   layer: { escape, outside, focusOutside, modal, hover, parentFloating, ids: [] },
         *   modal: { hideOthers, scrollLock },
         *   focus: { trap, loop, autoFocus, restore, returnFocusId },
         * }
         */
        open(dotnet: DotNetRef | null, contentId: string, opts: FloatingOptions) {
            const content = byId(contentId);
            if (!content) return 0;
            opts = opts || {};
            const id = ++floatingSeq;
            const surfaceIds = (opts.portalIds && opts.portalIds.length ? opts.portalIds : [contentId]).filter((x) => !!byId(x));
            const state: FloatingState = { id, contentId, opts, dotnet, surfaceIds, portals: [] };

            if (opts.portal) {
                for (const sid of surfaceIds) {
                    const h = portal.move(sid, opts.portal);
                    if (h) state.portals.push(h);
                }
            }

            for (const sid of surfaceIds) byId(sid)?.removeAttribute('hidden');
            content.removeAttribute('hidden');

            if (opts.anchor && opts.reference) {
                state.anchor = anchor.start(opts.reference, content, opts.anchor);
            }

            if (opts.layer) {
                const ids = [contentId, ...(opts.layer.ids || [])];
                const parent = opts.layer.parentFloating ? floatingHandles.get(opts.layer.parentFloating) : null;
                state.layer = layers.push(dotnet, ids, { ...opts.layer, parent: parent?.layer || 0 });
            }

            if (opts.modal) {
                if (opts.modal.hideOthers !== false) state.hidden = modal.hideOthers([contentId, ...surfaceIds]);
                if (opts.modal.scrollLock !== false) { lockScroll(); state.scrollLocked = true; }
            }

            if (opts.focus) {
                state.scope = focus.createScope(contentId, { ...opts.focus, layer: state.layer });
            }

            floatingHandles.set(id, state);
            return id;
        },

        update(handle: number, opts?: { anchor?: AnchorOptions; layerIds?: string[] }) {
            const state = floatingHandles.get(handle);
            if (!state) return;
            if (opts?.anchor && state.anchor) anchor.update(state.anchor, opts.anchor);
            else if (state.anchor) anchor.update(state.anchor);
            if (opts?.layerIds && state.layer) layers.setIds(state.layer, [state.contentId, ...opts.layerIds]);
        },

        setPoint(handle: number, x: number, y: number) {
            const state = floatingHandles.get(handle);
            if (state?.anchor) anchor.setPoint(state.anchor, x, y);
        },

        /** Closes the surface; resolves once the exit animation (if any) has finished. */
        async close(handle: number, opts?: { restoreFocus?: boolean; waitForExit?: boolean; hide?: boolean }) {
            const state = floatingHandles.get(handle);
            if (!state) return;
            floatingHandles.delete(handle);
            opts = opts || {};

            // Close nested surfaces first (a submenu never outlives its menu), and tell their
            // owners: a surface closed under them would otherwise still count itself open.
            for (const [childHandle, child] of Array.from(floatingHandles.entries())) {
                if (child.opts.layer?.parentFloating === handle) {
                    floating.close(childHandle, { waitForExit: false, restoreFocus: false });
                    child.dotnet?.invokeMethodAsync('OnClosedByParent').catch(() => { });
                }
            }

            // Un-inert the page before restoring focus: inert elements can't take focus.
            if (state.hidden) modal.unhide(state.hidden);
            if (state.scrollLocked) unlockScroll();
            if (state.layer) layers.remove(state.layer);
            if (state.scope) focus.destroyScope(state.scope, opts.restoreFocus);

            if (opts.waitForExit !== false) {
                const els = [...new Set([...state.surfaceIds, state.contentId])].map((x) => byId(x)).filter(Boolean);
                await Promise.all(els.map(presenceDone));
            }

            if (state.anchor) anchor.stop(state.anchor, true);
            const content = byId(state.contentId);
            if (content && opts.hide !== false) content.setAttribute('hidden', '');
            for (const h of state.portals.reverse()) portal.restore(h);
        },

        /** Closes every open surface. Used when the circuit tears down. */
        closeAll() {
            for (const handle of Array.from(floatingHandles.keys())) floating.close(handle, { waitForExit: false });
        },
    };

    // ─── hotkeys ─────────────────────────────────────────────────────────────────
    //
    // Global keyboard shortcuts ("mod+k", "shift+?", "ctrl+alt+p"). "mod" is Cmd
    // on Apple platforms and Ctrl elsewhere. Shortcuts are ignored while typing in
    // editable fields unless allowInInputs is set.

    const hotkeyHandles = new Map<number, (e: KeyboardEvent) => void>();
    let hotkeySeq = 0;
    const isApple = typeof navigator !== 'undefined' && /Mac|iPhone|iPad|iPod/i.test(navigator.platform || navigator.userAgent);

    function parseHotkey(combo: string) {
        const parts = String(combo).toLowerCase().split('+').map((p) => p.trim()).filter(Boolean);
        const key = parts.pop() || '';
        const mods = new Set(parts.map((m) => (m === 'mod' ? (isApple ? 'meta' : 'ctrl') : m === 'cmd' ? 'meta' : m === 'option' ? 'alt' : m)));
        return { key: key === 'space' ? ' ' : key, mods };
    }

    function hotkeyMatches(e: KeyboardEvent, hk: ReturnType<typeof parseHotkey>) {
        if ((e.key || '').toLowerCase() !== hk.key && (e.code || '').toLowerCase() !== `key${hk.key}`) return false;
        return e.ctrlKey === hk.mods.has('ctrl') && e.metaKey === hk.mods.has('meta')
            && e.altKey === hk.mods.has('alt') && (hk.key.length > 1 || hk.mods.has('shift') ? e.shiftKey === hk.mods.has('shift') : true);
    }

    function isEditable(el: EventTarget | null) {
        return isElement(el) && (el.isContentEditable || /^(input|textarea|select)$/i.test(el.tagName));
    }

    const hotkeys = {
        register(dotnet: DotNetRef, combos: string | string[], options?: { allowInInputs?: boolean }) {
            const id = ++hotkeySeq;
            const parsed = (Array.isArray(combos) ? combos : [combos]).map(parseHotkey);
            const allowInInputs = !!options?.allowInInputs;
            const handler = (e: KeyboardEvent) => {
                if (isComposing(e)) return;
                if (!allowInInputs && isEditable(e.target) && !parsed.some((h) => h.mods.has('ctrl') || h.mods.has('meta'))) return;
                if (parsed.some((h) => hotkeyMatches(e, h))) {
                    e.preventDefault();
                    dotnet.invokeMethodAsync('OnHotkey').catch(() => { });
                }
            };
            document.addEventListener('keydown', handler, true);
            hotkeyHandles.set(id, handler);
            return id;
        },
        unregister(handle: number) {
            const handler = hotkeyHandles.get(handle);
            if (!handler) return;
            hotkeyHandles.delete(handle);
            document.removeEventListener('keydown', handler, true);
        },
    };

    // ─── collection ──────────────────────────────────────────────────────────────

    const collection = {
        /**
         * Returns [id, text] for every element tagged [data-bit-bh-item="key"], in document
         * order. The text (data-bit-bh-text, else the item's own text - nested items of the
         * same collection, like child tree items, excluded) feeds typeahead.
         */
        order(key: string) {
            const selector = `[data-bit-bh-item="${CSS.escape(key)}"]`;
            const ownText = (el: Element) => {
                if (!el.querySelector(selector)) return el.textContent ?? '';
                const clone = el.cloneNode(true) as Element;
                clone.querySelectorAll(selector).forEach((nested) => nested.remove());
                return clone.textContent ?? '';
            };
            return Array.from(document.querySelectorAll(selector))
                .map((el) => [el.id, (el.getAttribute('data-bit-bh-text') ?? ownText(el)).trim().replace(/\s+/g, ' ')]);
        },
    };

    // ─── dom ─────────────────────────────────────────────────────────────────────

    function scrollParent(el: Element) {
        let node = el.parentElement;
        while (node && node !== document.body) {
            const style = getComputedStyle(node);
            if (/(auto|scroll|overlay)/.test(style.overflowY) && node.scrollHeight > node.clientHeight) return node;
            node = node.parentElement;
        }
        return null;
    }

    const dom = {
        scrollIntoView(id: ElementLike, block?: ScrollLogicalPosition) {
            const el = byId(id);
            if (!el) return;
            const parent = scrollParent(el);
            if (parent) {
                const pr = parent.getBoundingClientRect();
                const er = el.getBoundingClientRect();
                if (er.top < pr.top) parent.scrollTop -= pr.top - er.top;
                else if (er.bottom > pr.bottom) parent.scrollTop += er.bottom - pr.bottom;
            } else {
                el.scrollIntoView({ block: block || 'nearest', inline: 'nearest' });
            }
        },
        /** Scrolls a virtualized container so the item at `index` (fixed `itemSize`) is visible. */
        scrollToIndex(containerId: ElementLike, index: number, itemSize: number, itemId?: string) {
            const container = byId(containerId);
            if (!container) return;
            const el = itemId ? document.getElementById(itemId) : null;
            if (el) { dom.scrollIntoView(itemId); return; }
            const top = index * itemSize;
            if (top < container.scrollTop) container.scrollTop = top;
            else if (top + itemSize > container.scrollTop + container.clientHeight) container.scrollTop = top + itemSize - container.clientHeight;
        },
        click(id: ElementLike) {
            byId(id)?.click();
        },
        rect(id: ElementLike) {
            const el = byId(id);
            if (!el) return null;
            const r = el.getBoundingClientRect();
            return { x: r.x, y: r.y, width: r.width, height: r.height };
        },
        setAttribute(id: ElementLike, name: string, value: string | null | undefined) {
            const el = byId(id);
            if (!el) return;
            if (value === null || value === undefined) el.removeAttribute(name);
            else el.setAttribute(name, value);
        },
    };

    // ─── announcer ───────────────────────────────────────────────────────────────

    const VISUALLY_HIDDEN = 'position:absolute;width:1px;height:1px;margin:-1px;padding:0;border:0;overflow:hidden;clip:rect(0 0 0 0);clip-path:inset(50%);white-space:nowrap;';
    let liveRoot: HTMLElement | null = null;

    function liveRegion(politeness: string) {
        if (!document.body) return null;
        if (!liveRoot || !liveRoot.isConnected) {
            liveRoot = document.createElement('div');
            liveRoot.setAttribute('data-bit-bh-live-region', '');
            liveRoot.style.cssText = VISUALLY_HIDDEN;
            for (const p of ['polite', 'assertive']) {
                const region = document.createElement('div');
                region.setAttribute('aria-live', p);
                region.setAttribute('aria-atomic', 'true');
                region.setAttribute('role', p === 'assertive' ? 'alert' : 'status');
                region.setAttribute('data-bit-bh-politeness', p);
                liveRoot.appendChild(region);
            }
            document.body.appendChild(liveRoot);
        }
        return liveRoot.querySelector(`[data-bit-bh-politeness="${politeness === 'assertive' ? 'assertive' : 'polite'}"]`);
    }

    const announcer = {
        announce(message: string, politeness: string, clearAfterMs: number) {
            const region = liveRegion(politeness);
            if (!region) return;
            region.textContent = '';
            // A short delay ensures screen readers notice the change even when the
            // same message is announced twice in a row.
            setTimeout(() => {
                region.textContent = message;
                if (clearAfterMs > 0) setTimeout(() => { if (region.textContent === message) region.textContent = ''; }, clearAfterMs);
            }, 50);
        },
        clear() {
            if (!liveRoot) return;
            for (const r of Array.from(liveRoot.children)) r.textContent = '';
        },
    };

    // ─── transition ──────────────────────────────────────────────────────────────

    const runningTransitions = new WeakMap<Element, object>();

    function splitClasses(str: string | undefined) {
        return str ? String(str).trim().split(/\s+/).filter(Boolean) : [];
    }

    function nextFrame() {
        return new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));
    }

    // Runs one phase of a transition. Besides the class lists, it maintains the
    // Headless UI v2 data attributes so plain CSS can drive transitions:
    //   [data-transition]   while either phase runs
    //   [data-enter] / [data-leave]
    //   [data-closed]       on the first frame of enter and the last frames of leave
    async function runTransition(el: HTMLElement, phase: 'enter' | 'leave', base: string, from: string, to: string, final: string, clear: string) {
        const token = {};
        runningTransitions.set(el, token);
        el.classList.remove(...splitClasses(base), ...splitClasses(from), ...splitClasses(to), ...splitClasses(clear));
        el.removeAttribute(phase === 'enter' ? 'data-leave' : 'data-enter');
        el.setAttribute('data-transition', '');
        el.setAttribute(`data-${phase}`, '');
        if (phase === 'enter') el.setAttribute('data-closed', '');
        el.classList.add(...splitClasses(base), ...splitClasses(from));

        await nextFrame();
        if (runningTransitions.get(el) !== token) return false;

        el.classList.remove(...splitClasses(from));
        el.classList.add(...splitClasses(to));
        if (phase === 'enter') el.removeAttribute('data-closed');
        else el.setAttribute('data-closed', '');

        await waitForMotion(el);
        if (runningTransitions.get(el) !== token) return false;

        el.classList.remove(...splitClasses(base), ...splitClasses(to));
        el.classList.add(...splitClasses(final));
        el.removeAttribute('data-transition');
        el.removeAttribute(`data-${phase}`);
        runningTransitions.delete(el);
        return true;
    }

    const transition = {
        /** Runs the enter transition. Resolves true when it completed, false when interrupted. */
        enter(id: ElementLike, c: TransitionClasses) {
            const el = byId(id);
            if (!el) return Promise.resolve(true);
            c = c || {};
            return runTransition(el, 'enter', c.enter, c.enterFrom, c.enterTo, c.entered, `${c.leave || ''} ${c.leaveFrom || ''} ${c.leaveTo || ''}`);
        },
        leave(id: ElementLike, c: TransitionClasses) {
            const el = byId(id);
            if (!el) return Promise.resolve(true);
            c = c || {};
            el.classList.remove(...splitClasses(c.entered));
            return runTransition(el, 'leave', c.leave, c.leaveFrom, c.leaveTo, '', `${c.enter || ''} ${c.enterFrom || ''} ${c.enterTo || ''}`);
        },
    };

    // ─── sortable ────────────────────────────────────────────────────────────────
    //
    // Pointer-driven reordering. While dragging, siblings are shifted with CSS
    // transforms (no .NET round-trips); on drop the final indexes are reported to
    // .NET, which reorders its data and re-renders.

    interface SortableState {
        id: number;
        dotnet: DotNetRef | null;
        containerId: string;
        options: { orientation?: string; threshold?: number };
        drag: unknown;
        onPointerDown?: (e: PointerEvent) => void;
    }

    const sortables = new Map<number, SortableState>();
    let sortableSeq = 0;

    function sortableItems(state: SortableState) {
        const container = byId(state.containerId);
        if (!container) return [];
        return Array.from(container.querySelectorAll<HTMLElement>(`[data-bit-bh-sortable-item="${CSS.escape(state.containerId)}"]`));
    }

    function startSortDrag(state: SortableState, item: HTMLElement, e: PointerEvent) {
        const items = sortableItems(state);
        const from = items.indexOf(item);
        if (from < 0) return;
        const vertical = state.options.orientation !== 'horizontal';
        const rects = items.map((el) => el.getBoundingClientRect());
        const drag = { item, from, to: from, items, rects, vertical, startX: e.clientX, startY: e.clientY, pointerId: e.pointerId, started: false };
        state.drag = drag;
        // The inline values the drag writes on the item are the consumer's again once it ends.
        const savedPosition = item.style.position;
        const savedZIndex = item.style.zIndex;

        const move = (ev: PointerEvent) => {
            if (ev.pointerId !== drag.pointerId) return;
            const dx = ev.clientX - drag.startX;
            const dy = ev.clientY - drag.startY;
            if (!drag.started) {
                if (Math.hypot(dx, dy) < (state.options.threshold ?? 4)) return;
                drag.started = true;
                item.setAttribute('data-dragging', '');
                byId(state.containerId)?.setAttribute('data-dragging', '');
                item.style.zIndex = '1';
                item.style.position = item.style.position || 'relative';
                for (const el of items) if (el !== item) el.style.transition = 'transform 150ms ease';
                try { item.setPointerCapture(drag.pointerId); } catch { /* ignore */ }
                state.dotnet?.invokeMethodAsync('OnPointerDragStart', from).catch(() => { });
            }
            ev.preventDefault();
            item.style.transform = vertical ? `translateY(${dy}px)` : `translateX(${dx}px)`;

            const r = drag.rects[from];
            const center = vertical ? r.top + r.height / 2 + dy : r.left + r.width / 2 + dx;
            let to = from;
            for (let i = 0; i < drag.rects.length; i++) {
                const ri = drag.rects[i];
                const mid = vertical ? ri.top + ri.height / 2 : ri.left + ri.width / 2;
                if (i < from && center < mid) { to = i; break; }
                if (i > from && center > mid) to = i;
            }
            drag.to = to;
            const size = vertical ? r.height : r.width;
            const gapSize = items.length > 1 ? (vertical
                ? Math.abs((drag.rects[1].top - drag.rects[0].bottom))
                : Math.abs((drag.rects[1].left - drag.rects[0].right))) : 0;
            items.forEach((el, i) => {
                if (el === item) return;
                let shift = 0;
                if (from < to && i > from && i <= to) shift = -(size + gapSize);
                if (from > to && i >= to && i < from) shift = size + gapSize;
                el.style.transform = shift ? (vertical ? `translateY(${shift}px)` : `translateX(${shift}px)`) : '';
            });
        };

        const end = (ev: PointerEvent | null, cancelled: boolean) => {
            if (ev && ev.pointerId !== drag.pointerId) return;
            document.removeEventListener('pointermove', move, true);
            document.removeEventListener('pointerup', up, true);
            document.removeEventListener('pointercancel', cancel, true);
            document.removeEventListener('keydown', esc, true);
            for (const el of items) { el.style.transform = ''; el.style.transition = ''; }
            item.style.zIndex = savedZIndex;
            item.style.position = savedPosition;
            item.removeAttribute('data-dragging');
            byId(state.containerId)?.removeAttribute('data-dragging');
            state.drag = null;
            if (!drag.started) return;
            // Swallow the click that follows a drag.
            const swallow = (ce: Event) => { ce.stopPropagation(); ce.preventDefault(); };
            document.addEventListener('click', swallow, { capture: true, once: true });
            setTimeout(() => document.removeEventListener('click', swallow, true), 0);
            state.dotnet?.invokeMethodAsync('OnPointerDragEnd', drag.from, cancelled ? drag.from : drag.to, !!cancelled).catch(() => { });
        };
        const up = (ev: PointerEvent) => end(ev, false);
        const cancel = (ev: PointerEvent) => end(ev, true);
        const esc = (ev: KeyboardEvent) => { if (ev.key === 'Escape' && drag.started) { ev.preventDefault(); ev.stopPropagation(); end(null, true); } };

        document.addEventListener('pointermove', move, true);
        document.addEventListener('pointerup', up, true);
        document.addEventListener('pointercancel', cancel, true);
        document.addEventListener('keydown', esc, true);
    }

    const sortable = {
        /** options: { orientation, handleSelector, threshold } */
        attach(dotnet: DotNetRef, containerId: ElementLike, options: SortableState['options']) {
            const container = byId(containerId);
            if (!container) return 0;
            const id = ++sortableSeq;
            const state: SortableState = { id, dotnet, containerId: container.id, options: options || {}, drag: null };
            state.onPointerDown = (e: PointerEvent) => {
                if (e.button !== 0 || state.drag) return;
                const target = e.target as Element;
                const item = target.closest<HTMLElement>(`[data-bit-bh-sortable-item="${CSS.escape(state.containerId)}"]`);
                if (!item || item.hasAttribute('data-disabled')) return;
                const handle = item.querySelector('[data-bit-bh-sortable-handle]');
                if (handle && !handle.contains(target)) return;
                if (!handle && target.closest('input, textarea, select, [contenteditable="true"]')) return;
                startSortDrag(state, item, e);
            };
            container.addEventListener('pointerdown', state.onPointerDown);
            sortables.set(id, state);
            return id;
        },
        update(handle: number, options: SortableState['options']) {
            const state = sortables.get(handle);
            if (state) state.options = options || {};
        },
        detach(handle: number) {
            const state = sortables.get(handle);
            if (!state) return;
            sortables.delete(handle);
            byId(state.containerId)?.removeEventListener('pointerdown', state.onPointerDown);
        },
    };

    // ─── slider ──────────────────────────────────────────────────────────────────

    interface SliderState {
        id: number;
        dotnet: DotNetRef;
        rootId: string;
        trackId: string | null;
        options: { orientation?: string; inverted?: boolean };
        pending: number | null;
        busy: boolean;
        onPointerDown?: (e: PointerEvent) => void;
    }

    const sliders = new Map<number, SliderState>();
    let sliderSeq = 0;

    function sliderFraction(state: SliderState, e: PointerEvent) {
        const track = byId(state.trackId) || byId(state.rootId);
        if (!track) return 0;
        const r = track.getBoundingClientRect();
        const vertical = state.options.orientation === 'vertical';
        let f = vertical ? (r.bottom - e.clientY) / r.height : (e.clientX - r.left) / r.width;
        if (!vertical && isRtl(track)) f = 1 - f;
        if (state.options.inverted) f = 1 - f;
        return Math.max(0, Math.min(1, f));
    }

    const slider = {
        /** options: { orientation, inverted }. Calls OnSliderPointer(phase, fraction) on .NET. */
        attach(dotnet: DotNetRef, rootId: string, trackId: string | null, options: SliderState['options']) {
            const root = byId(rootId);
            if (!root) return 0;
            const id = ++sliderSeq;
            const state: SliderState = { id, dotnet, rootId, trackId, options: options || {}, pending: null, busy: false };

            const send = (phase: string, fraction: number) => {
                // Coalesce pointer moves: only one call in flight at a time.
                if (phase === 'move' && state.busy) { state.pending = fraction; return; }
                state.busy = true;
                dotnet.invokeMethodAsync('OnSliderPointer', phase, fraction).catch(() => { }).finally(() => {
                    state.busy = false;
                    if (state.pending !== null) { const f = state.pending; state.pending = null; send('move', f); }
                });
            };

            state.onPointerDown = (e: PointerEvent) => {
                if (e.button !== 0 || root.hasAttribute('data-disabled')) return;
                e.preventDefault();
                const thumb = (e.target as Element).closest<HTMLElement>('[role="slider"]');
                (thumb || root.querySelector<HTMLElement>('[role="slider"]'))?.focus({ preventScroll: true });
                try { root.setPointerCapture(e.pointerId); } catch { /* ignore */ }
                root.setAttribute('data-dragging', '');
                send('start', sliderFraction(state, e));
                const move = (ev: PointerEvent) => send('move', sliderFraction(state, ev));
                const up = (ev: PointerEvent) => {
                    root.removeEventListener('pointermove', move);
                    root.removeEventListener('pointerup', up);
                    root.removeEventListener('pointercancel', up);
                    root.removeAttribute('data-dragging');
                    state.pending = null;
                    send('end', sliderFraction(state, ev));
                };
                root.addEventListener('pointermove', move);
                root.addEventListener('pointerup', up);
                root.addEventListener('pointercancel', up);
            };
            root.addEventListener('pointerdown', state.onPointerDown);
            sliders.set(id, state);
            return id;
        },
        update(handle: number, options: SliderState['options']) {
            const state = sliders.get(handle);
            if (state) state.options = options || {};
        },
        detach(handle: number) {
            const state = sliders.get(handle);
            if (!state) return;
            sliders.delete(handle);
            byId(state.rootId)?.removeEventListener('pointerdown', state.onPointerDown);
        },
    };

    return {
        install, onEnhancedLoad,
        keys, interaction, anchor, layers, focus, modal, portal, presence, floating,
        hotkeys, collection, dom, announcer, transition, sortable, slider,
    };
}

type Runtime = ReturnType<typeof createRuntime>;

const runtime: Runtime = ((globalThis as { __bitBlazorHeadlessUI?: Runtime }).__bitBlazorHeadlessUI ??= createRuntime());

export const {
    install, onEnhancedLoad,
    keys, interaction, anchor, layers, focus, modal, portal, presence, floating,
    hotkeys, collection, dom, announcer, transition, sortable, slider,
} = runtime;

install();
