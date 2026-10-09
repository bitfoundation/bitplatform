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
            horizontal: boolean,
            autoLoad: boolean,
            feed: boolean,
            dotnetObj: DotNetObject) {

            // The observer bakes its root, threshold and margin in at construction, so a setup that arrives
            // for an id that already has one is a change of those options: drop the old one first.
            InfiniteScrolling.dispose(id);

            const instance = new InfiniteScrollingInstance(scrollerSelector, rootElement, lastElement, threshold, rootMargin, horizontal, feed, dotnetObj);
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

        public static scrollToOffset(id: string, offset: number, smooth: boolean) {
            InfiniteScrolling._instances.get(id)?.scrollToOffset(offset, smooth);
        }

        public static getScrollOffset(id: string): number {
            return InfiniteScrolling._instances.get(id)?.getScrollOffset() ?? 0;
        }

        public static focusItem(id: string, index: number) {
            InfiniteScrolling._instances.get(id)?.focusItem(index);
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
    //   * Moves the focus between the articles of a feed with Page Up / Page Down, and out of it with
    //     Ctrl+Home / Ctrl+End, as the WAI-ARIA feed pattern describes.
    class InfiniteScrollingInstance {
        // Selectors that all mean "the page itself scrolls". The IntersectionObserver root has to be null
        // for those: passing document.body (which is not the scroll container) makes the sentinel intersect
        // the whole document at all times, so every page would be requested at once.
        private static readonly VIEWPORT_SELECTORS = ['window', 'document', 'body', 'html', ':root'];

        // The input types that hold no text to edit. Every other one has a caret its own Ctrl+Home / Ctrl+End move.
        private static readonly NON_TEXT_INPUTS = ['button', 'checkbox', 'color', 'file', 'image', 'radio', 'range', 'reset', 'submit'];

        private _observer: IntersectionObserver;
        // The second observer of a list that scrolls an element of its own (see the constructor).
        private _viewportObserver: IntersectionObserver | null = null;
        private _inScroller = false;
        private _inViewport = false;
        private _dotnetObj: DotNetObject;
        private _rootElement: HTMLElement;
        private _scroller: HTMLElement | null;   // null means the page viewport
        private _horizontal: boolean;
        private _lastElement: HTMLElement | null = null;
        private _disposed = false;
        private _busy = false;
        private _observing = false;
        private _pending = false;
        private _prevScrollSize = -1;
        private _keydownHandler: ((e: KeyboardEvent) => void) | null = null;

        constructor(
            scrollerSelector: string | undefined,
            rootElement: HTMLElement,
            lastElement: HTMLElement,
            threshold: number | undefined,
            rootMargin: string | undefined,
            horizontal: boolean,
            feed: boolean,
            dotnetObj: DotNetObject) {

            this._dotnetObj = dotnetObj;
            this._rootElement = rootElement;
            this._horizontal = !!horizontal;
            this._scroller = InfiniteScrollingInstance._resolveScroller(scrollerSelector, rootElement, lastElement);

            const options: IntersectionObserverInit = {
                root: this._scroller,
                rootMargin: rootMargin || '0px',
                // A zero threshold fires as soon as a single pixel of the sentinel shows up, which is what a
                // one-pixel sentinel needs; a ratio is only meaningful for a sentinel with a real size. Out
                // of range values are rejected by the constructor, so they are clamped rather than thrown.
                threshold: Math.min(1, Math.max(0, threshold ?? 0)),
            };

            this._observer = InfiniteScrollingInstance._createObserver(options, entries => this._onIntersect(entries, true));

            // An element only clips what it holds once something limits its size. A list given no height grows
            // with its items, so its sentinel is always inside it and an observer of that element alone would
            // ask for every page at once - long before the reader gets anywhere near them. So while the scroller
            // does not actually overflow, the sentinel also has to be inside the page viewport, which is what
            // turns a list of no height into one that loads as the page scrolls.
            if (this._scroller) {
                this._viewportObserver = InfiniteScrollingInstance._createObserver({ ...options, root: null }, entries => this._onIntersect(entries, false));
            }

            if (feed) {
                this._keydownHandler = e => this._onFeedKeydown(e);
                rootElement.addEventListener('keydown', this._keydownHandler);
            }
        }

        private static _createObserver(options: IntersectionObserverInit, callback: IntersectionObserverCallback): IntersectionObserver {
            try {
                return new IntersectionObserver(callback, options);
            } catch {
                // A malformed rootMargin is the only thing the constructor rejects here. Losing the margin is
                // better than losing the loading altogether.
                return new IntersectionObserver(callback, { ...options, rootMargin: '0px' });
            }
        }

        private static _resolveScroller(selector: string | undefined, rootElement: HTMLElement, lastElement: HTMLElement): HTMLElement | null {
            if (!selector) return rootElement;

            if (InfiniteScrollingInstance.VIEWPORT_SELECTORS.indexOf(selector.trim().toLowerCase()) >= 0) return null;

            let element: Element | null = null;
            try {
                element = document.querySelector(selector);
            } catch {
                // An invalid selector is not worth breaking the component over.
                return null;
            }

            // A component that named a scroller stops scrolling itself, so its own root no longer clips
            // anything: a sentinel measured against it would intersect for as long as the list exists and
            // ask for every page at once. The page viewport is the honest fallback.
            if (!element) return null;

            // The root of an IntersectionObserver has to be an ancestor of the observed element, otherwise
            // the callback simply never runs and the list silently stops loading.
            if (!element.contains(lastElement)) return null;

            return element as HTMLElement;
        }

        private async _onIntersect(entries: IntersectionObserverEntry[], fromScroller: boolean) {
            if (this._disposed || !this._observing || entries.length === 0) return;

            // Only the latest state counts: a batch may carry both the leave and the re-enter of a sentinel.
            const intersecting = entries[entries.length - 1].isIntersecting;

            if (fromScroller) {
                this._inScroller = intersecting;
            } else {
                this._inViewport = intersecting;
            }

            if (!this._inScroller) return;

            if (this._viewportObserver && !this._inViewport && !this._scrollerOverflows()) return;

            // .NET re-observes the sentinel from inside the Load call, so the intersection of a sentinel that
            // stayed in view is delivered while the previous page is still landing. An observer only reports
            // a change, so dropping it here would be the last word on it and the list would stall: it is
            // remembered instead, and replayed once the load it arrived during is done.
            if (this._busy) {
                this._pending = true;
                return;
            }

            await this._load();
        }

        private async _load() {
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

            if (!this._pending) return;

            this._pending = false;

            // .NET leaves the sentinel unobserved for everything it does not want loaded (an error, the end
            // of the data, the manual mode), so a replay only follows one it asked to keep watching.
            if (this._disposed || !this._observing) return;

            await this._load();
        }

        public observe(lastElement: HTMLElement) {
            if (this._disposed) return;

            this.unobserve();

            this._lastElement = lastElement;
            this._observing = true;
            // Every observe() delivers the current state of the sentinel, so both flags start out unknown.
            this._inScroller = false;
            this._inViewport = false;
            this._observer.observe(lastElement);
            this._viewportObserver?.observe(lastElement);
        }

        public unobserve() {
            this._observing = false;

            if (this._lastElement) {
                this._observer.unobserve(this._lastElement);
                this._viewportObserver?.unobserve(this._lastElement);
            }
        }

        // The scroll size of the container, recorded right before items get inserted above (or before) the
        // current ones so the insertion can be compensated for.
        public prepareScroll() {
            const el = this._scrollElement();
            if (!el) return;

            this._prevScrollSize = this._scrollSize(el);
        }

        public restoreScroll() {
            const el = this._scrollElement();
            if (!el || this._prevScrollSize < 0) return;

            const delta = this._scrollSize(el) - this._prevScrollSize;
            this._prevScrollSize = -1;

            if (delta === 0) return;

            // The offset is read here rather than beside the previous size: scroll anchoring is off, so the
            // browser leaves the offset alone while the content grows, and reading it now keeps a scroll the
            // user made during the load.
            this._setScrollOffset(el, this._scrollOffset(el) + delta);
        }

        public scrollTo(toEnd: boolean, smooth: boolean) {
            const el = this._scrollElement();
            if (!el) return;

            this._applyScroll(el, () => toEnd ? this._scrollSize(el) : 0, smooth);
        }

        public scrollToOffset(offset: number, smooth: boolean) {
            const el = this._scrollElement();
            if (!el) return;

            this._applyScroll(el, () => offset, smooth);
        }

        public getScrollOffset(): number {
            const el = this._scrollElement();
            if (!el) return 0;

            return this._scrollOffset(el);
        }

        public focusItem(index: number) {
            if (this._disposed) return;

            const articles = this._articles();
            const article = articles[Math.max(0, Math.min(index, articles.length - 1))];

            article?.focus();
        }

        // The keyboard of the WAI-ARIA feed pattern. Page Down / Page Up move between the articles from wherever
        // the focus is inside one, and Ctrl+End / Ctrl+Home leave the feed for the first focusable element after
        // or before it - the way out of a list that may never end. A key an inner handler has already used (the
        // feed nested in an article of this one, above all) is left alone.
        private _onFeedKeydown(e: KeyboardEvent) {
            if (this._disposed || e.defaultPrevented || e.altKey || e.metaKey) return;

            const articles = this._articles();
            if (articles.length === 0) return;

            const target = e.target as Element | null;

            // Every one of these keys belongs to a control that edits text of its own: Ctrl+Home / Ctrl+End move its
            // caret to the start or the end, Page Up / Page Down scroll it.
            if (InfiniteScrollingInstance._editsText(target)) return;

            // The keys belong to the feed only while the focus is in one of its articles: the button beside them is
            // already outside it, and Ctrl+End there would only find the button itself.
            const current = articles.findIndex(a => a === target || a.contains(target));
            if (current < 0) return;

            if (e.ctrlKey && (e.key === 'End' || e.key === 'Home')) {
                const outside = this._focusableOutside(e.key === 'End');
                if (!outside) return;

                e.preventDefault();
                outside.focus();
                return;
            }

            if (e.ctrlKey || e.shiftKey) return;

            let next = -1;
            if (e.key === 'PageDown') {
                next = Math.min(articles.length - 1, current + 1);
            } else if (e.key === 'PageUp') {
                next = Math.max(0, current - 1);
            }

            if (next < 0) return;

            // The key is taken even at either end, where the focus stays put, rather than scrolling the page
            // away from the article that has it.
            e.preventDefault();

            if (next !== current) {
                articles[next].focus();
            }
        }

        private static _editsText(target: Element | null): boolean {
            if (target instanceof HTMLTextAreaElement || target instanceof HTMLSelectElement) return true;

            if (target instanceof HTMLInputElement) return InfiniteScrollingInstance.NON_TEXT_INPUTS.indexOf(target.type) < 0;

            return (target as HTMLElement | null)?.isContentEditable === true;
        }

        // The box of the feed role, which holds the articles and nothing else.
        private _feedElement(): HTMLElement | null {
            return Array.from(this._rootElement.children).find(c => c.classList.contains('bit-isc-fed')) as HTMLElement | undefined ?? null;
        }

        private _articles(): HTMLElement[] {
            const feed = this._feedElement();
            if (!feed) return [];

            return Array.from(feed.children).filter(c => c.classList.contains('bit-isc-art')) as HTMLElement[];
        }

        // What is outside the feed is outside the box of its role, so the Load more / Retry button the root renders
        // after the articles is the first stop after them rather than skipped over.
        private _focusableOutside(after: boolean): HTMLElement | null {
            const feed = this._feedElement();
            if (!feed) return null;

            const all = Array.from(document.querySelectorAll<HTMLElement>(Extras.focusableSelector))
                .filter(el => !feed.contains(el) && Extras.isFocusable(el));

            const position = (el: HTMLElement) => feed.compareDocumentPosition(el);

            if (after) {
                return all.find(el => (position(el) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0) ?? null;
            }

            const before = all.filter(el => (position(el) & Node.DOCUMENT_POSITION_PRECEDING) !== 0);

            return before.length > 0 ? before[before.length - 1] : null;
        }

        // The target is recomputed on the second pass: a list whose images or fonts land after the first one
        // is taller by then, and the end of a chat that is scrolled to too early stops short of the newest item.
        private _applyScroll(el: HTMLElement, target: () => number, smooth: boolean) {
            // A reader who asked the system for less motion gets the jump rather than the glide, unless the
            // component (or an ancestor) was told to animate anyway (ForceAnimation).
            const reduced = matchMedia('(prefers-reduced-motion: reduce)').matches && !this._rootElement.closest('.bit-fam');
            const animate = smooth && !reduced;
            const apply = () => this._setScrollOffset(el, target(), animate);

            apply();

            if (animate) return;

            requestAnimationFrame(apply);
        }

        private _scrollElement(): HTMLElement | null {
            return this._scroller ?? (document.scrollingElement as HTMLElement | null) ?? document.documentElement;
        }

        // Whether the scroller clips anything along the scroll axis, which is what makes it the one that decides
        // what the reader can see. The pixel of slack absorbs the rounding of fractional sizes.
        private _scrollerOverflows(): boolean {
            const el = this._scroller;
            if (!el) return true;

            const clientSize = this._horizontal ? el.clientWidth : el.clientHeight;

            return this._scrollSize(el) > clientSize + 1;
        }

        private _scrollSize(el: HTMLElement): number {
            return this._horizontal ? el.scrollWidth : el.scrollHeight;
        }

        // Always the positive distance from the start of the list, so a right-to-left horizontal container
        // (whose scrollLeft counts backwards from zero) is measured the same way as every other one.
        private _scrollOffset(el: HTMLElement): number {
            return Math.abs(this._horizontal ? el.scrollLeft : el.scrollTop);
        }

        // The offset arrives as a positive distance from the start of the list and gets the sign the container
        // expects here, which is negative in a right-to-left horizontal one.
        private _setScrollOffset(el: HTMLElement, offset: number, smooth?: boolean) {
            const behavior: ScrollBehavior = smooth ? 'smooth' : 'auto';
            const distance = Math.max(0, offset);

            if (this._horizontal) {
                const rtl = getComputedStyle(el).direction === 'rtl';
                el.scrollTo({ left: rtl ? -distance : distance, behavior });
            }
            else {
                el.scrollTo({ top: distance, behavior });
            }
        }

        public dispose() {
            this._disposed = true;
            this._observing = false;
            this._pending = false;
            this._observer.disconnect();
            this._viewportObserver?.disconnect();
            this._lastElement = null;

            if (this._keydownHandler) {
                this._rootElement.removeEventListener('keydown', this._keydownHandler);
                this._keydownHandler = null;
            }
        }
    }
}
