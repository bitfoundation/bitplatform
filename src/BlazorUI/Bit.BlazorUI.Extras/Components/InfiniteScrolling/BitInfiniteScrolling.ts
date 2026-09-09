namespace BitBlazorUI {
    export class InfiniteScrolling {
        private static _instances = new Map<string, InfiniteScrollingInstance>();

        public static setup(
            id: string,
            scrollerSelector: string | undefined,
            rootElement: HTMLElement,
            lastElement: HTMLElement,
            threshold: number | undefined,
            rootMargin: string | undefined,
            autoLoad: boolean,
            dotnetObj: DotNetObject) {

            // The observer bakes its root, threshold and margin in at construction, so a setup that arrives
            // for an id that already has one is a change of those options: drop the old one first.
            InfiniteScrolling.dispose(id);

            const instance = new InfiniteScrollingInstance(scrollerSelector, rootElement, lastElement, threshold, rootMargin, dotnetObj);
            InfiniteScrolling._instances.set(id, instance);

            if (autoLoad) {
                instance.observe(lastElement);
            }
        }

        public static reobserve(id: string, lastElement: HTMLElement) {
            InfiniteScrolling._instances.get(id)?.observe(lastElement);
        }

        public static unobserve(id: string) {
            InfiniteScrolling._instances.get(id)?.unobserve();
        }

        public static prepareScroll(id: string) {
            InfiniteScrolling._instances.get(id)?.prepareScroll();
        }

        public static restoreScroll(id: string) {
            InfiniteScrolling._instances.get(id)?.restoreScroll();
        }

        public static scrollTo(id: string, toEnd: boolean, smooth: boolean) {
            InfiniteScrolling._instances.get(id)?.scrollTo(toEnd, smooth);
        }

        public static dispose(id: string) {
            const instance = InfiniteScrolling._instances.get(id);
            if (!instance) return;

            instance.dispose();
            InfiniteScrolling._instances.delete(id);
        }
    }

    // The browser-side half of the BitInfiniteScrolling component. One instance per component:
    //   * Watches the sentinel element with an IntersectionObserver and asks .NET for the next page as
    //     soon as it enters the scroll viewport.
    //   * Resolves the scroll container, so a component that scrolls with the page observes the viewport
    //     itself rather than an element that never clips anything.
    //   * Records and restores the scroll geometry around a prepend, which is what keeps the reversed
    //     (chat) mode from jumping while older items are inserted above the current one.
    class InfiniteScrollingInstance {
        // Selectors that all mean "the page itself scrolls". The IntersectionObserver root has to be null
        // for those: passing document.body (which is not the scroll container) makes the sentinel intersect
        // the whole document at all times, so every page would be requested at once.
        private static readonly VIEWPORT_SELECTORS = ['window', 'document', 'body', 'html', ':root'];

        private _observer: IntersectionObserver;
        private _dotnetObj: DotNetObject;
        private _scroller: HTMLElement | null;   // null means the page viewport
        private _lastElement: HTMLElement | null = null;
        private _disposed = false;
        private _busy = false;
        private _prevScrollHeight = -1;
        private _prevScrollTop = 0;

        constructor(
            scrollerSelector: string | undefined,
            rootElement: HTMLElement,
            lastElement: HTMLElement,
            threshold: number | undefined,
            rootMargin: string | undefined,
            dotnetObj: DotNetObject) {

            this._dotnetObj = dotnetObj;
            this._scroller = InfiniteScrollingInstance._resolveScroller(scrollerSelector, rootElement, lastElement);

            const options: IntersectionObserverInit = {
                root: this._scroller,
                rootMargin: rootMargin || '0px',
                // A zero threshold fires as soon as a single pixel of the sentinel shows up, which is what a
                // one-pixel sentinel needs; a ratio is only meaningful for a sentinel with a real height.
                threshold: threshold ?? 0,
            };

            try {
                this._observer = new IntersectionObserver(entries => this._onIntersect(entries), options);
            } catch {
                // A malformed rootMargin is the only thing the constructor rejects here. Losing the margin is
                // better than losing the loading altogether.
                this._observer = new IntersectionObserver(entries => this._onIntersect(entries), { ...options, rootMargin: '0px' });
            }
        }

        private static _resolveScroller(selector: string | undefined, rootElement: HTMLElement, lastElement: HTMLElement): HTMLElement | null {
            if (!selector) return rootElement;

            if (InfiniteScrollingInstance.VIEWPORT_SELECTORS.indexOf(selector.trim().toLowerCase()) >= 0) return null;

            let element: Element | null = null;
            try {
                element = document.querySelector(selector);
            } catch {
                // An invalid selector is not worth breaking the component over: fall back to the root element.
                return rootElement;
            }

            if (!element) return rootElement;

            // The root of an IntersectionObserver has to be an ancestor of the observed element, otherwise
            // the callback simply never runs and the list silently stops loading.
            if (!element.contains(lastElement)) return rootElement;

            return element as HTMLElement;
        }

        private async _onIntersect(entries: IntersectionObserverEntry[]) {
            if (this._disposed || this._busy) return;

            if (!entries.some(e => e.isIntersecting)) return;

            this._busy = true;
            this.unobserve();

            try {
                await this._dotnetObj.invokeMethodAsync('Load');
            } catch {
                // The component may be gone (or its circuit disconnected) by the time this resolves; .NET
                // re-observes the sentinel itself when it is ready for the next page.
            } finally {
                this._busy = false;
            }
        }

        public observe(lastElement: HTMLElement) {
            if (this._disposed) return;

            this.unobserve();

            this._lastElement = lastElement;
            this._observer.observe(lastElement);
        }

        public unobserve() {
            if (this._lastElement) {
                this._observer.unobserve(this._lastElement);
            }
        }

        // The scroll geometry of the container, recorded right before items get inserted above the current
        // ones so the insertion can be compensated for.
        public prepareScroll() {
            const el = this._scrollElement();
            if (!el) return;

            this._prevScrollHeight = el.scrollHeight;
            this._prevScrollTop = el.scrollTop;
        }

        public restoreScroll() {
            const el = this._scrollElement();
            if (!el || this._prevScrollHeight < 0) return;

            const delta = el.scrollHeight - this._prevScrollHeight;
            this._prevScrollHeight = -1;

            if (delta === 0) return;

            el.scrollTop = this._prevScrollTop + delta;
        }

        public scrollTo(toEnd: boolean, smooth: boolean) {
            const el = this._scrollElement();
            if (!el) return;

            el.scrollTo({ top: toEnd ? el.scrollHeight : 0, behavior: smooth ? 'smooth' : 'auto' });
        }

        private _scrollElement(): HTMLElement | null {
            return this._scroller ?? (document.scrollingElement as HTMLElement | null) ?? document.documentElement;
        }

        public dispose() {
            this._disposed = true;
            this._observer.disconnect();
            this._lastElement = null;
        }
    }
}
