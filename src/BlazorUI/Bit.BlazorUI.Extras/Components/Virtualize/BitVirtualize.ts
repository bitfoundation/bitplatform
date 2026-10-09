namespace BitBlazorUI {
    export class Virtualize {
        private static _instances = new Map<string, VirtualizeInstance>();

        public static setup(
            id: string,
            rootElement: HTMLElement,
            horizontal: boolean,
            dynamic: boolean,
            scrollThreshold: number,
            scrollerSelector: string | null,
            dotnetObj: DotNetObject) {

            Virtualize._instances.get(id)?.dispose();

            const scroller = VirtualizeInstance.resolveScroller(scrollerSelector, rootElement);
            const instance = new VirtualizeInstance(rootElement, scroller, horizontal, dynamic, scrollThreshold, dotnetObj);
            Virtualize._instances.set(id, instance);

            return instance.metrics();
        }

        public static update(id: string, horizontal: boolean, dynamic: boolean, scrollThreshold: number) {
            Virtualize._instances.get(id)?.update(horizontal, dynamic, scrollThreshold);
        }

        public static sync(id: string) {
            Virtualize._instances.get(id)?.sync();
        }

        public static scrollToOffset(id: string, offset: number, smooth: boolean, seq: number) {
            Virtualize._instances.get(id)?.scrollToOffset(offset, smooth, seq);
        }

        public static scrollToEdge(id: string, end: boolean, smooth: boolean, seq: number) {
            Virtualize._instances.get(id)?.scrollToEdge(end, smooth, seq);
        }

        public static focusIndex(id: string, index: number) {
            Virtualize._instances.get(id)?.focusIndex(index);
        }

        public static dispose(id: string) {
            const instance = Virtualize._instances.get(id);
            if (!instance) return;

            instance.dispose();
            Virtualize._instances.delete(id);
        }
    }

    // The browser-side engine of the BitVirtualize component. One instance is created per component to:
    //   * Observe the scroll position and viewport size of the scroll container and report
    //     changes back to .NET (throttled to one notification per animation frame, and further
    //     coalesced by a movement threshold to keep Blazor Server interop chatter low).
    //   * Measure rendered items with a ResizeObserver (dynamic-size mode) and report
    //     real sizes back to .NET in batches.
    //   * Provide programmatic scrolling and scroll-anchor correction so dynamic
    //     measurements never make the content visibly jump.
    //   * Drive keyboard navigation (roving focus) via .NET.
    // Every offset exchanged with .NET is relative to the start of the items spacer: the content before it
    // (a header, or the space AlignToEnd adds, and the page above a list scrolled by an ancestor) is the "lead",
    // subtracted here so .NET never sees it.
    class VirtualizeInstance {
        private static readonly NAV_KEYS = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'PageDown', 'PageUp', 'Home', 'End'];
        private static readonly ITEM_SELECTOR = ':scope > .bit-vir-spc > .bit-vir-blk > [data-bit-vir-index]';
        // A smooth scroll is taken to be over once no scroll event arrived for SMOOTH_IDLE_MS, or SMOOTH_MAX_MS after it started.
        private static readonly SMOOTH_IDLE_MS = 150;
        private static readonly SMOOTH_MAX_MS = 1000;
        // Selectors that all mean "the page itself scrolls".
        private static readonly VIEWPORT_SELECTORS = ['window', 'document', 'body', 'html', ':root'];

        private _element: HTMLElement;
        // What scrolls the list: the list itself, an ancestor (ScrollerSelector), or null for the page viewport.
        private _scroller: HTMLElement | null;
        private _external: boolean;
        private _horizontal: boolean;
        private _dynamic: boolean;
        private _threshold: number;
        private _dotnetObj: DotNetObject;
        private _disposed = false;
        private _scrollScheduled = false;
        private _measureScheduled = false;
        private _viewportChanged = false;
        private _notified = false;
        private _lastNotifiedOffset = 0;
        private _trailingTimer: any = null;
        // Set while the scroll gets adjusted programmatically, to suppress the resulting
        // scroll event from being treated as a user scroll.
        private _suppressScroll = false;
        // The raw scroll position the last programmatic adjustment left the scroller at: a suppressed scroll event that
        // finds it elsewhere also carries a movement of the user's (both coalesce into one event per frame).
        private _adjustedRaw = NaN;
        // Whether a report went out since the browser last performed a scroll .NET requested. .NET discards a report that
        // was sent before the scroll it requested last got performed, so once it is, the position is reported again:
        // otherwise a user who stopped scrolling meanwhile leaves .NET rendering the window of a position long left.
        private _unconfirmed = false;
        private _owed = false;
        // Set while a smooth scroll this instance started is animating: writing the scroll position (as the
        // scroll anchoring does) would cancel the animation, so the anchoring waits for it to settle.
        private _smoothScrolling = false;
        private _smoothTimer: any = null;
        // When the animation is over at the latest, however long scroll events keep arriving: the user may carry on
        // scrolling from where it ends, which would otherwise hold the anchoring back for as long as they scroll.
        private _smoothDeadline = 0;
        // The signed anchor correction that arrived while the animation was running, applied once it settles.
        private _pendingAnchor = 0;
        // index -> element, tracks which items are currently observed.
        private _observed = new Map<number, Element>();
        private _pendingMeasures = new Map<number, number>(); // index -> size, batched until the next frame
        private _reported = new Map<number, number>(); // index -> last size reported to .NET, to skip duplicate reports
        // The pinned sticky header element, cached because _updateSticky runs synchronously on
        // every scroll event; re-resolved after renders (which can replace the element).
        private _stickyEl: HTMLElement | null = null;
        private _stickyResolved = false;
        // The elements that decide where the items start, and that distance (px): the root's padding, a header
        // and the space AlignToEnd adds.
        private _spacer: HTMLElement | null = null;
        private _header: HTMLElement | null = null;
        private _lead = 0;
        private _lastViewportSize = 0;
        private _lastCrossSize = 0;
        private _viewportObserver: ResizeObserver;
        private _leadObserver: ResizeObserver;
        private _itemObserver: ResizeObserver;
        // Performs the scroll a render carries (data-bit-vir-scroll on the spacer) as soon as the render lands:
        // mutation callbacks run before the browser paints, so the moved items and the scroll show up together.
        private _renderObserver: MutationObserver;
        // Whether the list lays out right to left. In horizontal mode browsers then report scrollLeft as <= 0 (0 at
        // the start, negative toward the end), and in either mode the next lane (and the next item of a horizontal
        // list) is on the left. Cached (rather than read via getComputedStyle on every scroll event) and refreshed
        // on render/resize, since the direction rarely changes.
        private _rtl = false;
        // Whether the scroller lays out right to left, which is what decides the sign of its scrollLeft.
        private _scrollRtl = false;
        // The number of the latest scroll .NET requested that got performed here, sent back with every report so
        // .NET can tell the reports sent before it (describing a position that no longer holds) from the rest.
        private _seq = 0;
        // The item the keyboard navigation is known to be on: the one .NET last focused, or a key was last pressed on.
        // A key pressed there continues from wherever the navigation has got to, since the keys pressed before it
        // may not have moved the focus yet; a key pressed on any other item (e.g. a clicked one) continues from it.
        private _navIndex = -1;
        // The element inside the list that last took the focus, until the focus moves to an element outside the list:
        // a render that removes it (its item got deleted, or the list emptied) would otherwise drop the focus to the page.
        private _focusTarget: Element | null = null;

        constructor(element: HTMLElement, scroller: HTMLElement | null, horizontal: boolean, dynamic: boolean, threshold: number, dotnetObj: DotNetObject) {
            this._element = element;
            this._scroller = scroller;
            this._external = scroller !== element;
            this._horizontal = horizontal;
            this._dynamic = dynamic;
            this._threshold = threshold > 0 ? threshold : 0;
            this._dotnetObj = dotnetObj;
            this._refreshRtl();

            const target = this._scrollTarget();
            target.addEventListener('scroll', this._onScroll, { passive: true });
            target.addEventListener('wheel', this._onGesture, { passive: true });
            target.addEventListener('touchstart', this._onGesture, { passive: true });
            this._element.addEventListener('keydown', this._onKeyDown);
            this._element.addEventListener('focusin', this._onFocusIn);
            this._element.addEventListener('focusout', this._onFocusOut);

            // Track viewport resizes. The observer's initial callback reports the size setup already returned;
            // notifying it would only send a stale offset that could race a scroll .NET is about to request.
            this._lastViewportSize = this._viewportSize();
            this._lastCrossSize = this._crossSize();
            this._viewportObserver = new ResizeObserver(this._onViewportResize);
            if (this._scroller) {
                this._viewportObserver.observe(this._scroller);
            } else {
                window.addEventListener('resize', this._onViewportResize);
            }
            // The size of the list across the scroll axis is what a responsive grid divides into lanes.
            if (this._external) {
                this._viewportObserver.observe(this._element);
            }

            // Track the size of what comes before the items (a header, or the AlignToEnd space that shrinks as the spacer grows).
            this._leadObserver = new ResizeObserver(() => {
                if (this._disposed) return;
                if (this._refreshLead()) {
                    this._viewportChanged = true;
                    this._onScroll();
                }
            });

            // Track item resizes (dynamic mode).
            this._itemObserver = new ResizeObserver(entries => this._onItemsResized(entries));

            this._renderObserver = new MutationObserver(() => {
                this._restoreFocus();
                this._applyRenderedScroll();
            });
            this._renderObserver.observe(this._element, { subtree: true, childList: true, attributes: true, attributeFilter: ['data-bit-vir-scroll'] });

            this._syncStructure();
        }

        public metrics() {
            // The offset first: it refreshes the lead of a list an ancestor scrolls, which the edges are read from.
            const scrollOffset = this._readOffset();
            const { headSize, tailSize } = this._edges();
            return { scrollOffset, viewportSize: this._viewportSize(), crossSize: this._crossSize(), headSize, tailSize };
        }

        // How far the edges scrollToEdge goes to lie beyond the items, before and after them: the content of the list
        // around its items (the header, the footer) as far as the scroll range reaches it. Independent of the scroll
        // position and of the size of the items, so .NET measures the edges against its own size of them.
        private _edges() {
            const spacer = this._spacer;
            if (!spacer || !spacer.isConnected) return { headSize: 0, tailSize: 0 };

            const spacerEnd = this._lead + this._sizeOf(spacer);
            const after = this._scrollExtent() - spacerEnd;
            if (!this._external) return { headSize: this._lead, tailSize: after };

            // An ancestor scrolls more than the list: its edges are the ones of the list, short of the scroll range's own.
            const start = this._startOf(this._element);
            return {
                headSize: Math.min(this._lead, this._lead - start),
                tailSize: Math.min(after, start + this._sizeOf(this._element) - spacerEnd),
            };
        }

        public update(horizontal: boolean, dynamic: boolean, threshold: number) {
            if (this._disposed) return;

            // Sizes measured along the other axis (or none at all anymore) are of no use.
            if (horizontal !== this._horizontal || dynamic === false) {
                this._itemObserver.disconnect();
                this._observed.clear();
                this._reported.clear();
                this._pendingMeasures.clear();
            }

            this._horizontal = horizontal;
            this._dynamic = dynamic;
            this._threshold = threshold > 0 ? threshold : 0;
            this._refreshRtl();
            this._refreshLead();
            this._lastViewportSize = this._viewportSize();
            this._viewportChanged = true;
            this._onScroll();
        }

        // Called by .NET after renders to keep the ResizeObserver subscriptions in sync with the elements
        // actually in the DOM. Measurement itself is left to the ResizeObserver (which fires on observe)
        // so we do not force a synchronous reflow on every render.
        public sync() {
            if (this._disposed) return;

            this._refreshRtl();
            this._syncStructure();

            if (this._dynamic) {
                this._syncMeasurements();
            }

            this._stickyResolved = false; // the render may have replaced the sticky element
            this._updateSticky();
        }

        public scrollToOffset(offset: number, smooth: boolean, seq: number) {
            if (this._disposed) return;

            this._adoptSeq(seq);
            this._scrollTo(offset + this._lead, smooth);
        }

        public scrollToEdge(end: boolean, smooth: boolean, seq: number) {
            if (this._disposed) return;

            this._adoptSeq(seq);
            if (!this._external) {
                this._scrollTo(end ? this._maxRawOffset() : 0, smooth);
                return;
            }

            // An ancestor scrolls more than the list: its edges are the edges of the list (header and footer included).
            const start = this._startOf(this._element);
            const target = end ? start + this._sizeOf(this._element) - this._viewportSize() : start;
            this._scrollTo(Math.min(this._maxRawOffset(), Math.max(0, target)), smooth);
        }

        // Adjusts the scroll position by delta without emitting a user-scroll event.
        // Used for scroll anchoring after items above the viewport are re-measured.
        public adjustScroll(delta: number, seq?: number) {
            if (this._disposed) return;

            this._adoptSeq(seq);
            if (delta === 0) return;

            // Mid-animation the correction cannot be written (it would cancel the smooth scroll),
            // so it is accumulated and applied in one go once the animation has settled.
            if (this._smoothScrolling) {
                this._pendingAnchor += delta;
                return;
            }

            this._suppressScroll = true;
            const scrolling = this._scrollingElement();
            if (this._horizontal) {
                scrolling.scrollLeft += this._scrollRtl ? -delta : delta;
            } else {
                scrolling.scrollTop += delta;
            }
            this._adjustedRaw = this._rawOffset();
            // Release the suppression after the scroll event has been dispatched.
            requestAnimationFrame(() => { this._suppressScroll = false; });
        }

        public focusIndex(index: number) {
            if (this._disposed) return;
            const el = this._element.querySelector(`:scope > .bit-vir-spc > .bit-vir-blk > [data-bit-vir-index='${index}']`) as HTMLElement | null;
            if (!el) return;
            this._navIndex = index;
            el.focus({ preventScroll: true });
        }

        // Resolves the element ScrollerSelector names: the list itself when there is none, null for the page.
        public static resolveScroller(selector: string | null | undefined, element: HTMLElement): HTMLElement | null {
            if (!selector || !selector.trim()) return element;

            if (VirtualizeInstance.VIEWPORT_SELECTORS.indexOf(selector.trim().toLowerCase()) >= 0) return null;

            try {
                // The nearest matching ancestor, falling back to the first match in the document.
                return (element.parentElement?.closest(selector) as HTMLElement | null) ?? (document.querySelector(selector) as HTMLElement | null);
            } catch {
                // An invalid selector is not worth breaking the component over: the page scrolls it, as with no ancestor.
                return null;
            }
        }

        public dispose() {
            this._disposed = true;
            const target = this._scrollTarget();
            target.removeEventListener('scroll', this._onScroll);
            target.removeEventListener('wheel', this._onGesture);
            target.removeEventListener('touchstart', this._onGesture);
            this._element.removeEventListener('keydown', this._onKeyDown);
            this._element.removeEventListener('focusin', this._onFocusIn);
            this._element.removeEventListener('focusout', this._onFocusOut);
            window.removeEventListener('resize', this._onViewportResize);
            this._viewportObserver.disconnect();
            this._leadObserver.disconnect();
            this._itemObserver.disconnect();
            this._renderObserver.disconnect();
            if (this._trailingTimer) clearTimeout(this._trailingTimer);
            if (this._smoothTimer) clearTimeout(this._smoothTimer);
            this._observed.clear();
            this._pendingMeasures.clear();
            this._reported.clear();
            this._stickyEl = null;
            this._spacer = null;
            this._header = null;
            this._focusTarget = null;
        }

        private _onFocusIn = (e: FocusEvent) => {
            this._focusTarget = e.target as Element;
        }

        // Only a focus that moves to another element forgets the target: one that goes nowhere may be the removal of the
        // target itself, which is what _restoreFocus is there for.
        private _onFocusOut = (e: FocusEvent) => {
            const next = e.relatedTarget as Node | null;
            if (next && !this._element.contains(next)) {
                this._focusTarget = null;
            }
        }

        // When the element that had the focus is gone and the focus with it, the focus goes to the item that took the place
        // of the removed one (the active item of the roving tabindex), or to the list itself when there is none.
        private _restoreFocus() {
            const target = this._focusTarget;
            if (!target || target.isConnected || this._disposed) return;

            this._focusTarget = null;
            const active = document.activeElement;
            if (active && active !== document.body && active !== document.documentElement) return;

            const next = this._element.querySelector(`${VirtualizeInstance.ITEM_SELECTOR}[tabindex='0']`) as HTMLElement | null;
            (next ?? this._element).focus({ preventScroll: true });
        }

        // "o:{offset}:{seq}" scrolls to an offset, "d:{delta}:{seq}" by a delta; each is performed once, and not at all
        // when a later scroll has been performed already.
        private _applyRenderedScroll() {
            if (this._disposed) return;

            const scroll = this._element.querySelector(':scope > .bit-vir-spc')?.getAttribute('data-bit-vir-scroll');
            if (!scroll) return;

            const [kind, valueText, seqText] = scroll.split(':');
            const value = parseFloat(valueText);
            const seq = parseInt(seqText, 10);
            if (isNaN(value) || !(seq > this._seq)) return;

            if (kind === 'd') {
                this.adjustScroll(value, seq);
            } else {
                // The same render may have resized what comes before the items (e.g. a header's text changed).
                this._syncStructure();
                this.scrollToOffset(value, false, seq);
            }
        }

        private _adoptSeq(seq: number | undefined) {
            if (typeof seq === 'number' && seq > this._seq) {
                this._seq = seq;

                if (this._unconfirmed) {
                    this._unconfirmed = false;
                    this._owed = true;
                    this._scheduleFlush();
                }
            }
        }

        private _refreshRtl() {
            this._rtl = getComputedStyle(this._element).direction === 'rtl';
            this._scrollRtl = this._external ? getComputedStyle(this._scrollingElement()).direction === 'rtl' : this._rtl;
        }

        // The element whose scroll position is read and written; the page's own for the page viewport.
        private _scrollingElement(): HTMLElement {
            return this._scroller ?? (document.scrollingElement as HTMLElement | null) ?? document.documentElement;
        }

        // What dispatches the scroll events: the scroller, or the window for the page.
        private _scrollTarget(): EventTarget {
            return this._scroller ?? window;
        }

        private _rawOffset() {
            const scrolling = this._scrollingElement();
            if (!this._horizontal) return scrolling.scrollTop;
            return this._scrollRtl ? -scrolling.scrollLeft : scrolling.scrollLeft;
        }

        private _readOffset() {
            // Whatever is above a list an ancestor scrolls may change at any time, so its lead is read afresh.
            if (this._external && this._spacer && this._spacer.isConnected) {
                this._lead = this._startOf(this._spacer);
            }
            return this._rawOffset() - this._lead;
        }

        private _viewportSize() {
            if (!this._scroller) {
                const root = document.documentElement;
                return this._horizontal ? root.clientWidth : root.clientHeight;
            }
            return this._horizontal ? this._scroller.clientWidth : this._scroller.clientHeight;
        }

        // The lanes share the content box of the list, which the spacer fills: the padding of the root is not theirs.
        private _crossSize() {
            const el = this._spacer && this._spacer.isConnected ? this._spacer : this._element;
            return this._horizontal ? el.clientHeight : el.clientWidth;
        }

        private _scrollExtent() {
            const scrolling = this._scrollingElement();
            return this._horizontal ? scrolling.scrollWidth : scrolling.scrollHeight;
        }

        private _sizeOf(el: HTMLElement) {
            return this._horizontal ? el.offsetWidth : el.offsetHeight;
        }

        // Where the leading edge of an element is within the scroll content of an ancestor scroller (or the page),
        // in the same coordinates as _rawOffset.
        private _startOf(el: HTMLElement) {
            const rect = el.getBoundingClientRect();
            let top = 0, left = 0, right = document.documentElement.clientWidth;
            if (this._scroller) {
                const box = this._scroller.getBoundingClientRect();
                top = box.top + this._scroller.clientTop;
                left = box.left + this._scroller.clientLeft;
                right = left + this._scroller.clientWidth;
            }

            const raw = this._rawOffset();
            if (!this._horizontal) return rect.top - top + raw;
            return this._scrollRtl ? right - rect.right + raw : rect.left - left + raw;
        }

        private _maxRawOffset() {
            return Math.max(0, this._scrollExtent() - this._viewportSize());
        }

        private _scrollTo(raw: number, smooth: boolean) {
            // The target is computed from the current sizes, so the corrections held back during an earlier animation are moot.
            this._pendingAnchor = 0;
            const behavior: ScrollBehavior = smooth && !VirtualizeInstance._reducedMotion() ? 'smooth' : 'auto';
            if (behavior === 'smooth') {
                this._smoothScrolling = true;
                this._smoothDeadline = performance.now() + VirtualizeInstance.SMOOTH_MAX_MS;
                this._armSmoothEnd();
            } else {
                // An instant scroll cancels any animation in flight.
                this._endSmooth();
            }
            const scrolling = this._scrollingElement();
            if (this._horizontal) {
                scrolling.scrollTo({ left: this._scrollRtl ? -raw : raw, behavior });
            } else {
                scrolling.scrollTo({ top: raw, behavior });
            }
        }

        // The animation is over once no scroll event arrived for a while (the scrollend event is not everywhere yet),
        // or once its deadline has passed.
        private _armSmoothEnd() {
            if (this._smoothTimer) clearTimeout(this._smoothTimer);
            const remaining = this._smoothDeadline - performance.now();
            if (remaining <= 0) {
                this._endSmooth();
                return;
            }
            this._smoothTimer = setTimeout(() => this._endSmooth(), Math.min(VirtualizeInstance.SMOOTH_IDLE_MS, remaining));
        }

        // Leaves the smooth-scrolling state, applying the anchor corrections held back while it lasted.
        private _endSmooth() {
            if (this._smoothTimer) { clearTimeout(this._smoothTimer); this._smoothTimer = null; }
            if (!this._smoothScrolling) return;

            this._smoothScrolling = false;
            const pending = this._pendingAnchor;
            this._pendingAnchor = 0;
            this.adjustScroll(pending);
        }

        private _onViewportResize = () => {
            if (this._disposed) return;
            this._refreshRtl();
            const leadChanged = this._refreshLead();
            const size = this._viewportSize();
            const cross = this._crossSize();
            if (size === this._lastViewportSize && cross === this._lastCrossSize && !leadChanged) return;
            this._lastViewportSize = size;
            this._lastCrossSize = cross;
            this._viewportChanged = true;
            this._onScroll();
        }

        // A wheel or touch gesture interrupts the browser's smooth scroll, so the anchoring need not wait for it any longer.
        private _onGesture = () => {
            if (this._disposed) return;
            this._endSmooth();
        }

        private static _reducedMotion() {
            return typeof matchMedia === 'function' && matchMedia('(prefers-reduced-motion: reduce)').matches;
        }

        // Re-resolves the spacer and the header (a render may add, remove or replace them) and observes their size.
        private _syncStructure() {
            const spacer = this._element.querySelector(':scope > .bit-vir-spc') as HTMLElement | null;
            if (spacer !== this._spacer) {
                if (this._spacer) this._leadObserver.unobserve(this._spacer);
                this._spacer = spacer;
                if (spacer) this._leadObserver.observe(spacer);
            }

            const header = this._element.querySelector(':scope > .bit-vir-hdr') as HTMLElement | null;
            if (header !== this._header) {
                if (this._header) this._leadObserver.unobserve(this._header);
                this._header = header;
                if (header) this._leadObserver.observe(header);
            }

            this._refreshLead();
        }

        // Recomputes the distance from the start of the scroll content to the start of the spacer; returns
        // whether it changed. When it changes while the viewport is past it, the scroll position follows so
        // the items in view stay put.
        private _refreshLead() {
            const spacer = this._spacer;
            if (this._external) {
                // The page around a list an ancestor scrolls is anchored by the browser itself, so nothing is adjusted here.
                const lead = spacer && spacer.isConnected ? this._startOf(spacer) : 0;
                const changed = Math.abs(lead - this._lead) > 0.5;
                this._lead = lead;
                return changed;
            }

            let lead = 0;
            if (spacer && spacer.isConnected) {
                // offsetTop/offsetLeft are layout positions: unaffected by the scroll position and by transforms.
                if (!this._horizontal) {
                    lead = spacer.offsetTop;
                } else if (!this._rtl) {
                    lead = spacer.offsetLeft;
                } else {
                    lead = this._element.clientWidth - (spacer.offsetLeft + spacer.offsetWidth);
                }
                lead = Math.max(0, lead);
            }

            const delta = lead - this._lead;
            if (delta === 0) return false;

            const raw = this._rawOffset();
            this._lead = lead;
            if (raw > lead - delta && raw > 0) {
                this.adjustScroll(delta);
            }

            return true;
        }

        private _syncMeasurements() {
            // Scoped to the own block so items of a nested BitVirtualize (rendered inside an
            // item, placeholder, or sticky template) never leak into this instance's measurements.
            const nodes = this._element.querySelectorAll(VirtualizeInstance.ITEM_SELECTOR);
            const present = new Set<number>();

            nodes.forEach(node => {
                const index = parseInt(node.getAttribute('data-bit-vir-index')!, 10);
                present.add(index);
                const previous = this._observed.get(index);
                if (previous !== node) {
                    if (previous) this._itemObserver.unobserve(previous);
                    this._observed.set(index, node);
                    this._reported.delete(index);
                    this._itemObserver.observe(node);
                }
            });

            // Stop observing items that have scrolled out of the rendered window.
            for (const [index, node] of this._observed) {
                if (!present.has(index)) {
                    this._itemObserver.unobserve(node);
                    this._observed.delete(index);
                    this._reported.delete(index);
                }
            }
        }

        private _onScroll = () => {
            if (this._disposed) return;

            // The pinned (sticky) header is positioned by css position:sticky, but the push-out
            // effect near the next group header has to track the scroll offset with no interop
            // latency, so it gets applied here synchronously on every scroll event.
            this._updateSticky();

            // Read before the animation may end below, applying its held-back anchoring: this event is not that adjustment's.
            const suppressed = this._suppressScroll;
            if (this._smoothScrolling) this._armSmoothEnd();

            // The scroll event of a programmatic adjustment is not a user scroll, but a viewport change that came with it
            // (a resize can move the lead, which adjusts the scroll before notifying) still has to reach .NET, and so does
            // a movement of the user's in the same frame, or a fast scroll that stops right there is never reported.
            if (suppressed && !this._viewportChanged && Math.abs(this._rawOffset() - this._adjustedRaw) < 0.5) return;

            this._scheduleFlush();
        }

        private _scheduleFlush() {
            if (this._scrollScheduled) return;
            this._scrollScheduled = true;
            requestAnimationFrame(this._flushScroll);
        }

        private _onKeyDown = (e: KeyboardEvent) => {
            if (this._disposed || e.defaultPrevented || e.altKey || e.metaKey) return;
            if (VirtualizeInstance.NAV_KEYS.indexOf(e.key) < 0) return;

            // Only take over navigation keys when focus is on the list container itself or on one of its own
            // item wrappers; let inner interactive controls (inputs, buttons, links) and the items of a nested
            // BitVirtualize handle them.
            const target = e.target as HTMLElement;
            let index = -1;
            if (target !== this._element) {
                if (!target.classList.contains('bit-vir-itm') || target.parentElement?.parentElement?.parentElement !== this._element) return;
                const focused = parseInt(target.getAttribute('data-bit-vir-index') || '-1', 10);
                index = focused === this._navIndex ? -1 : focused;
                this._navIndex = focused;
            }

            // In a right-to-left list the next item (or lane) is on the left.
            let key = e.key;
            if (this._rtl) {
                key = key === 'ArrowLeft' ? 'ArrowRight' : key === 'ArrowRight' ? 'ArrowLeft' : key;
            }

            e.preventDefault();
            this._dotnetObj.invokeMethodAsync('KeyNavigate', key, index);
        }

        // Pushes the pinned sticky header out of the way as the next group header (whose offset
        // .NET exposes through the data-bit-vir-sticky-next attribute) approaches the viewport edge.
        // The pinned header's size is its own: in dynamic mode, the size .NET has for its item is just the
        // estimate until that item has been rendered in the list (which a jump into its group skips).
        private _updateSticky() {
            if (!this._stickyResolved) {
                this._stickyEl = this._element.querySelector(':scope > .bit-vir-spc > .bit-vir-stk') as HTMLElement | null;
                this._stickyResolved = true;
            }

            const el = this._stickyEl;
            if (!el || !el.isConnected) return;

            const size = this._horizontal ? el.offsetWidth : el.offsetHeight;
            const next = parseFloat(el.getAttribute('data-bit-vir-sticky-next') || '');

            let delta = 0;
            if (!isNaN(next) && next >= 0) {
                delta = Math.min(0, next - this._readOffset() - size);
            }

            el.style.transform = this._horizontal
                ? `translateX(${this._rtl ? -delta : delta}px)`
                : `translateY(${delta}px)`;
        }

        private _flushScroll = () => {
            this._scrollScheduled = false;
            if (this._disposed) return;

            const m = this.metrics();

            if (this._shouldNotify(m.scrollOffset)) {
                this._notify(m);
            } else {
                this._scheduleTrailing();
            }
        }

        // Coalesce interop: skip notifications smaller than the movement threshold unless the viewport
        // changed or the scroll is near either edge (so edge-reached callbacks stay responsive).
        private _shouldNotify(offset: number) {
            if (this._viewportChanged || !this._notified || this._owed) return true;

            // A list an ancestor scrolls is out of view for most of the page's scrolling: while it stays out of view on the
            // same side there is nothing new to render.
            if (this._external) {
                const side = this._sideOf(offset);
                if (side !== 0 && side === this._sideOf(this._lastNotifiedOffset)) return false;
            }

            if (this._threshold <= 0) return true;

            const raw = this._rawOffset();
            const nearEdge = offset <= this._threshold || raw >= this._maxRawOffset() - this._threshold;
            return nearEdge || Math.abs(offset - this._lastNotifiedOffset) >= this._threshold;
        }

        // -1 while the viewport is entirely before the list, 1 while it is entirely past it, 0 while the two overlap.
        private _sideOf(offset: number) {
            if (offset + this._viewportSize() < 0) return -1;
            if (offset > (this._spacer ? this._sizeOf(this._spacer) : 0)) return 1;
            return 0;
        }

        private _notify(m: ReturnType<VirtualizeInstance['metrics']>) {
            this._notified = true;
            this._lastNotifiedOffset = m.scrollOffset;
            this._viewportChanged = false;
            this._owed = false;
            this._unconfirmed = true;
            if (this._trailingTimer) { clearTimeout(this._trailingTimer); this._trailingTimer = null; }
            this._dotnetObj.invokeMethodAsync('Scroll', m.scrollOffset, m.viewportSize, this._seq, m.crossSize, m.headSize, m.tailSize);
        }

        // Ensure the final resting position is always reported after the user stops scrolling.
        private _scheduleTrailing() {
            if (this._trailingTimer) return;
            this._trailingTimer = setTimeout(() => {
                this._trailingTimer = null;
                if (this._disposed) return;
                const m = this.metrics();
                if (m.scrollOffset === this._lastNotifiedOffset && !this._viewportChanged) return;

                // Like the coalescing above, a list an ancestor scrolls that is still out of view on the same side has
                // nothing new to render.
                if (this._external && !this._viewportChanged) {
                    const side = this._sideOf(m.scrollOffset);
                    if (side !== 0 && side === this._sideOf(this._lastNotifiedOffset)) return;
                }

                this._notify(m);
            }, 150);
        }

        private _onItemsResized = (entries: ResizeObserverEntry[]) => {
            if (this._disposed) return;

            // A list hidden by an ancestor (display:none, e.g. an inactive tab) reports every item as 0;
            // keep the real sizes it had instead.
            if (this._element.getClientRects().length === 0) return;

            for (const entry of entries) {
                // An item removed by a render reports 0 as well.
                if (!entry.target.isConnected) continue;

                const idxAttr = entry.target.getAttribute('data-bit-vir-index');
                if (idxAttr === null) continue;

                // The border box from the entry is not affected by transforms (e.g. a scaling dialog animation).
                const box = entry.borderBoxSize && entry.borderBoxSize[0];
                const size = box
                    ? (this._horizontal ? box.inlineSize : box.blockSize)
                    : this._measureElement(entry.target);

                this._pendingMeasures.set(parseInt(idxAttr, 10), size);
            }

            if (!this._measureScheduled && this._pendingMeasures.size > 0) {
                this._measureScheduled = true;
                requestAnimationFrame(this._flushMeasures);
            }
        }

        private _measureElement(el: Element) {
            const rect = el.getBoundingClientRect();
            return this._horizontal ? rect.width : rect.height;
        }

        private _flushMeasures = () => {
            this._measureScheduled = false;
            if (this._disposed || this._pendingMeasures.size === 0) return;

            const indices: number[] = [];
            const sizes: number[] = [];
            for (const [index, size] of this._pendingMeasures) {
                if (this._reported.get(index) === size) continue;
                this._reported.set(index, size);
                indices.push(index);
                sizes.push(size);
            }
            this._pendingMeasures.clear();

            if (indices.length === 0) return;

            this._dotnetObj.invokeMethodAsync('ItemsMeasured', indices, sizes);
        }
    }
}
