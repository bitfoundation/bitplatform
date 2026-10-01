namespace BitBlazorUI {
    export class PullToRefresh {
        private static _refreshers: Record<string, BitPullRefresher> = {};

        public static setup(
            id: string,
            anchor: HTMLElement | undefined,
            loadingEl: HTMLElement,
            scrollerElement: HTMLElement | undefined,
            scrollerSelector: string | undefined,
            trigger: number,
            factor: number,
            margin: number,
            threshold: number,
            maxPull: number,
            enabled: boolean,
            noMouse: boolean,
            dotnetObj: DotNetObject) {
            // An id that is already registered would otherwise leave the previous refresher's listeners on the
            // anchor forever, so a component re-created against the same id keeps a single live gesture.
            PullToRefresh.dispose(id);

            PullToRefresh._refreshers[id] = new BitPullRefresher(
                id,
                anchor ?? document.body,
                loadingEl,
                scrollerElement,
                scrollerSelector,
                { trigger, factor, margin, threshold, maxPull, enabled, noMouse },
                dotnetObj);
        }

        public static update(
            id: string,
            scrollerElement: HTMLElement | undefined,
            scrollerSelector: string | undefined,
            trigger: number,
            factor: number,
            margin: number,
            threshold: number,
            maxPull: number,
            enabled: boolean,
            noMouse: boolean) {
            PullToRefresh._refreshers[id]?.update(
                scrollerElement,
                scrollerSelector,
                { trigger, factor, margin, threshold, maxPull, enabled, noMouse });
        }

        public static async refresh(id: string) {
            await PullToRefresh._refreshers[id]?.refresh();
        }

        public static release(id: string) {
            PullToRefresh._refreshers[id]?.release();
        }

        public static dispose(id: string) {
            const refresher = PullToRefresh._refreshers[id];
            if (!refresher) return;

            delete PullToRefresh._refreshers[id];
            refresher.dispose();
        }
    }

    interface BitPullToRefreshOptions {
        trigger: number;
        factor: number;
        margin: number;
        threshold: number;
        maxPull: number;
        enabled: boolean;
        noMouse: boolean;
    }

    // How far the finger travels before the gesture decides whether it is a pull or a sideways swipe. Below
    // it nothing is reported and nothing is prevented, so the few pixels a horizontal scroller or a carousel
    // needs to claim the gesture are left to the browser.
    const AXIS_SLOP = 8;

    // What a mouse or a pen drags across to select text or to move a caret: a pull started there would take the
    // drag away from the field, so it is never claimed for one. A finger on a field at the top of the list still
    // pulls, the way it does in a native app.
    const EDITABLE = 'input, textarea, select, [contenteditable]:not([contenteditable="false"])';

    // A pull is one pointer travelling down. 0 while that is still undecided, 1 once it is a pull, -1 once the
    // gesture has been given up on - a sideways swipe, a second finger, a scroller that is no longer at its top.
    const enum BitPullAxis { Undecided = 0, Vertical = 1, Abandoned = -1 }

    class BitPullRefresher {
        readonly id: string;

        private readonly anchorEl: HTMLElement;
        private readonly loadingEl: HTMLElement;
        private readonly dotnetObj: DotNetObject;

        private scrollerEl: HTMLElement;
        private scrollerElement?: HTMLElement;
        private scrollerSelector?: string;
        private options: BitPullToRefreshOptions;

        private startX = 0;
        private startY = -1;
        private axis: BitPullAxis = BitPullAxis.Undecided;
        private diff = 0;
        private refreshing = false;
        private pointerId = -1;

        // The pull is reported to .NET at most once per frame, and never twice with the same rendered pixel:
        // a move event arrives far more often than a frame, and every report is an interop round trip that
        // re-renders the component - on a Blazor Server circuit, one over the network.
        private frameId = 0;
        private pendingDiff = -1;
        private reportedDiff = -1;
        private reporting = false;

        // The two inline styles the gesture writes on elements it does not own, remembered so that disposing
        // puts back whatever the application had there rather than blanking it.
        private readonly anchorTouchAction: string;
        private scrollerOverscroll = '';

        private resizeObserver?: ResizeObserver;
        private touchActionInEffect = '';
        private overscrollInEffect = '';

        constructor(id: string,
            anchorEl: HTMLElement,
            loadingEl: HTMLElement,
            scrollerElement: HTMLElement | undefined,
            scrollerSelector: string | undefined,
            options: BitPullToRefreshOptions,
            dotnetObj: DotNetObject) {
            this.id = id;
            this.anchorEl = anchorEl;
            this.loadingEl = loadingEl;
            this.dotnetObj = dotnetObj;
            this.options = BitPullRefresher.normalize(options);
            this.anchorTouchAction = anchorEl.style.touchAction;

            this.scrollerElement = scrollerElement;
            this.scrollerSelector = scrollerSelector;
            this.scrollerEl = this.resolveScroller();
            this.scrollerOverscroll = this.scrollerEl.style.overscrollBehaviorY;

            // Touch and pointer listeners are both registered, and the pointer ones step aside for a touch
            // pointer. Choosing between them up front on a "is this a touch device" answer left every
            // touch-capable laptop without mouse support, since such a device reports itself as touch and then
            // never sees a touch event from its mouse.
            this.anchorEl.addEventListener('touchstart', this.onTouchStart, { passive: true });
            // The only listener that has to stay non-passive: it is the one that prevents the browser's own
            // overscroll while the pull is being drawn.
            this.anchorEl.addEventListener('touchmove', this.onTouchMove, { passive: false });
            this.anchorEl.addEventListener('touchend', this.onTouchEnd, { passive: true });
            this.anchorEl.addEventListener('touchcancel', this.onTouchCancel, { passive: true });
            this.anchorEl.addEventListener('pointerdown', this.onPointerDown);
            this.anchorEl.addEventListener('pointermove', this.onPointerMove);
            this.anchorEl.addEventListener('pointerup', this.onPointerUp);
            this.anchorEl.addEventListener('pointercancel', this.onPointerCancel);
            this.anchorEl.addEventListener('lostpointercapture', this.onPointerCancel);
            this.anchorEl.addEventListener('selectstart', this.onSelectStart);

            this.bindScroller();

            // The loading strip is positioned out of the flow, so it has no width of its own to inherit. It is
            // kept in step with the anchor rather than measured once per pull, so that a resize, a rotation or
            // a zoom during a refresh - or a programmatic refresh before any pull has happened - still draws it
            // across the whole anchor.
            if (typeof ResizeObserver !== 'undefined') {
                this.resizeObserver = new ResizeObserver(() => this.syncWidth());
                this.resizeObserver.observe(this.anchorEl);
            } else {
                this.syncWidth();
            }
        }

        public update(scrollerElement: HTMLElement | undefined, scrollerSelector: string | undefined, options: BitPullToRefreshOptions) {
            this.options = BitPullRefresher.normalize(options);

            if (scrollerElement !== this.scrollerElement || scrollerSelector !== this.scrollerSelector) {
                this.scrollerElement = scrollerElement;
                this.scrollerSelector = scrollerSelector;

                const scrollerEl = this.resolveScroller();
                if (scrollerEl !== this.scrollerEl) {
                    this.unbindScroller();
                    this.scrollerEl = scrollerEl;
                    this.scrollerOverscroll = scrollerEl.style.overscrollBehaviorY;
                    this.overscrollInEffect = '';
                    this.bindScroller();
                }
            }

            if (!this.options.enabled && !this.refreshing) {
                this.reset();
                this.snapBack();
            }

            this.syncScrollStyles();
        }

        public async refresh() {
            if (!this.options.enabled || this.refreshing) return;

            this.refreshing = true;
            this.reset();

            try {
                this.syncWidth();
                // Nothing was pulled, so there is no indicator to keep at the size it had: the strip opens and the
                // indicator appears with the refreshing state the managed side renders, rather than first showing
                // the idle one at full size while that render is on its way.
                this.hold(false);

                await this.dotnetObj.invokeMethodAsync('Refresh');
            } finally {
                this.refreshing = false;
                this.snapBack();
            }
        }

        // Lets go of a refresh the managed side is done with, which calls it just before it renders the idle state:
        // the strip starts closing while the indicator still shows the refreshing or complete state it ended on,
        // instead of the hold keeping the idle one at full size until the refresh call has made it back here.
        public release() {
            if (!this.refreshing) return;

            this.snapBack();
        }

        public dispose() {
            this.cancelFrame();

            this.anchorEl.removeEventListener('touchstart', this.onTouchStart);
            this.anchorEl.removeEventListener('touchmove', this.onTouchMove);
            this.anchorEl.removeEventListener('touchend', this.onTouchEnd);
            this.anchorEl.removeEventListener('touchcancel', this.onTouchCancel);
            this.anchorEl.removeEventListener('pointerdown', this.onPointerDown);
            this.anchorEl.removeEventListener('pointermove', this.onPointerMove);
            this.anchorEl.removeEventListener('pointerup', this.onPointerUp);
            this.anchorEl.removeEventListener('pointercancel', this.onPointerCancel);
            this.anchorEl.removeEventListener('lostpointercapture', this.onPointerCancel);
            this.anchorEl.removeEventListener('selectstart', this.onSelectStart);

            this.unbindScroller();
            this.resizeObserver?.disconnect();
            this.releasePointer();

            this.anchorEl.style.touchAction = this.anchorTouchAction;
            this.anchorEl.classList.remove('bit-ptr-drg');
            this.loadingEl.style.minHeight = '';

            this.dotnetObj?.dispose();
        }



        // ---- gesture ----

        private onTouchStart = (e: TouchEvent) => {
            // Anything but a single finger is a pinch or a two-finger scroll, never a pull.
            if (e.touches.length !== 1) return this.abandon();

            this.start(e.touches[0].screenX, e.touches[0].screenY, e.target);
        };

        private onTouchMove = (e: TouchEvent) => {
            if (e.touches.length !== 1) return this.abandon();

            this.move(e, e.touches[0].screenX, e.touches[0].screenY);
        };

        private onTouchEnd = () => { void this.end(); };

        private onTouchCancel = () => { void this.cancel(); };

        private onPointerDown = (e: PointerEvent) => {
            // A touch pointer is already covered by the touch listeners above.
            if (e.pointerType === 'touch' || e.button !== 0) return;
            if (e.pointerType === 'mouse' && this.options.noMouse) return;
            if (e.target instanceof Element && e.target.closest(EDITABLE)) return;

            this.pointerId = e.pointerId;
            this.start(e.screenX, e.screenY, e.target);
        };

        private onPointerMove = (e: PointerEvent) => {
            if (e.pointerType === 'touch' || e.pointerId !== this.pointerId) return;

            this.move(e, e.screenX, e.screenY);
        };

        private onPointerUp = (e: PointerEvent) => {
            if (e.pointerType === 'touch' || e.pointerId !== this.pointerId) return;

            this.releasePointer();
            void this.end();
        };

        private onPointerCancel = (e: PointerEvent) => {
            if (e.pointerType === 'touch' || e.pointerId !== this.pointerId) return;

            this.releasePointer();
            void this.cancel();
        };

        // A selection the browser tries to start once a mouse pull has been claimed is refused outright: the
        // stylesheet alone does not stop a selection drag that was already under way when it took effect.
        private onSelectStart = (e: Event) => {
            if (this.isMousePull()) e.preventDefault();
        };

        private start(x: number, y: number, target: EventTarget | null) {
            if (!this.options.enabled || this.refreshing || this.getScrollTop() > 0 || this.isNestedScrollerAway(target)) {
                return this.abandon();
            }

            this.startX = x;
            this.startY = y;
            this.axis = BitPullAxis.Undecided;
            this.diff = 0;
            this.reportedDiff = -1;
            this.loadingEl.classList.remove('bit-ptr-rtn');

            const bcr = this.anchorEl.getBoundingClientRect();
            this.loadingEl.style.width = `${bcr.width}px`;

            this.invoke('OnStart', bcr.top, bcr.left, bcr.width);
        }

        private move(e: TouchEvent | PointerEvent, x: number, y: number) {
            if (this.startY === -1 || this.axis === BitPullAxis.Abandoned || this.refreshing) return;

            // A scroller that has left its top while the finger is down means the gesture belongs to the
            // scroller, not to the pull.
            if (this.getScrollTop() > 0) return this.abandon();

            const dx = x - this.startX;
            const dy = y - this.startY;

            if (this.axis === BitPullAxis.Undecided) {
                // Nothing is claimed until the finger has moved far enough to say which way it is going, so a
                // horizontal scroller or a carousel inside the anchor keeps the gestures that belong to it.
                if (Math.abs(dx) < AXIS_SLOP && Math.abs(dy) < AXIS_SLOP) return;
                if (Math.abs(dx) > Math.abs(dy) || dy <= 0) return this.abandon();

                this.axis = BitPullAxis.Vertical;

                // Taken only now, and not on pointerdown, so that a plain click or a sideways drag inside the
                // anchor is never retargeted. It is what keeps a mouse pull alive once the cursor leaves the
                // anchor, which used to cancel it.
                if (this.pointerId !== -1 && 'pointerId' in e) {
                    try { this.anchorEl.setPointerCapture(this.pointerId); } catch { /* the pointer is already gone */ }

                    // A mouse pull is a drag, and a drag over text selects it. The stylesheet and the selectstart
                    // listener keep the pull from starting a new selection.
                    this.anchorEl.classList.add('bit-ptr-drg');
                }
            }

            // What the slop above already selected is dropped, and so is whatever a selection drag the browser
            // had already begun goes on to extend while the pull runs.
            if (this.isMousePull()) {
                this.clearSelection();
            }

            if (dy <= 0) return this.abandon();

            if (dy <= this.options.threshold) {
                // Back inside the dead zone: drop the pull height so a release from here cannot trigger a
                // refresh with the distance the pull had before it came back.
                if (this.diff !== 0) {
                    this.diff = 0;
                    this.loadingEl.style.minHeight = '0';
                    this.paint(0);
                    this.queueMove(0);
                }
                return;
            }

            if (e.cancelable) {
                e.preventDefault();
                e.stopPropagation();
            }

            // Past the trigger the pull keeps following the finger up to the overpull limit, so that the
            // gesture does not go dead the moment it has done its job; without one the limit is the trigger
            // itself, which is what the component has always done.
            const limit = Math.max(this.options.maxPull, this.options.trigger);
            this.diff = Math.min((dy - this.options.threshold) / this.options.factor, limit);
            this.loadingEl.style.minHeight = `${this.pullHeight(this.diff)}px`;
            this.paint(this.diff);

            this.queueMove(this.diff);
        }

        private async end() {
            if (this.startY === -1 || this.refreshing) return;

            const diff = this.axis === BitPullAxis.Vertical ? this.diff : 0;
            const willRefresh = diff >= this.options.trigger;
            this.reset();

            // Claimed before the first round trip rather than between the two: a touch landing while the end
            // of the gesture is still being reported would otherwise start a second pull on top of the refresh
            // this one is about to run.
            this.refreshing = willRefresh;

            if (willRefresh) {
                this.hold();
            }

            try {
                await this.invoke('OnEnd', diff);

                if (willRefresh) {
                    await this.invoke('Refresh');
                }
            } finally {
                this.refreshing = false;
                this.snapBack();
            }
        }

        private async cancel() {
            if (this.startY === -1 || this.refreshing) return;

            const diff = this.axis === BitPullAxis.Vertical ? this.diff : 0;
            this.reset();
            this.snapBack();

            await this.invoke('OnCancel', diff);
        }

        // Gives up on the gesture: it turned out to be a scroll, a sideways swipe or a pinch. One that never
        // drew anything was never claimed, so there is nothing for the managed side to hear about; one that
        // did is a pull taken away before it was released, which is exactly a cancel - reporting it is what
        // keeps the managed pull height from staying behind at the distance the abandoned pull had reached.
        private abandon() {
            if (this.diff !== 0) {
                void this.cancel();
                return;
            }

            this.reset();
        }

        private reset() {
            this.cancelFrame();
            this.startY = -1;
            this.axis = BitPullAxis.Abandoned;
            this.diff = 0;
            this.pendingDiff = -1;
            this.reportedDiff = -1;
            this.releasePointer();
            this.anchorEl.classList.remove('bit-ptr-drg');
        }

        private releasePointer() {
            if (this.pointerId === -1) return;

            const pointerId = this.pointerId;
            this.pointerId = -1;
            try {
                if (this.anchorEl.hasPointerCapture(pointerId)) {
                    this.anchorEl.releasePointerCapture(pointerId);
                }
            } catch { /* the pointer is already gone */ }
        }

        private isMousePull() {
            return this.axis === BitPullAxis.Vertical && this.pointerId !== -1;
        }

        // Only a selection inside the anchor is the pull's doing; one elsewhere on the page is left alone.
        private clearSelection() {
            const selection = window.getSelection();
            if (!selection || selection.isCollapsed || selection.rangeCount === 0) return;

            if (selection.getRangeAt(0).intersectsNode(this.anchorEl)) {
                selection.removeAllRanges();
            }
        }

        private snapBack() {
            this.loadingEl.classList.add('bit-ptr-rtn');
            this.loadingEl.classList.remove('bit-ptr-hld');
            void this.loadingEl.offsetHeight;
            this.loadingEl.style.minHeight = '0';
            this.paint(0);
        }

        // Holds the strip open for a refresh that is about to run, settled at the trigger's height - an overpull is
        // let go of. For a released pull the indicator is also drawn at its full size whatever the managed side has
        // rendered so far: it is the strip that holds it, not the component's own refreshing class, so the round
        // trip that starts the refresh leaves no frame in which the indicator is drawn at the size the pull last
        // had. The managed side lets go of it (release) before it renders the end of the refresh.
        private hold(indicator = true) {
            this.loadingEl.classList.add('bit-ptr-rtn');
            if (indicator) {
                this.loadingEl.classList.add('bit-ptr-hld');
            }
            void this.loadingEl.offsetHeight;
            this.loadingEl.style.minHeight = `${this.pullHeight(this.options.trigger)}px`;
        }

        // Draws the indicator for a pull of the given (already damped) distance through three properties on the
        // strip that the stylesheet sizes, offsets and turns it with. They are written here rather than rendered
        // by the component, so that following the finger never re-renders it - and the whole anchor with it.
        private paint(diff: number) {
            const trigger = this.options.trigger;
            const visual = Math.min(diff, trigger);
            const style = this.loadingEl.style;

            style.setProperty('--bit-ptr-prg', `${Math.round(visual / trigger * 10000) / 10000}`);
            style.setProperty('--bit-ptr-off', `${diff / 2}px`);
            style.setProperty('--bit-ptr-rot', `${(visual - trigger) * 2}deg`);
        }

        // The height the strip is drawn at for a pull of the given (already damped) distance: the raw finger
        // travel that produced it, plus the configured margin.
        private pullHeight(diff: number) {
            return diff * this.options.factor + this.options.margin;
        }



        // ---- reporting ----

        private queueMove(diff: number) {
            this.pendingDiff = diff;

            if (this.frameId !== 0) return;

            this.frameId = requestAnimationFrame(() => {
                this.frameId = 0;
                void this.flushMove();
            });
        }

        private async flushMove() {
            // A report is still out; the value that arrived meanwhile is picked up when it comes back, so the
            // round trips are never allowed to interleave and land out of order.
            if (this.reporting || this.pendingDiff < 0) return;

            const diff = this.pendingDiff;
            const rounded = Math.round(diff);
            if (rounded === this.reportedDiff) return;

            this.reporting = true;
            this.reportedDiff = rounded;
            try {
                await this.invoke('OnMove', diff);
            } finally {
                this.reporting = false;
            }

            // The last frame of a fast pull is never dropped: whatever came in while the call was out is
            // reported now.
            if (this.pendingDiff >= 0 && Math.round(this.pendingDiff) !== this.reportedDiff) {
                this.queueMove(this.pendingDiff);
            }
        }

        private cancelFrame() {
            if (this.frameId === 0) return;

            cancelAnimationFrame(this.frameId);
            this.frameId = 0;
        }

        private async invoke(method: string, ...args: any[]) {
            try {
                await this.dotnetObj.invokeMethodAsync(method, ...args);
            } catch (e) {
                // The circuit or the component is gone; a pull that can no longer be reported is not an error
                // the page should see.
                console.error('BitBlazorUI.PullToRefresh:', e);
            }
        }



        // ---- scroller ----

        private resolveScroller(): HTMLElement {
            if (this.scrollerElement) return this.scrollerElement;

            if (this.scrollerSelector) {
                const el = this.anchorEl.querySelector(this.scrollerSelector) ?? document.querySelector(this.scrollerSelector);
                if (el) return el as HTMLElement;
            }

            // The loading strip is a child of the anchor too, and it never scrolls - taking it for the scroller
            // would leave the gesture reading a scrollTop that is always zero.
            const first = this.anchorEl.firstElementChild;

            return (first && first !== this.loadingEl) ? first as HTMLElement : this.anchorEl;
        }

        // The document's scroll offset does not live on the element that is styled as the scroller: in
        // standards mode body.scrollTop stays 0 however far the page is scrolled, which used to leave a
        // whole-page pull to refresh permanently at "the top". A rubber-banding iOS scroller also reports a
        // negative offset, which is still the top as far as the pull is concerned.
        private getScrollTop() {
            const el = this.scrollerEl;

            return (el === document.body || el === document.documentElement)
                ? (window.scrollY || document.documentElement.scrollTop || document.body.scrollTop)
                : el.scrollTop;
        }

        // A scrollable element between the pointer and the scroller that is not at its own top - a nested list, a
        // code block, a text area - takes a downward drag for its own scrolling, the way the browser hands a scroll to
        // the innermost scroller first; pulling there would refresh the page instead of scrolling it back up.
        private isNestedScrollerAway(target: EventTarget | null) {
            let el = target instanceof Element ? target : null;

            while (el && el !== this.scrollerEl && el !== this.anchorEl) {
                if (el.scrollTop > 0) {
                    const overflowY = getComputedStyle(el).overflowY;
                    if (overflowY === 'auto' || overflowY === 'scroll') return true;
                }

                el = el.parentElement;
            }

            return false;
        }

        private bindScroller() {
            this.scrollerEl.addEventListener('scroll', this.onScroll, { passive: true });
            this.syncScrollStyles();
        }

        private unbindScroller() {
            this.scrollerEl.removeEventListener('scroll', this.onScroll);
            this.scrollerEl.style.overscrollBehaviorY = this.scrollerOverscroll;
        }

        private onScroll = () => this.syncScrollStyles();

        // Written only when the value actually changes: a scroll handler that assigns an inline style on every
        // event makes the browser recalculate styles for the whole subtree at scroll speed.
        private syncScrollStyles() {
            const touchAction = (this.options.enabled && this.getScrollTop() <= 0) ? 'pan-x pan-down pinch-zoom' : this.anchorTouchAction;
            if (touchAction !== this.touchActionInEffect) {
                this.touchActionInEffect = touchAction;
                this.anchorEl.style.touchAction = touchAction;
            }

            const overscroll = this.options.enabled ? 'contain' : this.scrollerOverscroll;
            if (overscroll !== this.overscrollInEffect) {
                this.overscrollInEffect = overscroll;
                this.scrollerEl.style.overscrollBehaviorY = overscroll;
            }
        }

        private syncWidth() {
            this.loadingEl.style.width = `${this.anchorEl.getBoundingClientRect().width}px`;
        }



        // A factor of zero divides the pull distance by nothing and a negative one pulls the indicator
        // upwards, so the numbers the managed side sends are held inside the range the gesture can draw. The
        // same clamps are applied there, so the height js draws and the size the component renders agree.
        private static normalize(options: BitPullToRefreshOptions): BitPullToRefreshOptions {
            return {
                trigger: Math.max(options.trigger || 0, 1),
                factor: Math.max(options.factor || 0, 0.1),
                margin: Math.max(options.margin || 0, 0),
                threshold: Math.max(options.threshold || 0, 0),
                maxPull: Math.max(options.maxPull || 0, 0),
                enabled: options.enabled,
                noMouse: options.noMouse,
            };
        }
    }
}
