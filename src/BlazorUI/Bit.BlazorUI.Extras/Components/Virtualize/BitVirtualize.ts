namespace BitBlazorUI {
    export class Virtualize {
        private static _instances = new Map<string, VirtualizeInstance>();

        public static setup(
            id: string,
            rootElement: HTMLElement,
            horizontal: boolean,
            dotnetObj: DotNetObject) {

            const instance = new VirtualizeInstance(rootElement, horizontal, dotnetObj);
            Virtualize._instances.set(id, instance);

            return instance.metrics();
        }

        public static syncMeasurements(id: string) {
            Virtualize._instances.get(id)?.syncMeasurements();
        }

        public static updateSticky(id: string) {
            Virtualize._instances.get(id)?.updateSticky();
        }

        public static scrollToOffset(id: string, offset: number, smooth: boolean) {
            Virtualize._instances.get(id)?.scrollToOffset(offset, smooth);
        }

        public static adjustScroll(id: string, delta: number) {
            Virtualize._instances.get(id)?.adjustScroll(delta);
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
    //     changes back to .NET (throttled to one notification per animation frame).
    //   * Measure rendered items with a ResizeObserver (dynamic-size mode) and report
    //     real sizes back to .NET in batches.
    //   * Provide programmatic scrolling and scroll-anchor correction so dynamic
    //     measurements never make the content visibly jump.
    class VirtualizeInstance {
        private _element: HTMLElement;
        private _horizontal: boolean;
        private _dotnetObj: DotNetObject;
        private _disposed = false;
        private _scrollScheduled = false;
        private _measureScheduled = false;
        // Set while the scroll gets adjusted programmatically, to suppress the resulting
        // scroll event from being treated as a user scroll.
        private _suppressScroll = false;
        // index -> element, tracks which items are currently observed.
        private _observed = new Map<number, Element>();
        private _pendingMeasures = new Map<number, number>(); // index -> size, batched until the next frame
        private _reported = new Map<number, number>(); // index -> last size reported to .NET, to skip duplicate reports
        // The pinned sticky header element, cached because _updateSticky runs synchronously on
        // every scroll event; re-resolved after renders (which can replace the element).
        private _stickyEl: HTMLElement | null = null;
        private _viewportObserver: ResizeObserver;
        private _itemObserver: ResizeObserver;

        constructor(element: HTMLElement, horizontal: boolean, dotnetObj: DotNetObject) {
            this._element = element;
            this._horizontal = horizontal;
            this._dotnetObj = dotnetObj;

            this._element.addEventListener('scroll', this._onScroll, { passive: true });

            // Track viewport resizes.
            this._viewportObserver = new ResizeObserver(() => this._onScroll());
            this._viewportObserver.observe(this._element);

            // Track item resizes (dynamic mode).
            this._itemObserver = new ResizeObserver(entries => this._onItemsResized(entries));
        }

        public metrics() {
            return this._horizontal
                ? { scrollOffset: this._element.scrollLeft, viewportSize: this._element.clientWidth }
                : { scrollOffset: this._element.scrollTop, viewportSize: this._element.clientHeight };
        }

        // Called by .NET after every render to keep the ResizeObserver subscriptions
        // in sync with the items that are actually in the DOM, and to take an
        // immediate measurement of newly rendered items.
        public syncMeasurements() {
            if (this._disposed) return;

            // Scoped to the own spacer so items of a nested BitVirtualize (rendered inside an
            // item, placeholder, or sticky template) never leak into this instance's measurements.
            const nodes = this._element.querySelectorAll(':scope > .bit-vir-spc > [data-bit-vir-index]');
            const present = new Set<number>();
            const indices: number[] = [];
            const sizes: number[] = [];

            nodes.forEach(node => {
                const index = parseInt(node.getAttribute('data-bit-vir-index')!, 10);
                present.add(index);
                if (this._observed.get(index) !== node) {
                    this._observed.set(index, node);
                    this._itemObserver.observe(node);
                }
                const size = this.measureElement(node);
                if (this._reported.get(index) !== size) {
                    this._reported.set(index, size);
                    indices.push(index);
                    sizes.push(size);
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

            if (indices.length > 0) {
                this._dotnetObj.invokeMethodAsync('ItemsMeasured', indices, sizes);
            }

            this._stickyEl = null; // the render may have replaced the sticky element
            this._updateSticky();
        }

        public scrollToOffset(offset: number, smooth: boolean) {
            if (this._disposed) return;

            const behavior = smooth ? 'smooth' : 'auto';
            if (this._horizontal) {
                this._element.scrollTo({ left: offset, behavior });
            } else {
                this._element.scrollTo({ top: offset, behavior });
            }
        }

        // Adjusts the scroll position by delta without emitting a user-scroll event.
        // Used for scroll anchoring after items above the viewport are re-measured.
        public adjustScroll(delta: number) {
            if (this._disposed || delta === 0) return;

            this._suppressScroll = true;
            if (this._horizontal) {
                this._element.scrollLeft += delta;
            } else {
                this._element.scrollTop += delta;
            }
            // Release the suppression after the scroll event has been dispatched.
            requestAnimationFrame(() => { this._suppressScroll = false; });
        }

        public dispose() {
            this._disposed = true;
            this._element.removeEventListener('scroll', this._onScroll);
            this._viewportObserver.disconnect();
            this._itemObserver.disconnect();
            this._observed.clear();
            this._pendingMeasures.clear();
            this._reported.clear();
            this._stickyEl = null;
        }

        private measureElement(el: Element) {
            const rect = el.getBoundingClientRect();
            return this._horizontal ? rect.width : rect.height;
        }

        private _onScroll = () => {
            if (this._disposed) return;

            // The pinned (sticky) header is positioned by css position:sticky, but the push-out
            // effect near the next group header has to track the scroll offset with no interop
            // latency, so it gets applied here synchronously on every scroll event.
            this._updateSticky();

            if (this._suppressScroll) return;

            if (!this._scrollScheduled) {
                this._scrollScheduled = true;
                requestAnimationFrame(this._flushScroll);
            }
        }

        // Pushes the pinned sticky header out of the way as the next group header (whose offset
        // .NET exposes through the data-bit-vir-sticky-next attribute) approaches the viewport edge.
        public updateSticky() {
            if (this._disposed) return;

            this._stickyEl = null; // the render may have replaced the sticky element
            this._updateSticky();
        }

        private _updateSticky() {
            let el = this._stickyEl;
            if (!el || !el.isConnected) {
                el = this._element.querySelector(':scope > .bit-vir-spc > .bit-vir-stk') as HTMLElement | null;
                this._stickyEl = el;
            }
            if (!el) return;

            const size = parseFloat(el.getAttribute('data-bit-vir-sticky-size') || '');
            const next = parseFloat(el.getAttribute('data-bit-vir-sticky-next') || '');

            let delta = 0;
            if (!isNaN(size) && !isNaN(next) && next >= 0) {
                const offset = this._horizontal ? this._element.scrollLeft : this._element.scrollTop;
                delta = Math.min(0, next - offset - size);
            }

            el.style.transform = this._horizontal ? `translateX(${delta}px)` : `translateY(${delta}px)`;
        }

        private _flushScroll = () => {
            this._scrollScheduled = false;
            if (this._disposed) return;

            const m = this.metrics();
            this._dotnetObj.invokeMethodAsync('Scroll', m.scrollOffset, m.viewportSize);
        }

        private _onItemsResized = (entries: ResizeObserverEntry[]) => {
            if (this._disposed) return;

            for (const entry of entries) {
                const idxAttr = entry.target.getAttribute('data-bit-vir-index');
                if (idxAttr === null) continue;

                const index = parseInt(idxAttr, 10);
                this._pendingMeasures.set(index, this.measureElement(entry.target));
            }

            if (!this._measureScheduled) {
                this._measureScheduled = true;
                requestAnimationFrame(this._flushMeasures);
            }
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
