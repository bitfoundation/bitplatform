// Bit.Bheadless - JS runtime.
//
// The C# side imports this module on first use (BitBhInterop) and the JS
// initializer (Bit.Bheadless.lib.module.js) imports it at startup to install
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
//   scrub       - pointer-drag scrubbing of number fields
//   rating      - pointer preview / picking of ratings
//   toasts      - toast stack layout, expanded / paused state, swipe-to-dismiss
//   indicator   - selection indicators that follow the selected tab / segment
//   carousel    - carousel swipes
//   area        - two-dimensional pointer areas (color area)
//   resizer     - column resize handles
//   landmarks   - F6 / Shift+F6 navigation between page landmarks
//   scrollSpy   - which sections of a table of contents are in view
//   navigationMenu - viewport sizing and Tab order ([data-bit-bh-tab-into|out]) of navigation menus
//   drawer      - swipe-to-dismiss of drawers
//   (files)     - declarative file pickers and drop zones ([data-bit-bh-file-trigger|drop])
//   scrollArea  - custom scrollbars over a natively scrolling viewport
//   splitter    - resizable panel handles (pointer drags, saved layouts)
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
    alignItem?: string;
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
    secondaryIds?: string[];
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

    // Inputs that only take some characters (a number field) list them in data-bit-bh-chars: typed text with
    // any other character is refused before it reaches the input. Pasted and dropped text goes through - the
    // component reads what it can of it when the edit is committed.
    // data-bit-bh-pattern does the same with a regular expression every typed character must match (a PIN input).
    const patterns = new Map<string, RegExp | null>();
    function patternOf(source: string) {
        if (!patterns.has(source)) {
            let re: RegExp | null = null;
            try { re = new RegExp(`^(?:${source})$`, 'u'); } catch { re = null; }
            patterns.set(source, re);
        }
        return patterns.get(source);
    }

    function onGlobalBeforeInput(e: InputEvent) {
        const target = e.target;
        if (!isElement(target) || e.inputType !== 'insertText' || !e.data) return;
        const allowed = target.getAttribute('data-bit-bh-chars');
        const pattern = target.getAttribute('data-bit-bh-pattern');
        const re = pattern ? patternOf(pattern) : null;
        for (const ch of e.data) {
            if ((allowed !== null && !allowed.includes(ch)) || (re && !re.test(ch))) { e.preventDefault(); return; }
        }
    }

    // Inputs marked data-bit-bh-select-on-focus select their text when focused (a PIN position), so typing
    // replaces it. Deferred a frame: a mouse press would otherwise place the caret over the selection.
    function onSelectOnFocus(e: FocusEvent) {
        const target = e.target;
        if (!(target instanceof HTMLInputElement) || !target.hasAttribute('data-bit-bh-select-on-focus')) return;
        requestAnimationFrame(() => { if (document.activeElement === target) target.select(); });
    }

    // A focused element marked data-bit-bh-wheel steps its value with the wheel (a number field): the page must
    // not scroll under it. Listeners on the document are passive by default for wheel events, so Blazor's own
    // preventDefault can't do this - and a non-passive one there would make every page scroll on the main thread.
    // The element gets its own non-passive listener while it has the focus instead.
    function preventWheel(e: WheelEvent) {
        if (!e.ctrlKey) e.preventDefault();
    }

    function onWheelFocusIn(e: FocusEvent) {
        const target = e.target;
        if (!isElement(target) || !target.hasAttribute('data-bit-bh-wheel')) return;
        target.addEventListener('wheel', preventWheel as EventListener, { passive: false });
        target.addEventListener('blur', () => target.removeEventListener('wheel', preventWheel as EventListener), { once: true });
    }

    // ─── files ───────────────────────────────────────────────────────────────────
    //
    // File uploads, declaratively: an element marked data-bit-bh-file-trigger="<input id>" opens that
    // file input's picker on click (or Enter / Space) - in the click itself, since browsers only open a
    // picker with the user's activation, which a round trip to .NET could outlast. An element marked
    // data-bit-bh-file-drop="<input id>" accepts dropped files: they are handed to the input, whose
    // change event Blazor's InputFile reports as if they were picked.

    function fileInputOf(el: Element | null, attribute: string) {
        const id = el?.getAttribute(attribute);
        const input = id ? document.getElementById(id) : null;
        return input instanceof HTMLInputElement && input.type === 'file' && !input.disabled ? input : null;
    }

    function onFileTriggerClick(e: MouseEvent) {
        if (e.defaultPrevented || !isElement(e.target)) return;
        const trigger = e.target.closest('[data-bit-bh-file-trigger]');
        if (!trigger) return;
        // A control inside a clickable drop zone (a link, a button) does its own thing.
        const control = e.target.closest('a[href], button, input, select, textarea, label');
        if (control && control !== trigger && trigger.contains(control)) return;
        fileInputOf(trigger, 'data-bit-bh-file-trigger')?.click();
    }

    function onFileTriggerKeyDown(e: KeyboardEvent) {
        if ((e.key !== 'Enter' && e.key !== ' ') || e.repeat || !isElement(e.target)) return;
        const target = e.target;
        // Native buttons click on Enter / Space by themselves (and land in the click handler above).
        if (!target.hasAttribute('data-bit-bh-file-trigger') || target instanceof HTMLButtonElement) return;
        fileInputOf(target, 'data-bit-bh-file-trigger')?.click();
    }

    let dropZone: Element | null = null;

    function setDropZone(zone: Element | null) {
        if (dropZone === zone) return;
        dropZone?.removeAttribute('data-dragging');
        dropZone = zone;
        dropZone?.setAttribute('data-dragging', '');
    }

    function hasFiles(e: DragEvent) {
        return !!e.dataTransfer && Array.from(e.dataTransfer.types || []).includes('Files');
    }

    function onFileDragOver(e: DragEvent) {
        if (!hasFiles(e)) return;
        const zone = isElement(e.target) ? e.target.closest('[data-bit-bh-file-drop]') : null;
        setDropZone(zone && fileInputOf(zone, 'data-bit-bh-file-drop') ? zone : null);
        if (!dropZone) return;
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';
    }

    function onFileDragLeave(e: DragEvent) {
        if (dropZone && (!e.relatedTarget || !dropZone.contains(e.relatedTarget as Node))) setDropZone(null);
    }

    function onFileDrop(e: DragEvent) {
        const zone = dropZone;
        setDropZone(null);
        if (!zone || !hasFiles(e) || !isElement(e.target) || !zone.contains(e.target)) return;
        const input = fileInputOf(zone, 'data-bit-bh-file-drop');
        if (!input) return;
        e.preventDefault();
        let files = e.dataTransfer.files;
        if (!input.multiple && files.length > 1) {
            const one = new DataTransfer();
            one.items.add(files[0]);
            files = one.files;
        }
        input.files = files;
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }

    const keys = {
        matches: matchesKeyToken,
        /** Whether "mod" in a hotkey is Cmd (Apple platforms) rather than Ctrl. */
        isApple: () => isApple,
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
        document.addEventListener('beforeinput', onGlobalBeforeInput as EventListener, true);
        document.addEventListener('click', onFileTriggerClick, false);
        document.addEventListener('keydown', onFileTriggerKeyDown, false);
        document.addEventListener('dragover', onFileDragOver, false);
        document.addEventListener('dragenter', onFileDragOver, false);
        document.addEventListener('dragleave', onFileDragLeave, false);
        document.addEventListener('drop', onFileDrop, false);
        document.addEventListener('dragend', () => setDropZone(null), true);
        document.addEventListener('focusin', onSelectOnFocus, true);
        document.addEventListener('focusin', onWheelFocusIn, true);
        document.addEventListener('keydown', onLayerKeyDownCapture, true);
        document.addEventListener('keydown', onLayerKeyDown, false);
        document.addEventListener('pointerdown', onLayerPointerDown, true);
        document.addEventListener('pointerup', onLayerPressEnd, true);
        document.addEventListener('pointercancel', onLayerPressEnd, true);
        document.addEventListener('pointerdown', recordPress, true);
        document.addEventListener('click', onLayerClick, true);
        document.addEventListener('focusin', onLayerFocusIn, true);
        document.addEventListener('keydown', onTabBetween, false);

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

        // An item lined up over the reference (a listbox's selected option over its button).
        const aligned = options.alignItem ? alignOverReference(floating, options.alignItem, ref, w, h, vp, padding) : null;
        if (aligned) return aligned;

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
                // Wider than the room on either side (a submenu on a phone): overlap the reference rather than
                // run off the screen. A panel above or below keeps clear of its reference and gets the space
                // left for a max-height instead.
                pos.x = Math.max(vp.left + padding, Math.min(pos.x, vp.right - padding - w));
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

    function alignOverReference(floating: HTMLElement, selector: string, ref: ReturnType<typeof resolveReferenceRect>,
        w: number, h: number, vp: ReturnType<typeof viewportRect>, padding: number) {
        let item: HTMLElement | null = null;
        try { item = floating.querySelector<HTMLElement>(selector); } catch { item = null; }
        if (!item) return null;
        // A scrolling list shows the item first, in its middle.
        if (floating.scrollHeight > floating.clientHeight) {
            floating.scrollTop = Math.max(0, item.offsetTop - (floating.clientHeight - item.offsetHeight) / 2);
        }
        const fr = floating.getBoundingClientRect();
        const ir = item.getBoundingClientRect();
        if (ir.height === 0) return null;
        const y = ref.top + ref.height / 2 - (ir.top - fr.top + ir.height / 2);
        const x = ref.left - (ir.left - fr.left);
        const referenceHidden = ref.bottom < vp.top || ref.top > vp.bottom || ref.right < vp.left || ref.left > vp.right;
        return {
            ref, side: 'none', align: 'center',
            x: Math.max(vp.left + padding, Math.min(x, vp.right - padding - w)),
            y: Math.max(vp.top + padding, Math.min(y, vp.bottom - padding - h)),
            w, h, availableHeight: vp.height - padding * 2, availableWidth: vp.width - padding * 2, referenceHidden,
        };
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
            // A quarter turn leaves the arrow's box at its unrotated size: placing the box against the edge would
            // leave a gap of half the size difference. Offsetting it past the edge by half its width plus half its
            // height brings the rotated base onto the edge - from the far side, so the box keeps the content's
            // width to lay out in (a 'right: 100%' leaves it none, and an SVG with max-width: 100% shrinks).
            const outside = -(arrowSize.width + arrowSize.height) / 2 + 'px';
            if (p.side === 'right') arrow.style.left = outside;
            else arrow.style.right = outside;
        }
        // Rotate the default (downward pointing) arrow to face the reference.
        arrow.style.transform = `rotate(${ARROW_ROTATION[p.side]}deg)`;
        arrow.style.transformOrigin = 'center';
        return offset + (vertical ? arrowSize.width / 2 : arrowSize.height / 2);
    }

    function transformOrigin(p: ReturnType<typeof computePlacement>, arrowCenter: number | null) {
        const cross = arrowCenter != null ? `${Math.round(arrowCenter)}px` : 'center';
        switch (p.side) {
            case 'none': return 'center';
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

        // Lined up over its reference, a surface has no side to point an arrow from.
        const arrowCenter = p.side === 'none' ? null : applyArrow(state, p);
        floating.style.setProperty('--bit-bh-transform-origin', transformOrigin(p, arrowCenter));

        floating.setAttribute('data-side', p.side);
        floating.setAttribute('data-align', p.align);
        floating.setAttribute('data-anchor', p.side === 'none' ? 'none' : p.side + (p.align !== 'center' ? ' ' + p.align : ''));

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

    function isSecondaryPressOf(layer: Layer, target: EventTarget | null) {
        for (const id of layer.options.secondaryIds || []) {
            const el = document.getElementById(id);
            if (el && el.contains(target as Node)) return true;
        }
        return false;
    }

    function processOutside(target: EventTarget | null, reason: string, secondary = false) {
        for (let i = layerStack.length - 1; i >= 0; i--) {
            const layer = layerStack[i];
            if (!isInLayerBranch(layer, target) && layer.options[reason === 'focus-outside' ? 'focusOutside' : 'outside']
                && !(secondary && isSecondaryPressOf(layer, target))) {
                dismissLayer(layer, reason);
            }
            if (layer.options.modal) break;
        }
    }

    // The element a pointer press is on, until the press ends. Safari does not focus a button it presses, so the
    // focus moves to the button's focusable ancestor - outside a layer whose trigger the button is. A focus that
    // a press moves to an ancestor of the pressed element is judged by where the press is, which the press itself
    // already was; focus going anywhere else (into a menu the press opened) is judged by itself.
    let pressTarget: EventTarget | null = null;

    function onLayerPressEnd() {
        pressTarget = null;
    }

    function onLayerPointerDown(e: PointerEvent) {
        pressTarget = e.target;
        if (!layerStack.length) return;
        if (e.pointerType === 'touch') {
            pendingTouchDismiss = e.target;
            return;
        }
        pendingTouchDismiss = null;
        // The press that opens a context menu: the right button, or Ctrl+click on Apple platforms.
        const secondary = e.button === 2 || (e.button === 0 && e.ctrlKey && isApple);
        processOutside(e.target, 'outside', secondary);
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
        const pressed = pressTarget;
        const toAncestor = isElement(pressed) && pressed.isConnected && isElement(e.target) && e.target !== pressed && e.target.contains(pressed);
        processOutside(toAncestor ? pressed : e.target, 'focus-outside');
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

    // The focusable element the last pointer press landed on. Safari does not focus a button it clicks, so a
    // scope a click opens would remember the button's focusable ancestor - or <body> - as the place to give the
    // focus back to; the press says where the user really was. Listened for from the start: the press that
    // opens the first scope comes before it (install).
    let lastPress: { el: HTMLElement; time: number } | null = null;

    function recordPress(e: PointerEvent) {
        const target = isElement(e.target) ? e.target.closest<HTMLElement>('button, a[href], input, select, textarea, summary, [tabindex]') : null;
        lastPress = target ? { el: target, time: performance.now() } : null;
    }

    /** Where the focus is - or, right after a press the browser did not move it for, the element pressed. */
    function focusOrigin() {
        const active = document.activeElement;
        const press = lastPress;
        if (!press || press.el === active || !press.el.isConnected || performance.now() - press.time > 3000) return active;
        if (press.el.matches(':disabled')) return active;
        return !active || active === document.body || active.contains(press.el) ? press.el : active;
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
                previous: focusOrigin(),
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

        /** Changes whether a live scope traps / loops focus, without re-running auto-focus. */
        updateScope(handle: number, trap: boolean, loop: boolean) {
            const scope = scopeStack.find((s) => s.id === handle);
            if (!scope) return;
            scope.trap = !!trap;
            scope.loop = !!loop;
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

    /**
     * Applies attribute changes the observer has queued but not yet delivered. A render
     * batch and an interop call can land in the same task (WebAssembly), so the exit a
     * just-rendered `hidden` should start may not have started yet.
     */
    function flushPresence() {
        const pending = presenceObserver?.takeRecords();
        if (pending?.length) onPresenceMutations(pending);
    }

    /** Resolves when the element's presence exit (if any) has finished. */
    function presenceDone(el: HTMLElement | null) {
        if (!el) return Promise.resolve();
        flushPresence();
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
        // Shift counts unless the combo names a symbol typed with it on some layout ("?" is Shift+/ on a US
        // keyboard, a digit is shifted on AZERTY): there the produced key already tells the combos apart. A
        // letter only changes case, which is compared away, so "mod+k" must not also fire on Mod+Shift+K.
        const shiftMatters = hk.key.length > 1 || hk.mods.has('shift') || hk.key.toUpperCase() !== hk.key;
        return e.ctrlKey === hk.mods.has('ctrl') && e.metaKey === hk.mods.has('meta')
            && e.altKey === hk.mods.has('alt') && (!shiftMatters || e.shiftKey === hk.mods.has('shift'));
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
                // While typing, only combos with Ctrl/Meta fire: each combo is judged on its
                // own, so a bare "/" next to "mod+k" never swallows a typed character.
                const typing = !allowInInputs && isEditable(e.target);
                if (parsed.some((h) => (!typing || h.mods.has('ctrl') || h.mods.has('meta')) && hotkeyMatches(e, h))) {
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
         * same collection, like child tree items, excluded, and so is what assistive technology
         * does not read either: an aria-hidden check mark or chevron) feeds typeahead.
         */
        order(key: string) {
            const selector = `[data-bit-bh-item="${CSS.escape(key)}"]`;
            const excluded = `${selector}, [aria-hidden="true"]`;
            const ownText = (el: Element) => {
                if (!el.querySelector(excluded)) return el.textContent ?? '';
                const clone = el.cloneNode(true) as Element;
                clone.querySelectorAll(excluded).forEach((nested) => nested.remove());
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

    // A scroll container's padding-top (where its first item starts) and its scroll-padding (how far from the
    // edges an item scrolled into view stays).
    function scrollInsets(el: Element) {
        const style = getComputedStyle(el);
        return {
            paddingTop: parseFloat(style.paddingTop) || 0,
            top: parseFloat(style.scrollPaddingTop) || 0,
            bottom: parseFloat(style.scrollPaddingBottom) || 0,
        };
    }

    const dom = {
        scrollIntoView(id: ElementLike, block?: ScrollLogicalPosition) {
            const el = byId(id);
            if (!el) return;
            const parent = scrollParent(el);
            if (parent) {
                // What the container shows is its padding box (inside the borders), less its scroll-padding.
                const insets = scrollInsets(parent);
                const top = parent.getBoundingClientRect().top + parent.clientTop;
                const visibleTop = top + insets.top;
                const visibleBottom = top + parent.clientHeight - insets.bottom;
                const er = el.getBoundingClientRect();
                if (er.top < visibleTop) parent.scrollTop -= visibleTop - er.top;
                else if (er.bottom > visibleBottom) parent.scrollTop += er.bottom - visibleBottom;
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
            const insets = scrollInsets(container);
            const top = insets.paddingTop + index * itemSize;
            if (top - insets.top < container.scrollTop) container.scrollTop = top - insets.top;
            else if (top + itemSize + insets.bottom > container.scrollTop + container.clientHeight)
                container.scrollTop = top + itemSize + insets.bottom - container.clientHeight;
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
        // Narrows a file input's files to those at `indices` (a removed or rejected file is then not posted with
        // the form). Setting files fires no change event, and Blazor's InputFile keeps its own references.
        keepFiles(id: ElementLike, indices: number[]) {
            const input = byId(id) as HTMLInputElement | null;
            if (!input || !input.files || typeof DataTransfer === 'undefined') return;
            const transfer = new DataTransfer();
            for (const i of indices) {
                const file = input.files[i];
                if (file) transfer.items.add(file);
            }
            try { input.files = transfer.files; } catch { /* read-only in very old browsers */ }
        },
        requestSubmit(id: ElementLike) {
            const form = byId(id)?.closest('form');
            if (!form) return;
            if (typeof form.requestSubmit === 'function') form.requestSubmit();
            else form.submit();
        },
        async copy(text: string) {
            try {
                await navigator.clipboard.writeText(text);
                return true;
            } catch {
                // Older browsers, or a page without clipboard permission: the selection-based fallback.
                const area = document.createElement('textarea');
                area.value = text;
                area.setAttribute('readonly', '');
                area.setAttribute('style', 'position:fixed;top:0;left:0;opacity:0;pointer-events:none');
                document.body.appendChild(area);
                const previous = document.activeElement as HTMLElement | null;
                area.select();
                let ok = false;
                try { ok = document.execCommand('copy'); } catch { ok = false; }
                area.remove();
                previous?.focus?.({ preventScroll: true });
                return ok;
            }
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

    // Which announcement a region holds: the timers of an earlier one - even of the same text - must not
    // set or clear the region once a newer announcement has taken it over.
    const liveGeneration = new WeakMap<Element, number>();

    const announcer = {
        announce(message: string, politeness: string, clearAfterMs: number) {
            const region = liveRegion(politeness);
            if (!region) return;
            const generation = (liveGeneration.get(region) ?? 0) + 1;
            liveGeneration.set(region, generation);
            region.textContent = '';
            // A short delay ensures screen readers notice the change even when the
            // same message is announced twice in a row.
            setTimeout(() => {
                if (liveGeneration.get(region) !== generation) return;
                region.textContent = message;
                if (clearAfterMs > 0) setTimeout(() => { if (liveGeneration.get(region) === generation) region.textContent = ''; }, clearAfterMs);
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
        options: { orientation?: string; inverted?: boolean; rtl?: boolean };
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
        // .NET draws the thumbs by its own direction (Dir / DirectionProvider), so mirror by that one.
        if (!vertical && state.options.rtl) f = 1 - f;
        if (state.options.inverted) f = 1 - f;
        return Math.max(0, Math.min(1, f));
    }

    const slider = {
        /** options: { orientation, inverted, rtl }. Calls OnSliderPointer(phase, fraction) on .NET. */
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

    // ─── scrub ───────────────────────────────────────────────────────────────────
    //
    // Number-field scrub areas: dragging across the element changes the value one step per
    // `sensitivity` pixels. Movement is accumulated here and sent as whole steps, one call in flight.

    interface ScrubState {
        el: HTMLElement;
        dotnet: DotNetRef;
        options: { sensitivity?: number; vertical?: boolean };
        onPointerDown: (e: PointerEvent) => void;
    }

    const scrubs = new Map<number, ScrubState>();
    let scrubSeq = 0;

    const scrub = {
        attach(dotnet: DotNetRef, id: string, options: ScrubState['options']) {
            const el = byId(id);
            if (!el) return 0;
            const handle = ++scrubSeq;
            const state: ScrubState = { el, dotnet, options: options || {}, onPointerDown: null };
            state.onPointerDown = (e: PointerEvent) => {
                if (e.button !== 0 || el.hasAttribute('data-disabled')) return;
                e.preventDefault();
                const sensitivity = Math.max(1, state.options.sensitivity || 2);
                let carry = 0;
                let pending = 0;
                let large = false;
                let busy = false;
                let ended = false;
                const flush = () => {
                    if (busy || pending === 0) return;
                    const steps = pending;
                    pending = 0;
                    busy = true;
                    dotnet.invokeMethodAsync('OnScrub', 'move', steps, large).catch(() => { }).finally(() => {
                        busy = false;
                        if (pending !== 0) flush();
                        else if (ended) dotnet.invokeMethodAsync('OnScrub', 'end', 0, false).catch(() => { });
                    });
                };
                const move = (ev: PointerEvent) => {
                    carry += state.options.vertical ? -ev.movementY : (isRtl(el) ? -ev.movementX : ev.movementX);
                    large = ev.shiftKey;
                    const steps = Math.trunc(carry / sensitivity);
                    if (steps === 0) return;
                    carry -= steps * sensitivity;
                    pending += steps;
                    flush();
                };
                const up = () => {
                    el.removeEventListener('pointermove', move);
                    el.removeEventListener('pointerup', up);
                    el.removeEventListener('pointercancel', up);
                    el.removeAttribute('data-scrubbing');
                    ended = true;
                    if (!busy && pending === 0) dotnet.invokeMethodAsync('OnScrub', 'end', 0, false).catch(() => { });
                };
                try { el.setPointerCapture(e.pointerId); } catch { /* ignore */ }
                el.setAttribute('data-scrubbing', '');
                dotnet.invokeMethodAsync('OnScrub', 'start', 0, false).catch(() => { });
                el.addEventListener('pointermove', move);
                el.addEventListener('pointerup', up);
                el.addEventListener('pointercancel', up);
            };
            el.addEventListener('pointerdown', state.onPointerDown);
            scrubs.set(handle, state);
            return handle;
        },
        detach(handle: number) {
            const state = scrubs.get(handle);
            if (!state) return;
            scrubs.delete(handle);
            state.el.removeEventListener('pointerdown', state.onPointerDown);
        },
    };

    // ─── indicator ───────────────────────────────────────────────────────────────
    //
    // Selection indicators: an element that follows the selected item of its parent (a tab
    // underline, a segmented-control pill). The selected item is measured relative to the parent
    // and written to CSS variables on the indicator, whenever the selection, a size or the
    // parent's scroll position changes.

    interface IndicatorState {
        el: HTMLElement;
        container: HTMLElement;
        selector: string;
        frame: number;
        target: Element | null;
        last: { left: number; top: number } | null;
        mutations: MutationObserver;
        sizes: ResizeObserver | null;
        onScroll: () => void;
    }

    const indicators = new Map<number, IndicatorState>();
    let indicatorSeq = 0;

    function measureIndicator(state: IndicatorState) {
        state.frame = 0;
        const { el, container } = state;
        let target: Element | null = null;
        try {
            target = Array.from(container.querySelectorAll(state.selector)).find((c) => c !== el && !el.contains(c)) || null;
        } catch { target = null; }
        if (target !== state.target && state.sizes) {
            if (state.target) state.sizes.unobserve(state.target);
            if (target) state.sizes.observe(target);
        }
        state.target = target;
        if (!target) {
            el.setAttribute('data-hidden', '');
            return;
        }
        el.removeAttribute('data-hidden');
        const cr = container.getBoundingClientRect();
        const r = target.getBoundingClientRect();
        // In the container's padding box, scrolled content included: the indicator is positioned inside it.
        const left = r.left - cr.left - container.clientLeft + container.scrollLeft;
        const top = r.top - cr.top - container.clientTop + container.scrollTop;
        if (state.last && (state.last.left !== left || state.last.top !== top)) {
            const dx = left - state.last.left;
            const dy = top - state.last.top;
            const direction = Math.abs(dx) >= Math.abs(dy) ? (dx > 0 ? 'right' : 'left') : (dy > 0 ? 'down' : 'up');
            el.setAttribute('data-activation-direction', direction);
        }
        state.last = { left, top };
        el.style.setProperty('--bit-bh-indicator-left', `${left}px`);
        el.style.setProperty('--bit-bh-indicator-top', `${top}px`);
        el.style.setProperty('--bit-bh-indicator-width', `${r.width}px`);
        el.style.setProperty('--bit-bh-indicator-height', `${r.height}px`);
        // Measured once before transitions apply, so it doesn't slide in from 0 on the first paint.
        if (!el.hasAttribute('data-measured')) requestAnimationFrame(() => el.setAttribute('data-measured', ''));
    }

    function scheduleIndicator(state: IndicatorState) {
        if (!state.frame) state.frame = requestAnimationFrame(() => measureIndicator(state));
    }

    const indicator = {
        attach(id: string, selector: string) {
            const el = byId(id);
            const container = el?.parentElement;
            if (!el || !container) return 0;
            const handle = ++indicatorSeq;
            const state: IndicatorState = {
                el, container, selector, frame: 0, target: null, last: null, mutations: null, sizes: null, onScroll: null,
            };
            state.mutations = new MutationObserver(() => scheduleIndicator(state));
            state.mutations.observe(container, {
                subtree: true, childList: true, attributes: true,
                attributeFilter: ['aria-selected', 'aria-checked', 'aria-pressed', 'aria-current', 'data-state', 'class'],
            });
            if (typeof ResizeObserver !== 'undefined') {
                state.sizes = new ResizeObserver(() => scheduleIndicator(state));
                state.sizes.observe(container);
            }
            state.onScroll = () => scheduleIndicator(state);
            container.addEventListener('scroll', state.onScroll, { passive: true });
            window.addEventListener('resize', state.onScroll, { passive: true });
            indicators.set(handle, state);
            measureIndicator(state);
            return handle;
        },
        update(handle: number, selector: string) {
            const state = indicators.get(handle);
            if (!state) return;
            state.selector = selector;
            scheduleIndicator(state);
        },
        detach(handle: number) {
            const state = indicators.get(handle);
            if (!state) return;
            indicators.delete(handle);
            if (state.frame) cancelAnimationFrame(state.frame);
            state.mutations.disconnect();
            state.sizes?.disconnect();
            state.container.removeEventListener('scroll', state.onScroll);
            window.removeEventListener('resize', state.onScroll);
        },
    };

    // ─── carousel ────────────────────────────────────────────────────────────────
    //
    // Carousel swipes: a pointer drag on the slides follows the pointer (--bit-bh-carousel-drag,
    // data-dragging) and, released far or fast enough, asks .NET for the next / previous slides.

    interface CarouselState { el: HTMLElement; onPointerDown: (e: PointerEvent) => void; }

    const carousels = new Map<number, CarouselState>();
    let carouselSeq = 0;

    const carousel = {
        attach(dotnet: DotNetRef, id: string, vertical: boolean) {
            const el = byId(id);
            if (!el) return 0;
            const handle = ++carouselSeq;
            const onPointerDown = (e: PointerEvent) => {
                if (e.button !== 0 || !isElement(e.target)) return;
                if (e.target.closest('input, textarea, select, [contenteditable]:not([contenteditable="false"]), [data-bit-bh-swipe-ignore]')) return;
                const start = vertical ? e.clientY : e.clientX;
                const across0 = vertical ? e.clientX : e.clientY;
                const time = performance.now();
                let moved = false;
                let delta = 0;
                const move = (ev: PointerEvent) => {
                    delta = (vertical ? ev.clientY : ev.clientX) - start;
                    if (!moved) {
                        if (Math.abs(delta) < 6) return;
                        if (Math.abs((vertical ? ev.clientX : ev.clientY) - across0) > Math.abs(delta)) { end(null); return; }
                        moved = true;
                        try { el.setPointerCapture(ev.pointerId); } catch { /* ignore */ }
                        el.setAttribute('data-dragging', '');
                    }
                    el.style.setProperty('--bit-bh-carousel-drag', `${delta}px`);
                };
                const end = (ev: PointerEvent | null) => {
                    el.removeEventListener('pointermove', move);
                    el.removeEventListener('pointerup', end);
                    el.removeEventListener('pointercancel', cancel);
                    el.removeAttribute('data-dragging');
                    el.style.removeProperty('--bit-bh-carousel-drag');
                    if (!moved || !ev) return;
                    const suppress = (c: Event) => { c.preventDefault(); c.stopPropagation(); };
                    el.addEventListener('click', suppress, { capture: true, once: true });
                    setTimeout(() => el.removeEventListener('click', suppress, { capture: true }), 0);
                    const velocity = Math.abs(delta) / Math.max(1, performance.now() - time);
                    if (Math.abs(delta) < Math.min(80, el.clientWidth / 4) && velocity < 0.3) return;
                    // Dragging toward the start shows what comes next; mirrored right to left.
                    let direction = delta < 0 ? 1 : -1;
                    if (!vertical && isRtl(el)) direction = -direction;
                    dotnet.invokeMethodAsync('OnCarouselSwipe', direction).catch(() => { });
                };
                const cancel = () => { moved = false; end(null); };
                el.addEventListener('pointermove', move);
                el.addEventListener('pointerup', end);
                el.addEventListener('pointercancel', cancel);
            };
            el.addEventListener('pointerdown', onPointerDown);
            carousels.set(handle, { el, onPointerDown });
            return handle;
        },
        detach(handle: number) {
            const state = carousels.get(handle);
            if (!state) return;
            carousels.delete(handle);
            state.el.removeEventListener('pointerdown', state.onPointerDown);
        },
    };

    // ─── toasts ──────────────────────────────────────────────────────────────────
    //
    // Toast lists: stack layout (index, offset and height of every toast as CSS variables, so a
    // collapsed stack is plain CSS), the expanded state (hovered or focused), pausing while the page
    // is hidden, and swipe-to-dismiss. .NET hears only about a swipe that dismisses and pause changes.

    interface ToastsOptions { swipe?: string | null; threshold?: number; gap?: number; }

    interface ToastsState {
        list: HTMLElement;
        dotnet: DotNetRef;
        options: ToastsOptions;
        frame: number;
        expanded: boolean;
        hidden: boolean;
        mutations: MutationObserver;
        sizes: ResizeObserver | null;
        observed: Set<Element>;
        cleanup: (() => void)[];
    }

    const toastLists = new Map<number, ToastsState>();
    let toastsSeq = 0;

    function toastItems(state: ToastsState) {
        return Array.from(state.list.querySelectorAll<HTMLElement>('[data-bit-bh-toast]'));
    }

    function layoutToasts(state: ToastsState) {
        state.frame = 0;
        const items = toastItems(state);
        const gap = state.options.gap ?? 8;
        let offset = 0;
        let frontHeight = 0;
        // The toaster appends: the newest toast is the last one, and the front of the stack.
        for (let i = items.length - 1, index = 0; i >= 0; i--, index++) {
            const toast = items[i];
            const height = toast.offsetHeight;
            toast.style.setProperty('--bit-bh-toast-index', String(index));
            toast.style.setProperty('--bit-bh-toast-offset', `${offset}px`);
            toast.style.setProperty('--bit-bh-toast-height', `${height}px`);
            toast.setAttribute('data-index', String(index));
            if (index === 0) { toast.setAttribute('data-front', ''); frontHeight = height; }
            else toast.removeAttribute('data-front');
            offset += height + gap;
            if (state.sizes && !state.observed.has(toast)) { state.observed.add(toast); state.sizes.observe(toast); }
        }
        for (const el of Array.from(state.observed)) {
            if (!el.isConnected) { state.observed.delete(el); state.sizes?.unobserve(el); }
        }
        state.list.style.setProperty('--bit-bh-toast-count', String(items.length));
        state.list.style.setProperty('--bit-bh-toast-front-height', `${frontHeight}px`);
    }

    function scheduleToastLayout(state: ToastsState) {
        if (!state.frame) state.frame = requestAnimationFrame(() => layoutToasts(state));
    }

    function setToastsPause(state: ToastsState, expanded: boolean, hidden: boolean) {
        if (state.expanded === expanded && state.hidden === hidden) return;
        state.expanded = expanded;
        state.hidden = hidden;
        if (expanded) state.list.setAttribute('data-expanded', '');
        else state.list.removeAttribute('data-expanded');
        state.dotnet.invokeMethodAsync('OnToastsPause', expanded, hidden).catch(() => { });
    }

    function startToastSwipe(state: ToastsState, e: PointerEvent) {
        const direction = state.options.swipe;
        if (!direction || e.button !== 0 || !isElement(e.target)) return;
        const toast = e.target.closest<HTMLElement>('[data-bit-bh-toast]');
        if (!toast || !state.list.contains(toast)) return;
        if (e.target.closest('input, textarea, select, [contenteditable]:not([contenteditable="false"]), [data-bit-bh-swipe-ignore]')) return;

        const horizontal = direction === 'left' || direction === 'right';
        const sign = direction === 'right' || direction === 'down' ? 1 : -1;
        const startX = e.clientX;
        const startY = e.clientY;
        const startTime = performance.now();
        let moved = false;
        let along = 0;

        const setMove = (dx: number, dy: number) => {
            toast.style.setProperty('--bit-bh-toast-swipe-move-x', `${dx}px`);
            toast.style.setProperty('--bit-bh-toast-swipe-move-y', `${dy}px`);
        };
        const clearMove = () => {
            toast.style.removeProperty('--bit-bh-toast-swipe-move-x');
            toast.style.removeProperty('--bit-bh-toast-swipe-move-y');
        };
        const move = (ev: PointerEvent) => {
            const raw = horizontal ? ev.clientX - startX : ev.clientY - startY;
            if (!moved) {
                // A press that wanders a little is still a press; one across the other axis is a scroll.
                const across = horizontal ? ev.clientY - startY : ev.clientX - startX;
                if (Math.abs(raw) < 6) return;
                if (Math.abs(across) > Math.abs(raw)) { end(null); return; }
                moved = true;
                try { toast.setPointerCapture(ev.pointerId); } catch { /* ignore */ }
            }
            along = raw * sign;
            // Against the swipe direction the toast barely follows.
            const shown = (along >= 0 ? along : along / 6) * sign;
            toast.setAttribute('data-swipe', 'move');
            setMove(horizontal ? shown : 0, horizontal ? 0 : shown);
        };
        const end = (ev: PointerEvent | null) => {
            toast.removeEventListener('pointermove', move);
            toast.removeEventListener('pointerup', end);
            toast.removeEventListener('pointercancel', cancel);
            if (!moved || !ev) return;
            const velocity = along / Math.max(1, performance.now() - startTime);
            // A drag that moved is not a click on what it started on.
            // The click (if any) follows the pointerup in the same task; the guard is gone right after.
            const suppress = (c: Event) => { c.preventDefault(); c.stopPropagation(); };
            toast.addEventListener('click', suppress, { capture: true, once: true });
            setTimeout(() => toast.removeEventListener('click', suppress, { capture: true }), 0);
            if (along >= (state.options.threshold ?? 50) || velocity > 0.11) {
                const shown = Math.max(along, 0) * sign;
                toast.style.setProperty('--bit-bh-toast-swipe-end-x', `${horizontal ? shown : 0}px`);
                toast.style.setProperty('--bit-bh-toast-swipe-end-y', `${horizontal ? 0 : shown}px`);
                clearMove();
                toast.setAttribute('data-swipe', 'end');
                state.dotnet.invokeMethodAsync('OnToastSwiped', toast.id).catch(() => { });
            } else {
                clearMove();
                toast.setAttribute('data-swipe', 'cancel');
            }
        };
        const cancel = () => {
            clearMove();
            if (moved) toast.setAttribute('data-swipe', 'cancel');
            moved = false;
            end(null);
        };
        toast.addEventListener('pointermove', move);
        toast.addEventListener('pointerup', end);
        toast.addEventListener('pointercancel', cancel);
    }

    const toasts = {
        attach(dotnet: DotNetRef, listId: string, options: ToastsOptions) {
            const list = byId(listId);
            if (!list) return 0;
            const handle = ++toastsSeq;
            const state: ToastsState = {
                list, dotnet, options: options || {}, frame: 0, expanded: false, hidden: document.hidden,
                mutations: null, sizes: null, observed: new Set(), cleanup: [],
            };
            state.mutations = new MutationObserver(() => scheduleToastLayout(state));
            state.mutations.observe(list, { childList: true, subtree: true, attributes: true, attributeFilter: ['data-state'] });
            if (typeof ResizeObserver !== 'undefined') state.sizes = new ResizeObserver(() => scheduleToastLayout(state));

            const listen = <K extends keyof DocumentEventMap>(target: EventTarget, name: K | string, fn: EventListener) => {
                target.addEventListener(name, fn);
                state.cleanup.push(() => target.removeEventListener(name, fn));
            };
            let hovered = false;
            let focused = false;
            const update = () => setToastsPause(state, hovered || focused, document.hidden);
            listen(list, 'pointerenter', ((e: PointerEvent) => { if (e.pointerType !== 'touch') { hovered = true; update(); } }) as EventListener);
            listen(list, 'pointerleave', () => { hovered = false; update(); });
            listen(list, 'focusin', () => { focused = true; update(); });
            listen(list, 'focusout', ((e: FocusEvent) => {
                if (e.relatedTarget && list.contains(e.relatedTarget as Node)) return;
                focused = false;
                update();
            }) as EventListener);
            listen(document, 'visibilitychange', update);
            listen(list, 'pointerdown', ((e: PointerEvent) => startToastSwipe(state, e)) as EventListener);

            toastLists.set(handle, state);
            layoutToasts(state);
            if (state.hidden) dotnet.invokeMethodAsync('OnToastsPause', false, true).catch(() => { });
            return handle;
        },
        update(handle: number, options: ToastsOptions) {
            const state = toastLists.get(handle);
            if (!state) return;
            state.options = options || {};
            scheduleToastLayout(state);
        },
        detach(handle: number) {
            const state = toastLists.get(handle);
            if (!state) return;
            toastLists.delete(handle);
            if (state.frame) cancelAnimationFrame(state.frame);
            state.mutations.disconnect();
            state.sizes?.disconnect();
            for (const fn of state.cleanup) fn();
        },
    };

    // ─── area ────────────────────────────────────────────────────────────────────
    //
    // Two-dimensional pointer areas (a color area): a press anywhere moves the thumb there and a drag
    // follows the pointer. Fractions are sent (x from the left, y from the bottom), one call in flight.

    interface AreaState { el: HTMLElement; onPointerDown: (e: PointerEvent) => void; }

    const areas = new Map<number, AreaState>();
    let areaSeq = 0;

    const area = {
        attach(dotnet: DotNetRef, id: string) {
            const el = byId(id);
            if (!el) return 0;
            const handle = ++areaSeq;
            let busy = false;
            let pending: [string, number, number] | null = null;
            const fractions = (e: PointerEvent): [number, number] => {
                const r = el.getBoundingClientRect();
                const x = r.width > 0 ? (e.clientX - r.left) / r.width : 0;
                const y = r.height > 0 ? (r.bottom - e.clientY) / r.height : 0;
                return [Math.max(0, Math.min(1, x)), Math.max(0, Math.min(1, y))];
            };
            const send = (phase: string, x: number, y: number) => {
                if (phase === 'move' && busy) { pending = [phase, x, y]; return; }
                busy = true;
                dotnet.invokeMethodAsync('OnAreaPointer', phase, x, y).catch(() => { }).finally(() => {
                    busy = false;
                    if (pending) { const [p, px, py] = pending; pending = null; send(p, px, py); }
                });
            };
            const onPointerDown = (e: PointerEvent) => {
                if (e.button !== 0 || el.hasAttribute('data-disabled')) return;
                e.preventDefault();
                el.querySelector<HTMLElement>('[role="slider"]')?.focus({ preventScroll: true });
                try { el.setPointerCapture(e.pointerId); } catch { /* ignore */ }
                send('start', ...fractions(e));
                const move = (ev: PointerEvent) => send('move', ...fractions(ev));
                const up = (ev: PointerEvent) => {
                    el.removeEventListener('pointermove', move);
                    el.removeEventListener('pointerup', up);
                    el.removeEventListener('pointercancel', up);
                    pending = null;
                    send('end', ...fractions(ev));
                };
                el.addEventListener('pointermove', move);
                el.addEventListener('pointerup', up);
                el.addEventListener('pointercancel', up);
            };
            el.addEventListener('pointerdown', onPointerDown);
            areas.set(handle, { el, onPointerDown });
            return handle;
        },
        detach(handle: number) {
            const state = areas.get(handle);
            if (!state) return;
            areas.delete(handle);
            state.el.removeEventListener('pointerdown', state.onPointerDown);
        },
    };

    // ─── resizer ─────────────────────────────────────────────────────────────────
    //
    // Column resizers: a drag reports the new width (the width when pressed, read from
    // data-bit-bh-resizer-width, plus the distance moved; mirrored right to left), one call in flight.

    interface ResizerState { el: HTMLElement; onPointerDown: (e: PointerEvent) => void; }

    const resizers = new Map<number, ResizerState>();
    let resizerSeq = 0;

    const resizer = {
        attach(dotnet: DotNetRef, id: string, rtl: boolean) {
            const el = byId(id);
            if (!el) return 0;
            const handle = ++resizerSeq;
            const onPointerDown = (e: PointerEvent) => {
                if (e.button !== 0) return;
                e.preventDefault();
                e.stopPropagation();
                el.focus({ preventScroll: true });
                const startX = e.clientX;
                const startWidth = parseFloat(el.getAttribute('data-bit-bh-resizer-width') || '0') || 0;
                let busy = false;
                let pending: number | null = null;
                const send = (phase: string, width: number) => {
                    if (phase === 'move' && busy) { pending = width; return; }
                    busy = true;
                    dotnet.invokeMethodAsync('OnResize', phase, width).catch(() => { }).finally(() => {
                        busy = false;
                        if (pending !== null) { const w = pending; pending = null; send('move', w); }
                    });
                };
                try { el.setPointerCapture(e.pointerId); } catch { /* ignore */ }
                el.setAttribute('data-resizing', '');
                const move = (ev: PointerEvent) => send('move', startWidth + (rtl ? startX - ev.clientX : ev.clientX - startX));
                const up = () => {
                    el.removeEventListener('pointermove', move);
                    el.removeEventListener('pointerup', up);
                    el.removeEventListener('pointercancel', up);
                    el.removeAttribute('data-resizing');
                    pending = null;
                    send('end', 0);
                };
                el.addEventListener('pointermove', move);
                el.addEventListener('pointerup', up);
                el.addEventListener('pointercancel', up);
            };
            el.addEventListener('pointerdown', onPointerDown);
            resizers.set(handle, { el, onPointerDown });
            return handle;
        },
        detach(handle: number) {
            const state = resizers.get(handle);
            if (!state) return;
            resizers.delete(handle);
            state.el.removeEventListener('pointerdown', state.onPointerDown);
        },
    };

    // ─── landmarks ───────────────────────────────────────────────────────────────
    //
    // F6 / Shift+F6 move the focus from landmark to landmark (header, nav, main, aside, footer, a
    // named region or form, search), as React Aria's useLandmark and desktop apps do. A landmark gets
    // back the element that last had the focus in it; one never visited is focused itself.

    const LANDMARKS = [
        'main', '[role="main"]', 'nav', '[role="navigation"]', 'aside', '[role="complementary"]',
        'body > header', '[role="banner"]', 'body > footer', '[role="contentinfo"]', '[role="search"]', 'search',
        'section[aria-label]', 'section[aria-labelledby]', '[role="region"][aria-label]', '[role="region"][aria-labelledby]',
        'form[aria-label]', 'form[aria-labelledby]',
    ].join(',');

    let landmarkUsers = 0;
    const lastFocusedIn = new WeakMap<Element, Element>();

    function landmarkList() {
        return Array.from(document.querySelectorAll<HTMLElement>(LANDMARKS))
            .filter((el) => isVisible(el) && !el.closest('[inert],[aria-hidden="true"]'));
    }

    function onLandmarkFocusIn(e: FocusEvent) {
        if (!isElement(e.target)) return;
        for (let el = e.target.parentElement; el; el = el.parentElement) {
            if (el.matches(LANDMARKS)) { lastFocusedIn.set(el, e.target); return; }
        }
    }

    function onLandmarkKeyDown(e: KeyboardEvent) {
        if (e.key !== 'F6' || e.ctrlKey || e.metaKey || e.altKey || e.defaultPrevented) return;
        const list = landmarkList();
        if (list.length === 0) return;
        const active = document.activeElement;
        // The innermost landmark around the focus.
        let current = -1;
        list.forEach((el, i) => { if (active && el.contains(active)) current = i; });
        const step = e.shiftKey ? -1 : 1;
        const next = list[(current + step + list.length) % list.length];
        if (!next) return;
        e.preventDefault();
        const remembered = lastFocusedIn.get(next);
        if (remembered && next.contains(remembered) && isFocusable(remembered)) {
            focusElement(remembered);
            return;
        }
        if (!next.hasAttribute('tabindex')) {
            next.setAttribute('tabindex', '-1');
            next.addEventListener('blur', () => next.removeAttribute('tabindex'), { once: true });
        }
        next.focus();
    }

    const landmarks = {
        enable() {
            if (landmarkUsers++ > 0) return;
            document.addEventListener('keydown', onLandmarkKeyDown, false);
            document.addEventListener('focusin', onLandmarkFocusIn, true);
        },
        disable() {
            if (landmarkUsers === 0 || --landmarkUsers > 0) return;
            document.removeEventListener('keydown', onLandmarkKeyDown, false);
            document.removeEventListener('focusin', onLandmarkFocusIn, true);
        },
    };

    // ─── scroll spy ──────────────────────────────────────────────────────────────
    //
    // Tables of contents: which of the given sections are in view, from an IntersectionObserver.
    // .NET hears only when the active section(s) change.

    interface SpyState { observer: IntersectionObserver; visible: Map<string, number>; last: string; }

    const spies = new Map<number, SpyState>();
    let spySeq = 0;

    const scrollSpy = {
        attach(dotnet: DotNetRef, ids: string[], options: { rootMargin?: string; multiple?: boolean }) {
            if (typeof IntersectionObserver === 'undefined') return 0;
            const handle = ++spySeq;
            const state: SpyState = { observer: null, visible: new Map(), last: '#none' };
            const order = new Map(ids.map((id, i) => [id, i]));
            const report = () => {
                // In document order: the first section in view is the current one (or every one in view).
                const inView = Array.from(state.visible.keys()).sort((a, b) => (order.get(a) ?? 0) - (order.get(b) ?? 0));
                const active = options?.multiple ? inView : inView.slice(0, 1);
                const key = active.join(' ');
                if (key === state.last) return;
                state.last = key;
                dotnet.invokeMethodAsync('OnScrollSpy', active).catch(() => { });
            };
            state.observer = new IntersectionObserver((entries) => {
                for (const entry of entries) {
                    const id = (entry.target as HTMLElement).id;
                    if (entry.isIntersecting) state.visible.set(id, entry.intersectionRatio);
                    else state.visible.delete(id);
                }
                report();
            }, { rootMargin: options?.rootMargin || '0px 0px -60% 0px', threshold: [0, 1] });
            for (const id of ids) {
                const el = document.getElementById(id);
                if (el) state.observer.observe(el);
            }
            spies.set(handle, state);
            return handle;
        },
        detach(handle: number) {
            const state = spies.get(handle);
            if (!state) return;
            spies.delete(handle);
            state.observer.disconnect();
        },
    };

    // ─── rating ──────────────────────────────────────────────────────────────────
    //
    // Pointer preview and picking for ratings. The value under the pointer (an item, or the first half of one
    // when the rating allows halves) is worked out here, and .NET is only called when it changes.

    interface RatingState {
        el: HTMLElement;
        listeners: [string, EventListener][];
    }

    const ratings = new Map<number, RatingState>();
    let ratingSeq = 0;

    function ratingValueAt(root: HTMLElement, e: PointerEvent | MouseEvent) {
        const mode = root.getAttribute('data-bit-bh-rating');
        if (!mode || mode === 'off') return null;
        const item = isElement(e.target) ? e.target.closest<HTMLElement>('[data-bit-bh-rating-item]') : null;
        if (!item || !root.contains(item)) return null;
        const index = parseInt(item.getAttribute('data-bit-bh-rating-item'), 10);
        if (isNaN(index)) return null;
        if (mode !== 'half') return index + 1;
        const r = item.getBoundingClientRect();
        const fraction = r.width > 0 ? (e.clientX - r.left) / r.width : 1;
        const firstHalf = root.hasAttribute('data-bit-bh-rtl') ? fraction > 0.5 : fraction < 0.5;
        return index + (firstHalf ? 0.5 : 1);
    }

    const rating = {
        attach(dotnet: DotNetRef, id: string) {
            const el = byId(id);
            if (!el) return 0;
            const handle = ++ratingSeq;
            let last: number | null = null;
            const send = (phase: string, value: number) => dotnet.invokeMethodAsync('OnRatingPointer', phase, value).catch(() => { });
            const move = (e: PointerEvent) => {
                if (e.pointerType === 'touch') return;
                const value = ratingValueAt(el, e);
                if (value === null || value === last) return;
                last = value;
                send('hover', value);
            };
            const leave = () => {
                if (last === null) return;
                last = null;
                send('leave', 0);
            };
            const click = (e: MouseEvent) => {
                const value = ratingValueAt(el, e);
                if (value !== null) send('pick', value);
            };
            const listeners: [string, EventListener][] = [['pointermove', move as EventListener], ['pointerleave', leave], ['click', click as EventListener]];
            for (const [name, fn] of listeners) el.addEventListener(name, fn);
            ratings.set(handle, { el, listeners });
            return handle;
        },
        detach(handle: number) {
            const state = ratings.get(handle);
            if (!state) return;
            ratings.delete(handle);
            for (const [name, fn] of state.listeners) state.el.removeEventListener(name, fn);
        },
    };

    // ─── scrollArea ──────────────────────────────────────────────────────────────
    //
    // Custom scrollbars over a natively scrolling viewport whose own scrollbars are hidden. All of it runs
    // here, with no .NET call per scroll: thumb sizes and positions, visibility by type (hover / scroll /
    // auto / always), thumb drags, paging presses on the track, the wheel over a scrollbar, the overflow data
    // attributes and CSS variables, the corner and the viewport's tab stop. The parts carry the root's id in
    // their data-bit-bh-scroll-area-* attribute, so nested areas stay apart. A ResizeObserver and a
    // MutationObserver keep it right when the content, the parts or their sizes change. Every style this
    // writes is written in one animation frame; its own writes are taken off the attribute observer's queue.

    interface ScrollAxis {
        vertical: boolean;
        bar: HTMLElement | null;
        thumb: HTMLElement | null;
        /** The content overflows on this axis (by more than a rounding pixel). */
        overflow: boolean;
        visible: boolean;
        scrolling: boolean;
        /** How far the viewport scrolls on this axis (0 without overflow). */
        max: number;
        /** The track: the scrollbar's content box along the axis. */
        track: number;
        /** The thumb's rendered length (the CSS may add a minimum, a margin …). */
        thumbSize: number;
        /** Where the thumb sits along the track without its transform. */
        natural: number;
    }

    interface ScrollAreaState {
        root: HTMLElement;
        rootId: string;
        options: { type?: string; hideDelay?: number };
        viewport: HTMLElement | null;
        corner: HTMLElement | null;
        y: ScrollAxis;
        x: ScrollAxis;
        rtl: boolean;
        hovering: boolean;
        barHovering: boolean;
        dragging: boolean;
        scrolling: boolean;
        revealed: boolean;
        hideTimer: number;
        scrollEndTimer: number;
        frame: number;
        measure: boolean;
        checkFocus: boolean;
        partsStale: boolean;
        ownsTabIndex: boolean;
        lastTop: number;
        lastLeft: number;
        resize: ResizeObserver | null;
        content: MutationObserver;
        parts: MutationObserver;
        observed: Element[];
        wheelBars: HTMLElement[];
        listeners: [string, EventListener, boolean | AddEventListenerOptions][];
        endPress: (() => void) | null;
        detached: boolean;
    }

    const scrollAreas = new Map<number, ScrollAreaState>();
    let scrollAreaSeq = 0;
    let scrollAreaSheetAdded = false;
    const SCROLL_THUMB_MIN = 18;
    const SCROLL_END_DELAY = 100;

    // Older WebKit and Blink hide an element's scrollbars only through ::-webkit-scrollbar, which no inline
    // style reaches: one shared rule, in a constructed sheet where supported (no inline <style> for a CSP to refuse).
    function addScrollAreaSheet() {
        if (scrollAreaSheetAdded || typeof document === 'undefined') return;
        scrollAreaSheetAdded = true;
        const css = '[data-bit-bh-scroll-area-viewport]::-webkit-scrollbar{display:none}';
        try {
            const sheet = new CSSStyleSheet();
            sheet.replaceSync(css);
            document.adoptedStyleSheets = [...document.adoptedStyleSheets, sheet];
        } catch {
            const style = document.createElement('style');
            style.setAttribute('data-bit-bh-scroll-area-sheet', '');
            style.textContent = css;
            document.head.appendChild(style);
        }
    }

    function scrollAreaSelector(state: ScrollAreaState, part: string) {
        return `[data-bit-bh-scroll-area-${part}="${CSS.escape(state.rootId)}"]`;
    }

    function setScrollAreaFlag(el: Element | null, name: string, on: boolean) {
        if (!el) return;
        if (on) { if (!el.hasAttribute(name)) el.setAttribute(name, ''); }
        else if (el.hasAttribute(name)) el.removeAttribute(name);
    }

    function setScrollAreaStyle(el: HTMLElement | null, name: string, value: string) {
        if (el && el.style.getPropertyValue(name) !== value) {
            if (value) el.style.setProperty(name, value);
            else el.style.removeProperty(name);
        }
    }

    /** The area's own scrollbar the node is in (not a nested area's). */
    function ownScrollbar(state: ScrollAreaState, node: EventTarget | null) {
        const bar = isElement(node) ? node.closest<HTMLElement>('[data-bit-bh-scroll-area-scrollbar]') : null;
        return bar && bar.getAttribute('data-bit-bh-scroll-area-scrollbar') === state.rootId ? bar : null;
    }

    function scrollAxisOf(state: ScrollAreaState, bar: HTMLElement | null) {
        return !bar ? null : bar === state.y.bar ? state.y : bar === state.x.bar ? state.x : null;
    }

    function scheduleScrollArea(state: ScrollAreaState, measure = false) {
        if (measure) state.measure = true;
        if (state.frame || state.detached) return;
        state.frame = requestAnimationFrame(() => {
            state.frame = 0;
            flushScrollArea(state);
        });
    }

    function flushScrollArea(state: ScrollAreaState) {
        if (state.detached) return;
        if (state.partsStale || (state.viewport && !state.viewport.isConnected)) resolveScrollAreaParts(state);
        if (state.measure) {
            state.measure = false;
            measureScrollArea(state);
        }
        positionScrollArea(state);
        // The writes above are this runtime's own: they are no news to the attribute observer.
        state.parts.takeRecords();
    }

    function resolveScrollAreaParts(state: ScrollAreaState) {
        const root = state.root;
        state.partsStale = false;
        state.viewport = root.querySelector<HTMLElement>(scrollAreaSelector(state, 'viewport'));
        state.corner = root.querySelector<HTMLElement>(scrollAreaSelector(state, 'corner'));
        state.y.bar = null;
        state.x.bar = null;
        for (const bar of Array.from(root.querySelectorAll<HTMLElement>(scrollAreaSelector(state, 'scrollbar')))) {
            const axis = bar.getAttribute('data-orientation') === 'horizontal' ? state.x : state.y;
            if (!axis.bar) axis.bar = bar;
        }
        for (const axis of [state.y, state.x]) {
            axis.thumb = axis.bar ? axis.bar.querySelector<HTMLElement>(scrollAreaSelector(state, 'thumb')) : null;
            if (!axis.bar) { axis.visible = false; axis.scrolling = false; }
        }

        // A re-render that rewrites a part's style drops what was written here; an attribute the component
        // changes (Focusable, Orientation) changes what is measured.
        state.parts.disconnect();
        for (const el of [root, state.viewport, state.y.bar, state.y.thumb, state.x.bar, state.x.thumb, state.corner]) {
            if (el) state.parts.observe(el, { attributes: true, attributeFilter: ['style', 'tabindex', 'data-orientation', 'data-bit-bh-focusable'] });
        }

        // The wheel over a scrollbar scrolls the content. Not over the whole root: a wheel listener that may cancel
        // keeps the browser from scrolling off the main thread under it.
        for (const bar of state.wheelBars) bar.removeEventListener('wheel', onScrollAreaWheel);
        state.wheelBars = [state.y.bar, state.x.bar].filter((b): b is HTMLElement => !!b);
        for (const bar of state.wheelBars) bar.addEventListener('wheel', onScrollAreaWheel, { passive: false });

        state.measure = true;
        state.checkFocus = true;
    }

    function observeScrollAreaSizes(state: ScrollAreaState) {
        const ro = state.resize;
        if (!ro) return;
        const next: Element[] = [];
        const vp = state.viewport;
        if (vp) {
            next.push(vp);
            // The content's size: its top-level elements (a single wrapper is cheapest to watch).
            for (let i = 0; i < vp.children.length && i < 200; i++) next.push(vp.children[i]);
        }
        if (state.y.bar) next.push(state.y.bar);
        if (state.x.bar) next.push(state.x.bar);
        if (next.length === state.observed.length && next.every((el, i) => el === state.observed[i])) return;
        ro.disconnect();
        for (const el of next) ro.observe(el);
        state.observed = next;
    }

    function measureScrollArea(state: ScrollAreaState) {
        const vp = state.viewport;
        if (!vp) return;
        observeScrollAreaSizes(state);
        state.rtl = isRtl(vp);
        const wasOverflowing = state.y.overflow || state.x.overflow;
        state.y.max = Math.max(0, vp.scrollHeight - vp.clientHeight);
        state.x.max = Math.max(0, vp.scrollWidth - vp.clientWidth);
        for (const axis of [state.y, state.x]) {
            axis.overflow = axis.max > 1;
            if (!axis.overflow) axis.max = 0;
        }

        // Visibility before the sizes: a scrollbar that shows only now has nothing to measure yet.
        const type = state.options.type || 'hover';
        for (const axis of [state.y, state.x]) {
            const visible = !!axis.bar && (type === 'always' || (axis.overflow && (type === 'auto' || state.revealed)));
            axis.visible = visible;
            if (!axis.bar) continue;
            const value = visible ? 'visible' : 'hidden';
            if (axis.bar.getAttribute('data-state') !== value) axis.bar.setAttribute('data-state', value);
            // Functional: a hidden scrollbar is out of sight, and a press there belongs to the content under it.
            setScrollAreaStyle(axis.bar, 'pointer-events', visible ? '' : 'none');
        }

        // The corner before the tracks: the scrollbars stop short of it.
        const corner = !!(state.y.visible && state.x.visible) && type !== 'scroll';
        setScrollAreaStyle(state.root, '--bit-bh-scroll-area-corner-width', `${corner ? state.y.bar.offsetWidth : 0}px`);
        setScrollAreaStyle(state.root, '--bit-bh-scroll-area-corner-height', `${corner ? state.x.bar.offsetHeight : 0}px`);
        if (state.corner && state.corner.getAttribute('data-state') !== (corner ? 'visible' : 'hidden'))
            state.corner.setAttribute('data-state', corner ? 'visible' : 'hidden');

        for (const axis of [state.y, state.x]) {
            const bar = axis.bar;
            if (!bar) continue;
            const cs = getComputedStyle(bar);
            const padStart = parseFloat(axis.vertical ? cs.paddingTop : cs.paddingLeft) || 0;
            const padEnd = parseFloat(axis.vertical ? cs.paddingBottom : cs.paddingRight) || 0;
            const track = Math.max(0, (axis.vertical ? bar.clientHeight : bar.clientWidth) - padStart - padEnd);
            const client = axis.vertical ? vp.clientHeight : vp.clientWidth;
            const ratio = client + axis.max > 0 ? client / (client + axis.max) : 1;
            const size = Math.min(track, Math.max(track * ratio, SCROLL_THUMB_MIN));
            setScrollAreaStyle(bar, axis.vertical ? '--bit-bh-scroll-area-thumb-height' : '--bit-bh-scroll-area-thumb-width', `${Math.round(size * 100) / 100}px`);
            axis.track = track;
            const thumb = axis.thumb;
            // Read back what the CSS made of it, and where it sits untransformed (offsets ignore transforms):
            // the thumb may be a flex item, a block in a right-to-left bar, have a margin …
            axis.thumbSize = thumb ? (axis.vertical ? thumb.offsetHeight : thumb.offsetWidth) : size;
            axis.natural = thumb ? (axis.vertical ? thumb.offsetTop : thumb.offsetLeft) - padStart : 0;
        }

        if (state.checkFocus || wasOverflowing !== (state.y.overflow || state.x.overflow)) {
            state.checkFocus = false;
            updateScrollAreaTabStop(state);
        }
    }

    // A viewport left automatic is a tab stop exactly while it scrolls and nothing in it takes the focus: the
    // keyboard then scrolls it by focusing it, as browsers do on their own for such scrollers lately.
    function updateScrollAreaTabStop(state: ScrollAreaState) {
        const vp = state.viewport;
        if (!vp) return;
        let want = false;
        if (vp.getAttribute('data-bit-bh-focusable') === 'auto' && (state.y.overflow || state.x.overflow)) {
            want = true;
            for (const el of Array.from(vp.querySelectorAll(CANDIDATE_SELECTOR))) {
                if (isTabbable(el)) { want = false; break; }
            }
        }
        if (want) {
            if (vp.getAttribute('tabindex') !== '0') vp.setAttribute('tabindex', '0');
            state.ownsTabIndex = true;
        } else if (state.ownsTabIndex && document.activeElement !== vp) {
            // Only the tab stop this runtime added; a focused viewport keeps it until it is left.
            state.ownsTabIndex = false;
            if (vp.getAttribute('tabindex') === '0') vp.removeAttribute('tabindex');
        }
    }

    function positionScrollArea(state: ScrollAreaState) {
        const vp = state.viewport;
        if (!vp) return;
        const y = state.y;
        const x = state.x;
        // Reads first, then writes: nothing here forces a layout.
        const yStart = Math.min(Math.max(0, vp.scrollTop), y.max);
        // Right to left, scrollLeft runs from 0 (the start, on the right) into the negatives.
        const xStart = Math.min(Math.abs(vp.scrollLeft), x.max);
        const edges: [string, number][] = [
            ['y-start', yStart], ['y-end', y.max - yStart], ['x-start', xStart], ['x-end', x.max - xStart],
        ];
        for (const [edge, px] of edges) {
            setScrollAreaStyle(state.root, `--bit-bh-scroll-area-overflow-${edge}`, `${Math.max(0, Math.round(px))}px`);
            for (const el of [state.root, vp]) setScrollAreaFlag(el, `data-overflow-${edge}`, px >= 1);
        }
        for (const el of [state.root, vp]) {
            setScrollAreaFlag(el, 'data-overflow-y', y.overflow);
            setScrollAreaFlag(el, 'data-overflow-x', x.overflow);
        }
        placeScrollAreaThumb(y, yStart, false);
        placeScrollAreaThumb(x, xStart, state.rtl);
    }

    function placeScrollAreaThumb(axis: ScrollAxis, start: number, rtl: boolean) {
        const thumb = axis.thumb;
        if (!thumb) return;
        const range = Math.max(0, axis.track - axis.thumbSize);
        const fraction = axis.max > 0 ? start / axis.max : 0;
        // Physical: right to left, the start of a horizontal track is its right end.
        const along = rtl ? (1 - fraction) * range : fraction * range;
        const shift = Math.round((along - axis.natural) * 100) / 100 || 0;
        const value = axis.vertical ? `translate3d(0px, ${shift}px, 0px)` : `translate3d(${shift}px, 0px, 0px)`;
        if (thumb.style.transform !== value) thumb.style.transform = value;
    }

    /** Shows the scrollbars while the area is active, and hides them hideDelay after it stops being. */
    function updateScrollAreaActivity(state: ScrollAreaState) {
        const type = state.options.type || 'hover';
        const active = state.dragging || state.scrolling || (type === 'scroll' ? state.barHovering : state.hovering);
        if (active) {
            if (state.hideTimer) { clearTimeout(state.hideTimer); state.hideTimer = 0; }
            if (!state.revealed) { state.revealed = true; scheduleScrollArea(state, true); }
        } else if (state.revealed && !state.hideTimer) {
            state.hideTimer = window.setTimeout(() => {
                state.hideTimer = 0;
                state.revealed = false;
                scheduleScrollArea(state, true);
            }, Math.max(0, state.options.hideDelay ?? 600));
        }
    }

    function onScrollAreaWheel(this: HTMLElement, e: WheelEvent) {
        let state: ScrollAreaState | null = null;
        for (const s of scrollAreas.values()) if (s.wheelBars.includes(this)) { state = s; break; }
        const vp = state?.viewport;
        const axis = state && scrollAxisOf(state, this);
        if (!vp || !axis || e.ctrlKey) return;
        const unit = e.deltaMode === 1 ? 16 : e.deltaMode === 2 ? (axis.vertical ? vp.clientHeight : vp.clientWidth) : 1;
        // A vertical wheel over a horizontal scrollbar scrolls sideways, as over a native one.
        const delta = (axis.vertical ? e.deltaY : (e.deltaX || e.deltaY)) * unit;
        if (!delta) return;
        const current = axis.vertical ? vp.scrollTop : vp.scrollLeft;
        const low = !axis.vertical && state.rtl ? -axis.max : 0;
        const high = !axis.vertical && state.rtl ? 0 : axis.max;
        // At the end the wheel goes on to scroll the page, as it would from the content.
        if (delta > 0 ? current >= high - 0.5 : current <= low + 0.5) return;
        e.preventDefault();
        vp.scrollBy(axis.vertical ? { top: delta } : { left: delta });
    }

    function onScrollAreaPointerDown(state: ScrollAreaState, e: PointerEvent) {
        if (e.button !== 0 || state.endPress) return;
        const bar = ownScrollbar(state, e.target);
        const axis = scrollAxisOf(state, bar);
        const vp = state.viewport;
        if (!axis || !vp || !axis.visible) return;
        e.preventDefault();
        const thumb = axis.thumb;
        const at = (ev: PointerEvent) => (axis.vertical ? ev.clientY : ev.clientX);
        let pointer = at(e);
        let timer = 0;
        let onMove: (ev: PointerEvent) => void;

        if (thumb && thumb.contains(e.target as Node)) {
            // A thumb drag: the content follows the pointer, at the ratio of the scroll range to the thumb's range.
            const start = pointer;
            const startScroll = axis.vertical ? vp.scrollTop : vp.scrollLeft;
            const ratio = axis.max / Math.max(1, axis.track - axis.thumbSize);
            state.dragging = true;
            setScrollAreaFlag(bar, 'data-dragging', true);
            setScrollAreaFlag(thumb, 'data-dragging', true);
            updateScrollAreaActivity(state);
            onMove = (ev) => {
                if (ev.pointerId !== e.pointerId) return;
                const next = startScroll + (at(ev) - start) * ratio;
                if (axis.vertical) vp.scrollTop = next; else vp.scrollLeft = next;
            };
        } else {
            // A press on the track scrolls a page towards the pointer, and again while it is held, until the
            // thumb reaches the pointer.
            const page = () => {
                const r = thumb ? thumb.getBoundingClientRect() : null;
                if (!r) return false;
                const before = pointer < (axis.vertical ? r.top : r.left);
                const after = pointer > (axis.vertical ? r.bottom : r.right);
                if (!before && !after) return false;
                const size = (axis.vertical ? vp.clientHeight : vp.clientWidth) * 0.875;
                vp.scrollBy(axis.vertical ? { top: before ? -size : size } : { left: before ? -size : size });
                return true;
            };
            if (!page()) return;
            const repeat = (delay: number) => { timer = window.setTimeout(() => { if (page()) repeat(60); }, delay); };
            repeat(400);
            onMove = (ev) => { if (ev.pointerId === e.pointerId) pointer = at(ev); };
        }

        const end = (ev?: PointerEvent) => {
            if (ev && ev.pointerId !== e.pointerId) return;
            clearTimeout(timer);
            bar.removeEventListener('pointermove', onMove);
            bar.removeEventListener('pointerup', end);
            bar.removeEventListener('pointercancel', end);
            try { bar.releasePointerCapture(e.pointerId); } catch { /* ignore */ }
            state.endPress = null;
            if (state.dragging) {
                state.dragging = false;
                setScrollAreaFlag(bar, 'data-dragging', false);
                setScrollAreaFlag(thumb, 'data-dragging', false);
                updateScrollAreaActivity(state);
            }
        };
        try { bar.setPointerCapture(e.pointerId); } catch { /* ignore */ }
        state.endPress = () => end();
        bar.addEventListener('pointermove', onMove);
        bar.addEventListener('pointerup', end);
        bar.addEventListener('pointercancel', end);
    }

    function onScrollAreaScroll(state: ScrollAreaState, e: Event) {
        const vp = state.viewport;
        if (!vp || e.target !== vp) return;
        const top = vp.scrollTop;
        const left = vp.scrollLeft;
        if (top !== state.lastTop) state.y.scrolling = true;
        if (left !== state.lastLeft) state.x.scrolling = true;
        state.lastTop = top;
        state.lastLeft = left;
        state.scrolling = true;
        setScrollAreaFlag(state.root, 'data-scrolling', true);
        setScrollAreaFlag(state.y.bar, 'data-scrolling', state.y.scrolling);
        setScrollAreaFlag(state.x.bar, 'data-scrolling', state.x.scrolling);
        clearTimeout(state.scrollEndTimer);
        state.scrollEndTimer = window.setTimeout(() => {
            state.scrollEndTimer = 0;
            state.scrolling = false;
            state.y.scrolling = false;
            state.x.scrolling = false;
            for (const el of [state.root, state.y.bar, state.x.bar]) setScrollAreaFlag(el, 'data-scrolling', false);
            updateScrollAreaActivity(state);
        }, SCROLL_END_DELAY);
        updateScrollAreaActivity(state);
        scheduleScrollArea(state);
    }

    function setScrollAreaHovering(state: ScrollAreaState, hovering: boolean) {
        state.hovering = hovering;
        for (const el of [state.root, state.y.bar, state.x.bar]) setScrollAreaFlag(el, 'data-hovering', hovering);
        updateScrollAreaActivity(state);
    }

    function newScrollAxis(vertical: boolean): ScrollAxis {
        return { vertical, bar: null, thumb: null, overflow: false, visible: false, scrolling: false, max: 0, track: 0, thumbSize: 0, natural: 0 };
    }

    const scrollArea = {
        /** options: { type: 'hover' | 'scroll' | 'auto' | 'always', hideDelay (ms) }. */
        attach(rootId: string, options: ScrollAreaState['options']) {
            const root = byId(rootId);
            if (!root) return 0;
            addScrollAreaSheet();
            const handle = ++scrollAreaSeq;
            const state: ScrollAreaState = {
                root, rootId, options: options || {}, viewport: null, corner: null,
                y: newScrollAxis(true), x: newScrollAxis(false), rtl: false,
                hovering: false, barHovering: false, dragging: false, scrolling: false, revealed: false,
                hideTimer: 0, scrollEndTimer: 0, frame: 0, measure: true, checkFocus: true, partsStale: true,
                ownsTabIndex: false, lastTop: 0, lastLeft: 0,
                resize: typeof ResizeObserver === 'function' ? new ResizeObserver(() => scheduleScrollArea(state, true)) : null,
                content: null, parts: null, observed: [], wheelBars: [], listeners: [], endPress: null, detached: false,
            };
            // Content and parts that come and go: a change inside the viewport is content (sizes and the tab stop),
            // one outside it may be a scrollbar, a thumb or the viewport itself.
            state.content = new MutationObserver((records) => {
                const vp = state.viewport;
                for (const r of records) {
                    if (!vp || !vp.isConnected || !vp.contains(r.target)) { state.partsStale = true; break; }
                }
                state.checkFocus = true;
                scheduleScrollArea(state, true);
            });
            state.content.observe(root, { childList: true, subtree: true, characterData: true });
            state.parts = new MutationObserver(() => {
                state.checkFocus = true;
                scheduleScrollArea(state, true);
            });

            const listen = (name: string, fn: EventListener, opts: boolean | AddEventListenerOptions) => {
                root.addEventListener(name, fn, opts);
                state.listeners.push([name, fn, opts]);
            };
            // Scroll events don't bubble; the capture phase still passes the root.
            listen('scroll', (e) => onScrollAreaScroll(state, e), { capture: true, passive: true });
            listen('pointerenter', ((e: PointerEvent) => { if (e.pointerType !== 'touch') setScrollAreaHovering(state, true); }) as EventListener, false);
            listen('pointerleave', () => setScrollAreaHovering(state, false), false);
            listen('pointerover', ((e: PointerEvent) => {
                if (!state.barHovering && ownScrollbar(state, e.target)) { state.barHovering = true; updateScrollAreaActivity(state); }
            }) as EventListener, false);
            listen('pointerout', ((e: PointerEvent) => {
                const bar = ownScrollbar(state, e.target);
                if (bar && !(e.relatedTarget && bar.contains(e.relatedTarget as Node))) { state.barHovering = false; updateScrollAreaActivity(state); }
            }) as EventListener, false);
            listen('pointerdown', ((e: PointerEvent) => onScrollAreaPointerDown(state, e)) as EventListener, false);

            resolveScrollAreaParts(state);
            state.lastTop = state.viewport ? state.viewport.scrollTop : 0;
            state.lastLeft = state.viewport ? state.viewport.scrollLeft : 0;
            scrollAreas.set(handle, state);
            // Measured right away, so a page that comes interactive shows its thumbs in the same frame.
            flushScrollArea(state);
            return handle;
        },
        update(handle: number, options: ScrollAreaState['options']) {
            const state = scrollAreas.get(handle);
            if (!state) return;
            state.options = options || {};
            updateScrollAreaActivity(state);
            scheduleScrollArea(state, true);
        },
        detach(handle: number) {
            const state = scrollAreas.get(handle);
            if (!state) return;
            scrollAreas.delete(handle);
            state.detached = true;
            state.endPress?.();
            if (state.frame) cancelAnimationFrame(state.frame);
            clearTimeout(state.hideTimer);
            clearTimeout(state.scrollEndTimer);
            state.resize?.disconnect();
            state.content.disconnect();
            state.parts.disconnect();
            for (const bar of state.wheelBars) bar.removeEventListener('wheel', onScrollAreaWheel);
            for (const [name, fn, opts] of state.listeners) state.root.removeEventListener(name, fn, opts);
        },
    };

    // ─── splitter ────────────────────────────────────────────────────────────────
    //
    // Splitter handles: a drag reports the distance moved in percentage points of the space the panels share
    // (the sum of their sizes along the axis, so the handles' own thickness doesn't count), mirrored by the
    // layout's direction, one call in flight. Escape puts the panels back. Layouts are kept in localStorage.

    interface SplitterState {
        root: HTMLElement;
        rootId: string;
        options: { orientation?: string };
        onPointerDown: (e: PointerEvent) => void;
        cancel: (() => void) | null;
    }

    const splitters = new Map<number, SplitterState>();
    let splitterSeq = 0;

    const splitter = {
        /** options: { orientation }. Calls OnSplitterPointer(phase, handleId, delta) on .NET: start, move, end or cancel. */
        attach(dotnet: DotNetRef, rootId: string, options: SplitterState['options']) {
            const root = byId(rootId);
            if (!root) return 0;
            const id = ++splitterSeq;
            const state: SplitterState = { root, rootId, options: options || {}, onPointerDown: null, cancel: null };
            state.onPointerDown = (e: PointerEvent) => {
                if (e.button !== 0 || state.cancel) return;
                const handle = isElement(e.target) ? e.target.closest<HTMLElement>('[data-bit-bh-splitter-handle]') : null;
                // A nested splitter's handle is its own business.
                if (!handle || handle.getAttribute('data-bit-bh-splitter-handle') !== rootId || handle.hasAttribute('data-disabled')) return;
                e.preventDefault();
                handle.focus({ preventScroll: true });
                const vertical = state.options.orientation === 'vertical';
                // The layout is drawn by the CSS direction: mirror by what is on screen.
                const rtl = !vertical && isRtl(root);
                let total = 0;
                for (const panel of Array.from(root.querySelectorAll<HTMLElement>(`[data-bit-bh-splitter-panel="${CSS.escape(rootId)}"]`))) {
                    const r = panel.getBoundingClientRect();
                    total += vertical ? r.height : r.width;
                }
                const start = vertical ? e.clientY : e.clientX;
                let delta = 0;
                let busy = false;
                let pending: number | null = null;
                const send = (phase: string, value: number) => {
                    // Coalesce moves: one call in flight, the latest distance next.
                    if (phase === 'move' && busy) { pending = value; return; }
                    busy = true;
                    dotnet.invokeMethodAsync('OnSplitterPointer', phase, handle.id, value).catch(() => { }).finally(() => {
                        busy = false;
                        if (pending !== null) { const v = pending; pending = null; send('move', v); }
                    });
                };
                const distance = (ev: PointerEvent) => {
                    if (total <= 0) return 0;
                    const moved = (vertical ? ev.clientY : ev.clientX) - start;
                    return (rtl ? -moved : moved) / total * 100;
                };
                const move = (ev: PointerEvent) => {
                    if (ev.pointerId !== e.pointerId) return;
                    delta = distance(ev);
                    send('move', delta);
                };
                const finish = (phase: string) => {
                    handle.removeEventListener('pointermove', move);
                    handle.removeEventListener('pointerup', up);
                    handle.removeEventListener('pointercancel', up);
                    document.removeEventListener('keydown', esc, true);
                    try { handle.releasePointerCapture(e.pointerId); } catch { /* ignore */ }
                    root.removeAttribute('data-dragging');
                    state.cancel = null;
                    pending = null;
                    send(phase, delta);
                };
                const up = (ev: PointerEvent) => {
                    if (ev.pointerId !== e.pointerId) return;
                    if (ev.type === 'pointerup') delta = distance(ev);
                    finish('end');
                };
                const esc = (ev: KeyboardEvent) => {
                    if (ev.key !== 'Escape') return;
                    // The drag's Escape: not a dialog's or a popover's around the splitter.
                    ev.preventDefault();
                    ev.stopPropagation();
                    finish('cancel');
                };
                try { handle.setPointerCapture(e.pointerId); } catch { /* ignore */ }
                root.setAttribute('data-dragging', '');
                state.cancel = () => finish('cancel');
                send('start', 0);
                handle.addEventListener('pointermove', move);
                handle.addEventListener('pointerup', up);
                handle.addEventListener('pointercancel', up);
                document.addEventListener('keydown', esc, true);
            };
            root.addEventListener('pointerdown', state.onPointerDown);
            splitters.set(id, state);
            return id;
        },
        update(handle: number, options: SplitterState['options']) {
            const state = splitters.get(handle);
            if (state) state.options = options || {};
        },
        detach(handle: number) {
            const state = splitters.get(handle);
            if (!state) return;
            splitters.delete(handle);
            state.cancel?.();
            state.root.removeEventListener('pointerdown', state.onPointerDown);
        },
        /** The sizes saved under the key, or null (none, unreadable, or storage refused). */
        load(key: string): number[] | null {
            try {
                const raw = localStorage.getItem(key);
                const sizes = raw ? JSON.parse(raw) : null;
                return Array.isArray(sizes) && sizes.every((s) => typeof s === 'number' && isFinite(s)) ? sizes : null;
            } catch {
                return null;
            }
        },
        save(key: string, sizes: number[]) {
            try { localStorage.setItem(key, JSON.stringify(sizes)); } catch { /* storage full or refused */ }
        },
    };

    // ─── tab between ─────────────────────────────────────────────────────────────
    //
    // A disclosure whose panel is rendered elsewhere (a navigation menu's content in a shared viewport) keeps
    // the Tab order of a panel that follows its trigger:
    //
    //     <button data-bit-bh-tab-into="panel-id">        Tab moves to the panel's first tabbable element
    //     <div id="panel-id" data-bit-bh-tab-out="button-id">
    //                                                     Shift+Tab on its first one goes back to the trigger,
    //                                                     Tab on its last one to what follows the trigger
    //
    // Where the panel does follow its trigger this is what the browser does anyway.

    function onTabBetween(e: KeyboardEvent) {
        if (e.key !== 'Tab' || e.altKey || e.ctrlKey || e.metaKey || e.defaultPrevented || isComposing(e)) return;
        const target = e.target;
        if (!isElement(target)) return;

        const into = e.shiftKey ? null : target.getAttribute('data-bit-bh-tab-into');
        if (into) {
            const first = tabbables(byId(into))[0];
            if (first) { e.preventDefault(); focusElement(first); }
            return;
        }

        const panel = target.closest<HTMLElement>('[data-bit-bh-tab-out]');
        if (!panel) return;
        const origin = byId(panel.getAttribute('data-bit-bh-tab-out'));
        const items = tabbables(panel);
        if (!origin || !origin.isConnected || items.length === 0) return;
        if (e.shiftKey) {
            if (target !== items[0]) return;
            e.preventDefault();
            focusElement(origin);
            return;
        }
        if (target !== items[items.length - 1]) return;
        const outside = tabbables(document.body).filter((el) => !panel.contains(el));
        const index = outside.indexOf(origin);
        const next = index >= 0 ? outside[index + 1] : null;
        // Nothing after the trigger: the browser moves on to its own UI.
        if (next) { e.preventDefault(); focusElement(next); }
    }

    // ─── navigationMenu ──────────────────────────────────────────────────────────
    //
    // The size of the content open in a navigation menu's viewport, written on the viewport as
    // --bit-bh-navigation-menu-viewport-width / -height so it can animate between the sizes of the contents
    // it shows. Follows size changes (ResizeObserver) until told otherwise; the last size stays while the
    // viewport animates out. (The menu's indicator is a selection indicator: see `indicator`.)

    interface ViewportState {
        contentId: string | null;
        observer?: ResizeObserver;
        frame: number;
        update: () => void;
    }

    const viewports = new Map<string, ViewportState>();

    function sizeViewport(viewportId: string, contentId: string | null) {
        const viewport = byId(viewportId);
        const content = byId(contentId);
        if (!viewport || !content || content.hasAttribute('hidden')) return;
        viewport.style.setProperty('--bit-bh-navigation-menu-viewport-width', `${content.offsetWidth}px`);
        viewport.style.setProperty('--bit-bh-navigation-menu-viewport-height', `${content.offsetHeight}px`);
    }

    const navigationMenu = {
        /** Sizes the viewport after the content open in it, and keeps it so (null: stops, keeping the last size). */
        measure(viewportId: string, contentId: string | null) {
            let state = viewports.get(viewportId);
            if (!state) {
                const created: ViewportState = { contentId: null, frame: 0, update: null };
                created.update = () => {
                    if (created.frame) return;
                    created.frame = requestAnimationFrame(() => {
                        created.frame = 0;
                        if (viewports.get(viewportId) === created) sizeViewport(viewportId, created.contentId);
                    });
                };
                window.addEventListener('resize', created.update, { passive: true });
                viewports.set(viewportId, created);
                state = created;
            }
            state.observer?.disconnect();
            state.observer = undefined;
            state.contentId = contentId;
            if (!contentId) return;
            sizeViewport(viewportId, contentId);
            const content = byId(contentId);
            if (content && typeof ResizeObserver !== 'undefined') {
                state.observer = new ResizeObserver(state.update);
                state.observer.observe(content);
            }
        },
        /** Stops following for a viewport that is going away. */
        release(viewportId: string) {
            const state = viewports.get(viewportId);
            if (!state) return;
            viewports.delete(viewportId);
            state.observer?.disconnect();
            if (state.frame) cancelAnimationFrame(state.frame);
            window.removeEventListener('resize', state.update);
        },
    };

    // ─── drawer ──────────────────────────────────────────────────────────────────
    //
    // Swipe to dismiss. A pointer dragged on the panel towards the edge the drawer is attached to moves it:
    // --bit-bh-drawer-swipe-movement-x / -y (signed px along that edge's axis) and
    // --bit-bh-drawer-swipe-progress (0..1 of the panel's size) are written on the panel and its backdrop,
    // with data-swiping while the drag lasts. Released past the threshold - or flicked fast enough - the
    // drawer asks .NET to close (OnSwipeDismiss) and the values stay, so the exit animation starts where the
    // finger left the panel; otherwise they are cleared and the panel settles back.
    //
    // A drag is not a swipe when it starts in a text field, in [data-bit-bh-swipe-ignore], or in content that
    // can still scroll the way the finger moves (a list scrolled away from its top in a bottom drawer).

    interface DrawerOptions { side?: string; threshold?: number; backdropId?: string | null; enabled?: boolean }

    interface DrawerState {
        dotnet: DotNetRef;
        panelId: string;
        options: DrawerOptions;
        swiping: boolean;
        onPointerDown?: (e: PointerEvent) => void;
        onTouchMove?: (e: TouchEvent) => void;
    }

    const drawers = new Map<number, DrawerState>();
    let drawerSeq = 0;

    const SWIPE_VARS = ['--bit-bh-drawer-swipe-movement-x', '--bit-bh-drawer-swipe-movement-y', '--bit-bh-drawer-swipe-progress'];
    const SWIPE_IGNORE = 'input, textarea, select, [contenteditable]:not([contenteditable="false"]), [data-bit-bh-swipe-ignore]';

    function drawerSurfaces(state: DrawerState) {
        return [byId(state.panelId), byId(state.options.backdropId)].filter(Boolean);
    }

    function clearSwipe(state: DrawerState) {
        for (const el of drawerSurfaces(state)) {
            for (const v of SWIPE_VARS) el.style.removeProperty(v);
            el.removeAttribute('data-swiping');
        }
    }

    /** Whether `el` scrolls, and can still scroll the way a finger moving towards `side` scrolls it. */
    function scrollsTowards(el: HTMLElement, side: string) {
        const style = getComputedStyle(el);
        const scrollable = /(auto|scroll|overlay)/;
        if (side === 'top' || side === 'bottom') {
            if (!scrollable.test(style.overflowY) || el.scrollHeight <= el.clientHeight + 1) return false;
            // A finger moving down shows what is above: possible unless the content is at its top.
            return side === 'bottom' ? el.scrollTop > 0 : el.scrollTop + el.clientHeight < el.scrollHeight - 1;
        }
        if (!scrollable.test(style.overflowX) || el.scrollWidth <= el.clientWidth + 1) return false;
        const max = el.scrollWidth - el.clientWidth;
        const pos = Math.abs(el.scrollLeft);
        // scrollLeft runs 0 → -max in right-to-left content.
        const fromLeft = isRtl(el) ? max - pos : pos;
        return side === 'right' ? fromLeft > 1 : max - fromLeft > 1;
    }

    function startSwipe(state: DrawerState, panel: HTMLElement, e: PointerEvent) {
        const side = state.options.side || 'bottom';
        const vertical = side === 'top' || side === 'bottom';
        // +1 when the closing direction is the axis' positive direction (down / right).
        const sign = side === 'bottom' || side === 'right' ? 1 : -1;
        const pointerId = e.pointerId;
        const startX = e.clientX;
        const startY = e.clientY;
        let started = false;
        let movement = 0;
        const samples: { t: number; d: number }[] = [];

        const size = () => Math.max(1, vertical ? panel.offsetHeight : panel.offsetWidth);

        const write = () => {
            const progress = Math.min(1, movement / size());
            for (const el of drawerSurfaces(state)) {
                el.style.setProperty('--bit-bh-drawer-swipe-movement-x', vertical ? '0px' : `${sign * movement}px`);
                el.style.setProperty('--bit-bh-drawer-swipe-movement-y', vertical ? `${sign * movement}px` : '0px');
                el.style.setProperty('--bit-bh-drawer-swipe-progress', String(Math.round(progress * 1000) / 1000));
            }
        };

        const stop = () => {
            document.removeEventListener('pointermove', move, true);
            document.removeEventListener('pointerup', up, true);
            document.removeEventListener('pointercancel', cancel, true);
            state.swiping = false;
            try { panel.releasePointerCapture(pointerId); } catch { /* ignore */ }
        };

        const move = (ev: PointerEvent) => {
            if (ev.pointerId !== pointerId) return;
            const along = sign * (vertical ? ev.clientY - startY : ev.clientX - startX);
            const across = vertical ? ev.clientX - startX : ev.clientY - startY;
            if (!started) {
                if (Math.abs(along) < 4 && Math.abs(across) < 4) return;
                // A drag across the axis, or away from the edge, is the content's (scrolling, selecting).
                if (along <= 0 || Math.abs(along) < Math.abs(across)) { stop(); return; }
                started = true;
                state.swiping = true;
                try { panel.setPointerCapture(pointerId); } catch { /* ignore */ }
                for (const el of drawerSurfaces(state)) el.setAttribute('data-swiping', '');
            }
            ev.preventDefault();
            window.getSelection?.()?.removeAllRanges();
            movement = Math.max(0, along);
            const now = performance.now();
            samples.push({ t: now, d: movement });
            while (samples.length > 2 && now - samples[0].t > 100) samples.shift();
            write();
        };

        const finish = (ev: PointerEvent, cancelled: boolean) => {
            if (ev.pointerId !== pointerId) return;
            stop();
            if (!started) return;
            for (const el of drawerSurfaces(state)) el.removeAttribute('data-swiping');
            // The click a drag ends with is not a click on what the drag started on.
            const swallow = (ce: Event) => { ce.stopPropagation(); ce.preventDefault(); };
            document.addEventListener('click', swallow, { capture: true, once: true });
            setTimeout(() => document.removeEventListener('click', swallow, true), 0);

            const first = samples[0];
            const last = samples[samples.length - 1];
            const velocity = first && last && last.t > first.t ? (last.d - first.d) / (last.t - first.t) : 0;
            const threshold = state.options.threshold ?? 0.25;
            const dismiss = !cancelled && movement > 0 && (movement / size() >= threshold || (velocity > 0.5 && movement > 10));
            if (!dismiss) { clearSwipe(state); return; }
            // Kept while .NET closes the drawer: its exit animation starts from here.
            state.dotnet.invokeMethodAsync<boolean>('OnSwipeDismiss')
                .then((closing) => { if (closing !== true) clearSwipe(state); }, () => clearSwipe(state));
        };
        const up = (ev: PointerEvent) => finish(ev, false);
        const cancel = (ev: PointerEvent) => finish(ev, true);

        document.addEventListener('pointermove', move, true);
        document.addEventListener('pointerup', up, true);
        document.addEventListener('pointercancel', cancel, true);
    }

    const drawer = {
        /** options: { side: 'top' | 'right' | 'bottom' | 'left', threshold (0..1), backdropId, enabled }. */
        attach(dotnet: DotNetRef, panelId: string, options: DrawerOptions) {
            const panel = byId(panelId);
            if (!panel) return 0;
            const handle = ++drawerSeq;
            const state: DrawerState = { dotnet, panelId, options: options || {}, swiping: false };
            clearSwipe(state);
            state.onPointerDown = (e: PointerEvent) => {
                if (state.options.enabled === false || state.swiping) return;
                if (e.pointerType === 'mouse' && e.button !== 0) return;
                const target = e.target as Element;
                if (!isElement(target) || !panel.contains(target) || target.closest(SWIPE_IGNORE)) return;
                const side = state.options.side || 'bottom';
                // A drag on the handle is always the drawer's; elsewhere, content that can scroll that way keeps it.
                if (!target.closest('[data-bit-bh-drawer-handle]')) {
                    for (let el: HTMLElement | null = target as HTMLElement; el; el = el.parentElement) {
                        if (scrollsTowards(el, side)) return;
                        if (el === panel) break;
                    }
                }
                startSwipe(state, panel, e);
            };
            // Touch: once the drag is a swipe, the browser must not also pan / scroll with it.
            state.onTouchMove = (e: TouchEvent) => { if (state.swiping && e.cancelable) e.preventDefault(); };
            panel.addEventListener('pointerdown', state.onPointerDown);
            panel.addEventListener('touchmove', state.onTouchMove, { passive: false });
            drawers.set(handle, state);
            return handle;
        },
        update(handle: number, options: DrawerOptions) {
            const state = drawers.get(handle);
            if (state) state.options = options || {};
        },
        detach(handle: number) {
            const state = drawers.get(handle);
            if (!state) return;
            drawers.delete(handle);
            const panel = byId(state.panelId);
            panel?.removeEventListener('pointerdown', state.onPointerDown);
            panel?.removeEventListener('touchmove', state.onTouchMove);
        },
    };

    return {
        install, onEnhancedLoad,
        keys, interaction, anchor, layers, focus, modal, portal, presence, floating,
        hotkeys, collection, dom, announcer, transition, sortable, slider, scrub, rating, toasts, indicator, carousel,
        landmarks, scrollSpy, area, resizer, scrollArea, splitter,
        navigationMenu, drawer,
    };
}

type Runtime = ReturnType<typeof createRuntime>;

const runtime: Runtime = ((globalThis as { __bitBheadless?: Runtime }).__bitBheadless ??= createRuntime());

export const {
    install, onEnhancedLoad,
    keys, interaction, anchor, layers, focus, modal, portal, presence, floating,
    hotkeys, collection, dom, announcer, transition, sortable, slider, scrub, rating, toasts, indicator, carousel,
    landmarks, scrollSpy, area, resizer, scrollArea, splitter,
    navigationMenu, drawer,
} = runtime;

install();
