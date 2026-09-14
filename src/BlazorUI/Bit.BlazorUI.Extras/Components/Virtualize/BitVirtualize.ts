namespace BitBlazorUI {
    export class Virtualize {
        private static _instances = new Map<string, VirtualizeInstance>();

        public static setup(
            id: string,
            rootElement: HTMLElement,
            horizontal: boolean,
            dynamic: boolean,
            scrollThreshold: number,
            dotnetObj: DotNetObject) {

            Virtualize._instances.get(id)?.dispose();

            const instance = new VirtualizeInstance(rootElement, horizontal, dynamic, scrollThreshold, dotnetObj);
            Virtualize._instances.set(id, instance);

            return instance.metrics();
        }

        public static update(id: string, horizontal: boolean, dynamic: boolean, scrollThreshold: number) {
            Virtualize._instances.get(id)?.update(horizontal, dynamic, scrollThreshold);
        }

        public static sync(id: string) {
            Virtualize._instances.get(id)?.sync();
        }

        public static scrollToOffset(id: string, offset: number, smooth: boolean) {
            Virtualize._instances.get(id)?.scrollToOffset(offset, smooth);
        }

        public static scrollToEdge(id: string, end: boolean, smooth: boolean) {
            Virtualize._instances.get(id)?.scrollToEdge(end, smooth);
        }

        public static adjustScroll(id: string, delta: number) {
            Virtualize._instances.get(id)?.adjustScroll(delta);
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
    // (a header, or the space AlignToEnd adds) is the "lead", subtracted here so .NET never sees it.
    class VirtualizeInstance {
        private static readonly NAV_KEYS = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'PageDown', 'PageUp', 'Home', 'End'];
        private static readonly ITEM_SELECTOR = ':scope > .bit-vir-spc > .bit-vir-blk > [data-bit-vir-index]';
        // A smooth scroll is taken to be over once no scroll event arrived for SMOOTH_IDLE_MS, or SMOOTH_MAX_MS after it started.
        private static readonly SMOOTH_IDLE_MS = 150;
        private static readonly SMOOTH_MAX_MS = 1000;

        private _element: HTMLElement;
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
        private _viewportObserver: ResizeObserver;
        private _leadObserver: ResizeObserver;
        private _itemObserver: ResizeObserver;
        // In RTL horizontal mode, browsers report scrollLeft as <= 0 (0 at the start, negative toward
        // the end). Cached (rather than read via getComputedStyle on every scroll event) and refreshed
        // on render/resize, since the direction rarely changes.
        private _rtl = false;

        constructor(element: HTMLElement, horizontal: boolean, dynamic: boolean, threshold: number, dotnetObj: DotNetObject) {
            this._element = element;
            this._horizontal = horizontal;
            this._dynamic = dynamic;
            this._threshold = threshold > 0 ? threshold : 0;
            this._dotnetObj = dotnetObj;
            this._refreshRtl();

            this._element.addEventListener('scroll', this._onScroll, { passive: true });
            this._element.addEventListener('keydown', this._onKeyDown);
            this._element.addEventListener('wheel', this._onGesture, { passive: true });
            this._element.addEventListener('touchstart', this._onGesture, { passive: true });

            // Track viewport resizes. The observer's initial callback reports the size setup already returned;
            // notifying it would only send a stale offset that could race a scroll .NET is about to request.
            this._lastViewportSize = this._viewportSize();
            this._viewportObserver = new ResizeObserver(() => {
                if (this._disposed) return;
                this._refreshRtl();
                const leadChanged = this._refreshLead();
                const size = this._viewportSize();
                if (size === this._lastViewportSize && !leadChanged) return;
                this._lastViewportSize = size;
                this._viewportChanged = true;
                this._onScroll();
            });
            this._viewportObserver.observe(this._element);

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

            this._syncStructure();
        }

        public metrics() {
            return { scrollOffset: this._readOffset(), viewportSize: this._viewportSize() };
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

        public scrollToOffset(offset: number, smooth: boolean) {
            if (this._disposed) return;

            this._scrollTo(offset + this._lead, smooth);
        }

        public scrollToEdge(end: boolean, smooth: boolean) {
            if (this._disposed) return;

            this._scrollTo(end ? this._maxRawOffset() : 0, smooth);
        }

        // Adjusts the scroll position by delta without emitting a user-scroll event.
        // Used for scroll anchoring after items above the viewport are re-measured.
        public adjustScroll(delta: number) {
            if (this._disposed || delta === 0) return;

            // Mid-animation the correction cannot be written (it would cancel the smooth scroll),
            // so it is accumulated and applied in one go once the animation has settled.
            if (this._smoothScrolling) {
                this._pendingAnchor += delta;
                return;
            }

            this._suppressScroll = true;
            if (this._horizontal) {
                this._element.scrollLeft += this._rtl ? -delta : delta;
            } else {
                this._element.scrollTop += delta;
            }
            // Release the suppression after the scroll event has been dispatched.
            requestAnimationFrame(() => { this._suppressScroll = false; });
        }

        public focusIndex(index: number) {
            if (this._disposed) return;
            const el = this._element.querySelector(`:scope > .bit-vir-spc > .bit-vir-blk > [data-bit-vir-index='${index}']`) as HTMLElement | null;
            el?.focus({ preventScroll: true });
        }

        public dispose() {
            this._disposed = true;
            this._element.removeEventListener('scroll', this._onScroll);
            this._element.removeEventListener('keydown', this._onKeyDown);
            this._element.removeEventListener('wheel', this._onGesture);
            this._element.removeEventListener('touchstart', this._onGesture);
            this._viewportObserver.disconnect();
            this._leadObserver.disconnect();
            this._itemObserver.disconnect();
            if (this._trailingTimer) clearTimeout(this._trailingTimer);
            if (this._smoothTimer) clearTimeout(this._smoothTimer);
            this._observed.clear();
            this._pendingMeasures.clear();
            this._reported.clear();
            this._stickyEl = null;
            this._spacer = null;
            this._header = null;
        }

        private _refreshRtl() {
            this._rtl = this._horizontal && getComputedStyle(this._element).direction === 'rtl';
        }

        private _rawOffset() {
            if (!this._horizontal) return this._element.scrollTop;
            return this._rtl ? -this._element.scrollLeft : this._element.scrollLeft;
        }

        private _readOffset() {
            return this._rawOffset() - this._lead;
        }

        private _viewportSize() {
            return this._horizontal ? this._element.clientWidth : this._element.clientHeight;
        }

        private _scrollExtent() {
            return this._horizontal ? this._element.scrollWidth : this._element.scrollHeight;
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
            if (this._horizontal) {
                this._element.scrollTo({ left: this._rtl ? -raw : raw, behavior });
            } else {
                this._element.scrollTo({ top: raw, behavior });
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
            // (a resize can move the lead, which adjusts the scroll before notifying) still has to reach .NET.
            if (suppressed && !this._viewportChanged) return;

            if (!this._scrollScheduled) {
                this._scrollScheduled = true;
                requestAnimationFrame(this._flushScroll);
            }
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
                index = parseInt(target.getAttribute('data-bit-vir-index') || '-1', 10);
            }

            // In a right-to-left horizontal list the next item is on the left.
            let key = e.key;
            if (this._horizontal && this._rtl) {
                key = key === 'ArrowLeft' ? 'ArrowRight' : key === 'ArrowRight' ? 'ArrowLeft' : key;
            }

            e.preventDefault();
            this._dotnetObj.invokeMethodAsync('KeyNavigate', key, index);
        }

        // Pushes the pinned sticky header out of the way as the next group header (whose offset
        // .NET exposes through the data-bit-vir-sticky-next attribute) approaches the viewport edge.
        private _updateSticky() {
            if (!this._stickyResolved) {
                this._stickyEl = this._element.querySelector(':scope > .bit-vir-spc > .bit-vir-stk') as HTMLElement | null;
                this._stickyResolved = true;
            }

            const el = this._stickyEl;
            if (!el || !el.isConnected) return;

            const size = parseFloat(el.getAttribute('data-bit-vir-sticky-size') || '');
            const next = parseFloat(el.getAttribute('data-bit-vir-sticky-next') || '');

            let delta = 0;
            if (!isNaN(size) && !isNaN(next) && next >= 0) {
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
                this._notify(m.scrollOffset, m.viewportSize);
            } else {
                this._scheduleTrailing();
            }
        }

        // Coalesce interop: skip notifications smaller than the movement threshold unless the viewport
        // changed or the scroll is near either edge (so edge-reached callbacks stay responsive).
        private _shouldNotify(offset: number) {
            if (this._viewportChanged || this._threshold <= 0 || !this._notified) return true;

            const raw = this._rawOffset();
            const nearEdge = offset <= this._threshold || raw >= this._maxRawOffset() - this._threshold;
            return nearEdge || Math.abs(offset - this._lastNotifiedOffset) >= this._threshold;
        }

        private _notify(offset: number, viewportSize: number) {
            this._notified = true;
            this._lastNotifiedOffset = offset;
            this._viewportChanged = false;
            if (this._trailingTimer) { clearTimeout(this._trailingTimer); this._trailingTimer = null; }
            this._dotnetObj.invokeMethodAsync('Scroll', offset, viewportSize);
        }

        // Ensure the final resting position is always reported after the user stops scrolling.
        private _scheduleTrailing() {
            if (this._trailingTimer) return;
            this._trailingTimer = setTimeout(() => {
                this._trailingTimer = null;
                if (this._disposed) return;
                const m = this.metrics();
                if (m.scrollOffset !== this._lastNotifiedOffset) {
                    this._notify(m.scrollOffset, m.viewportSize);
                }
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
