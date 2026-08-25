namespace BitBlazorUI {
    // The options the swiper is driven with. They are handed over as a whole (both at setup and on
    // every change), so the instance never has to keep them in sync member by member.
    interface SwiperOptions {
        vertical: boolean;
        noDrag: boolean;
        wheel: boolean;
        enabled: boolean;
        snap: boolean;
        align: number;   // 0 = start, 0.5 = center, 1 = end
        duration: number;  // seconds
        threshold: number; // pixels the pointer has to travel before a drag starts
        scrollCount: number;
        start: number;     // the item the swiper is laid out on, read only at setup
    }

    interface SwiperState {
        index: number;
        page: number;
        pagesCount: number;
        atStart: boolean;
        atEnd: boolean;
        scrollable: boolean;
        viewport: number;
    }

    // The measurements every navigation is computed from. They are read fresh each time instead of
    // being cached, so items of different sizes (and items that changed size since the last look, an
    // image that finished loading for one) are always navigated against what is actually on screen.
    interface SwiperGeometry {
        rtl: boolean;
        scroll: number;
        viewport: number;
        max: number;
        positions: number[];
        sizes: number[];
    }

    export class Swiper {
        private static _swipers: Record<string, BitSwiper> = {};

        public static setup(
            id: string,
            root: HTMLElement,
            container: HTMLElement,
            dotnetObj: DotNetObject,
            options: SwiperOptions) {

            if (!root || !container) return;

            // A setup that lands on an id that is still registered (a re-render that re-created the
            // element, for one) would leave the previous listeners and observer behind.
            Swiper.dispose(id);

            const swiper = new BitSwiper(id, root, container, dotnetObj, options);

            Swiper._swipers[id] = swiper;

            swiper.init();
        }

        public static update(id: string, options: SwiperOptions) {
            Swiper._swipers[id]?.update(options);
        }

        public static refresh(id: string) {
            Swiper._swipers[id]?.refresh();
        }

        public static go(id: string, forward: boolean, count: number) {
            Swiper._swipers[id]?.go(forward, count);
        }

        public static goToItem(id: string, index: number) {
            Swiper._swipers[id]?.goToItem(index);
        }

        public static goToPage(id: string, page: number) {
            Swiper._swipers[id]?.goToPage(page);
        }

        public static goToEdge(id: string, end: boolean) {
            Swiper._swipers[id]?.goToEdge(end);
        }

        public static dispose(id: string) {
            const swiper = Swiper._swipers[id];
            if (!swiper) return;

            delete Swiper._swipers[id];

            swiper.dispose();
        }
    }

    class BitSwiper {
        private id: string;
        private root: HTMLElement;
        private container: HTMLElement;
        private dotnetObj: DotNetObject;
        private options: SwiperOptions;

        private ac = new AbortController();
        private observer: ResizeObserver | null = null;

        private animation = -1;
        private notification = -1;
        private lastKey = '';
        private direction = 'ltr';

        private dragging = false;
        private moved = false;
        private pointerId = -1;
        private startPointer = 0;
        private startScroll = 0;
        private lastPointer = 0;
        private lastMoveTime = 0;
        private velocity = 0;
        private lastWheel = 0;

        constructor(
            id: string,
            root: HTMLElement,
            container: HTMLElement,
            dotnetObj: DotNetObject,
            options: SwiperOptions) {

            this.id = id;
            this.root = root;
            this.container = container;
            this.dotnetObj = dotnetObj;
            this.options = options;
        }

        public init() {
            const signal = this.ac.signal;
            const container = this.container;

            // The scroll listener is passive: the swiper only reads the position the browser landed on,
            // it never takes the scroll away from it.
            container.addEventListener('scroll', this.onScroll, { signal, passive: true });

            container.addEventListener('pointerdown', this.onPointerDown, { signal });
            container.addEventListener('pointermove', this.onPointerMove, { signal });
            container.addEventListener('pointerup', this.onPointerUp, { signal });
            container.addEventListener('pointercancel', this.onPointerUp, { signal });
            container.addEventListener('lostpointercapture', this.onPointerUp, { signal });
            container.addEventListener('pointerleave', this.onPointerLeave, { signal });

            // Non-passive, since a wheel the swiper navigates with has to be taken from the page.
            container.addEventListener('wheel', this.onWheel, { signal, passive: false });

            try {
                this.observer = new ResizeObserver(() => this.scheduleNotify());
                this.observe();
            } catch (e) { console.error("BitBlazorUI.Swiper.init:", e); }

            this.readDirection();

            // The swiper is laid out on the item it was told to start on rather than scrolling over to it
            // after the fact, so nothing is ever seen sliding past on the way in.
            if (this.options.start > 0) {
                const g = this.geometry();

                if (g.positions.length > 0) {
                    const start = Math.min(this.options.start, g.positions.length - 1);

                    this.setScroll(this.clamp(this.alignedPos(g, start), 0, g.max));
                }
            }

            this.scheduleNotify();
        }

        public update(options: SwiperOptions) {
            if (!options) return;

            this.options = options;

            this.readDirection();

            this.scheduleNotify();
        }

        // Re-reads what the swiper holds. The observer watches the items themselves (not only the box
        // they scroll in), so a swiper whose items grew or shrank reports its new extent without anyone
        // having to ask; this is for the changes the observer cannot see, which is items being added to
        // or taken out of the swiper.
        public refresh() {
            this.observe();

            this.readDirection();

            this.scheduleNotify();
        }

        public go(forward: boolean, count: number) {
            const g = this.geometry();

            if (g.positions.length === 0) return;

            const current = this.indexAt(g, g.scroll);
            const step = Math.max(1, count || 1);

            const next = Math.max(0, Math.min(g.positions.length - 1, current + (forward ? step : -step)));

            let target = this.clamp(this.alignedPos(g, next), 0, g.max);

            // The last step of a swiper whose items do not divide evenly into screenfuls lands on an item
            // that is already at (or past) the current position, so it runs out to the end instead of
            // standing still, and the same the other way around at the start.
            if (forward && target <= g.scroll + 1) {
                target = g.max;
            } else if (forward === false && target >= g.scroll - 1) {
                target = 0;
            }

            this.animateTo(target);
        }

        public goToItem(index: number) {
            const g = this.geometry();

            if (g.positions.length === 0) return;

            const clamped = Math.max(0, Math.min(g.positions.length - 1, index));

            this.animateTo(this.clamp(this.alignedPos(g, clamped), 0, g.max));
        }

        public goToPage(page: number) {
            const g = this.geometry();
            const pages = this.pages(g);

            if (pages.length === 0) return;

            this.animateTo(pages[Math.max(0, Math.min(pages.length - 1, page))]);
        }

        public goToEdge(end: boolean) {
            const g = this.geometry();

            this.animateTo(end ? g.max : 0);
        }

        public dispose() {
            this.cancelAnimation();

            if (this.notification !== -1) {
                cancelAnimationFrame(this.notification);
                this.notification = -1;
            }

            try { this.observer?.disconnect(); } catch (e) { }
            this.observer = null;

            try { this.ac.abort(); } catch (e) { }

            this.dotnetObj?.dispose();
        }



        // #region geometry

        private items(): HTMLElement[] {
            const result: HTMLElement[] = [];
            const children = this.container.children;

            for (let i = 0; i < children.length; i++) {
                const child = children[i];

                // Only the items are measured. Anything else the content of the swiper holds would shift
                // every position a navigation is computed from away from the items the .NET side counted,
                // so an index would no longer point at the item it was asked for.
                if (child instanceof HTMLElement && child.classList.contains('bit-swpi')) {
                    result.push(child);
                }
            }

            return result;
        }

        private observe() {
            if (!this.observer) return;

            try {
                this.observer.disconnect();

                // The box the items scroll in decides how much of them fits...
                this.observer.observe(this.container);

                // ...and the items themselves decide how far the swiper scrolls, which an observer on the
                // container alone never sees: the content of a scroll container grows without the
                // container changing size at all.
                const items = this.items();
                for (let i = 0; i < items.length; i++) {
                    this.observer.observe(items[i]);
                }
            } catch (e) { console.error("BitBlazorUI.Swiper.observe:", e); }
        }

        private get vertical(): boolean {
            return this.options.vertical === true;
        }

        // The direction is read off the element rather than taken from the options, so the swiper follows
        // whatever it actually inherited (a dir attribute on an ancestor, for one). Reading it costs a
        // style resolution, and a drag (and every frame of an animation) asks for it over and over, so it
        // is read where the swiper is measured anyway and kept until the next measurement.
        private readDirection() {
            try {
                this.direction = window.getComputedStyle(this.container).direction === 'rtl' ? 'rtl' : 'ltr';
            } catch (e) {
                this.direction = 'ltr';
            }
        }

        private get rtl(): boolean {
            if (this.vertical) return false;

            return this.direction === 'rtl';
        }

        // The scroll position as a distance from the start of the content, which is the right edge in a
        // right-to-left swiper (where scrollLeft counts down from zero).
        private getScroll(): number {
            return this.vertical ? this.container.scrollTop : Math.abs(this.container.scrollLeft);
        }

        private setScroll(value: number) {
            if (this.vertical) {
                this.container.scrollTop = value;
            } else {
                this.container.scrollLeft = this.rtl ? -value : value;
            }
        }

        private geometry(): SwiperGeometry {
            this.readDirection();

            const container = this.container;
            const vertical = this.vertical;
            const rtl = this.rtl;
            const scroll = this.getScroll();

            const viewport = vertical ? container.clientHeight : container.clientWidth;
            const max = Math.max(0, vertical
                ? container.scrollHeight - container.clientHeight
                : container.scrollWidth - container.clientWidth);

            const positions: number[] = [];
            const sizes: number[] = [];

            const rect = container.getBoundingClientRect();
            const items = this.items();

            for (let i = 0; i < items.length; i++) {
                const r = items[i].getBoundingClientRect();

                // Measured against the start edge of the swiper (the top of a vertical one, the right of
                // a right-to-left one), and turned into a content position by adding the scroll offset.
                const offset = vertical
                    ? (r.top - rect.top)
                    : (rtl ? (rect.right - r.right) : (r.left - rect.left));

                positions.push(offset + scroll);
                sizes.push(vertical ? r.height : r.width);
            }

            return { rtl, scroll, viewport, max, positions, sizes };
        }

        // Where the swiper has to stand for the given item to sit where the snap alignment asks for: at
        // the start of the view, in its middle, or at its end. A swiper that does not snap aligns to the
        // start, which is what a rail of items scrolls to.
        private alignedPos(g: SwiperGeometry, index: number): number {
            const align = this.options.snap ? (this.options.align || 0) : 0;

            return g.positions[index] - align * (g.viewport - g.sizes[index]);
        }

        // The item the swiper is standing on is the one whose resting place is nearest to where it
        // actually is, which reads the same whichever alignment the items snap with.
        private indexAt(g: SwiperGeometry, scroll: number): number {
            let best = 0;
            let bestDistance = Number.MAX_VALUE;

            for (let i = 0; i < g.positions.length; i++) {
                const distance = Math.abs(this.clamp(this.alignedPos(g, i), 0, g.max) - scroll);

                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        // Where the swiper stands on each of its pages, a page being one screenful of items. They are laid
        // over the items rather than cut out of the scroll extent in viewport sized slices: a page starts
        // at the first item the page before it had no room for, so the last page is the one that ends the
        // swiper and standing at the end of the swiper is standing on it. Slicing the extent instead
        // leaves a sliver of a page at the end of every swiper whose items do not divide evenly into
        // screenfuls (the gaps between the items alone are enough for that), one the swiper can never
        // reach the start of, so its dot would never be the current one.
        private pages(g: SwiperGeometry): number[] {
            if (g.viewport <= 0) return [];

            const count = g.positions.length;

            if (count === 0) return [];
            if (g.max <= 1) return [0];

            const result: number[] = [];

            let i = 0;

            while (i < count) {
                const start = this.clamp(this.alignedPos(g, i), 0, g.max);

                // A page that comes to rest where the one before it did is the same page (the items at the
                // end of a swiper all rest at its end), so it is not given a dot of its own.
                if (result.length > 0 && start <= result[result.length - 1] + 1) break;

                result.push(start);

                if (start >= g.max) break;

                // The next page starts at the first item that no longer fits next to the one this page
                // starts with, and never before the item after it, so an item wider than the swiper still
                // moves it on rather than paging in place.
                const edge = g.positions[i] + g.viewport;

                let next = i + 1;

                while (next < count && g.positions[next] + g.sizes[next] <= edge + 1) {
                    next++;
                }

                i = next;
            }

            return result.length > 0 ? result : [0];
        }

        // The page the swiper is standing on is the one whose resting place is nearest to where it
        // actually is, which is how the item it stands on is read too.
        private pageAt(pages: number[], scroll: number): number {
            let best = 0;
            let bestDistance = Number.MAX_VALUE;

            for (let i = 0; i < pages.length; i++) {
                const distance = Math.abs(pages[i] - scroll);

                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = i;
                }
            }

            return best;
        }

        private clamp(value: number, min: number, max: number): number {
            if (!isFinite(value)) return min;

            return Math.max(min, Math.min(max, value));
        }

        // #endregion



        // #region animation

        // The duration the swiper actually moves with. It collapses to nothing when the operating system
        // or the browser asks for reduced motion, unless the swiper (or a subtree it sits in) opted out
        // of that with ForceAnimation, which is what the shared bit-fam class marks.
        private effectiveDuration(): number {
            const duration = this.options.duration;

            if (!(duration > 0)) return 0;

            try {
                if (window.matchMedia('(prefers-reduced-motion: reduce)').matches &&
                    this.root.classList.contains('bit-fam') === false &&
                    this.root.closest('.bit-fam') === null) {
                    return 0;
                }
            } catch (e) { }

            return duration;
        }

        private cancelAnimation() {
            if (this.animation === -1) return;

            cancelAnimationFrame(this.animation);
            this.animation = -1;

            this.container.classList.remove('bit-swp-anm');
        }

        private animateTo(target: number) {
            this.cancelAnimation();

            const from = this.getScroll();
            const delta = target - from;
            const duration = this.effectiveDuration() * 1000;

            if (Math.abs(delta) < 1 || duration <= 0) {
                this.setScroll(target);
                this.scheduleNotify();
                return;
            }

            // The browser snaps back to the nearest snap point after every scroll it is handed, which
            // would cut the animation short on its first frame, so the snapping steps aside while the
            // swiper moves itself and is handed back the moment it arrives.
            this.container.classList.add('bit-swp-anm');

            const start = performance.now();

            const step = (now: number) => {
                const t = Math.min(1, (now - start) / duration);

                // Ease out: the move leaves at speed and settles, which is how a flick of the swiper by
                // hand ends too, so navigating with the buttons and by dragging look like the same thing.
                this.setScroll(from + delta * (1 - Math.pow(1 - t, 3)));

                if (t < 1) {
                    this.animation = requestAnimationFrame(step);
                } else {
                    this.animation = -1;
                    this.container.classList.remove('bit-swp-anm');
                    this.scheduleNotify();
                }
            };

            this.animation = requestAnimationFrame(step);
        }

        // #endregion



        // #region events

        private onScroll = () => {
            this.scheduleNotify();
        };

        // A control inside an item is operated rather than dragged, so a press on one is left to it: the
        // press has to reach it for it to take the focus, and a drag of it is its own business.
        private isControl(target: EventTarget | null): boolean {
            if (target instanceof Element === false) return false;

            const control = (target as Element).closest(
                'button,a,input,textarea,select,[contenteditable]:not([contenteditable="false"])');

            return control !== null && this.container !== control && this.container.contains(control);
        }

        private onPointerDown = (e: PointerEvent) => {
            // A hand landing on the swiper takes it over, whichever pointer it came with: an animation
            // that kept running under a finger would fight the scrolling the browser does for it.
            this.cancelAnimation();

            // Touch and pen are left to the browser, which scrolls the swiper natively (with the
            // momentum and the snapping of the platform); only the mouse, which has no such scrolling
            // of its own, is dragged by hand here.
            if (e.pointerType !== 'mouse' || e.button !== 0) return;
            if (this.options.noDrag || this.options.enabled === false) return;
            if (this.isControl(e.target)) return;

            this.readDirection();

            this.dragging = true;
            this.moved = false;
            this.pointerId = e.pointerId;
            this.startPointer = this.vertical ? e.clientY : e.clientX;
            this.lastPointer = this.startPointer;
            this.lastMoveTime = e.timeStamp;
            this.velocity = 0;
            this.startScroll = this.getScroll();

            // Keeps the drag from starting the browser's own drag of an image (or a selection of the
            // text) inside the swiper. The event itself is left to travel on.
            e.preventDefault();
        };

        private onPointerMove = (e: PointerEvent) => {
            if (this.dragging === false || e.pointerId !== this.pointerId) return;

            const pointer = this.vertical ? e.clientY : e.clientX;
            const travelled = pointer - this.startPointer;

            if (this.moved === false) {
                if (Math.abs(travelled) < Math.max(1, this.options.threshold)) return;

                this.moved = true;

                // The pointer is captured only once the drag is real, so a plain click on an item is
                // never taken away from it. The snapping steps aside for the same reason it does during
                // an animation: it would fight every frame of the drag.
                try { this.container.setPointerCapture(e.pointerId); } catch (err) { }

                this.container.classList.add('bit-swp-drg');
            }

            const elapsed = Math.max(1, e.timeStamp - this.lastMoveTime);

            this.velocity = (pointer - this.lastPointer) / elapsed;
            this.lastPointer = pointer;
            this.lastMoveTime = e.timeStamp;

            // Dragging the content towards the start of a right-to-left swiper means moving it to the
            // right, which is the other way around from every other swiper.
            const sign = this.rtl ? 1 : -1;

            this.setScroll(this.clamp(this.startScroll + sign * travelled, 0, this.geometryMax()));
        };

        private onPointerUp = (e: PointerEvent) => {
            if (this.dragging === false || e.pointerId !== this.pointerId) return;

            this.dragging = false;
            this.pointerId = -1;

            if (this.moved === false) return;

            this.moved = false;

            try { this.container.releasePointerCapture(e.pointerId); } catch (err) { }

            this.container.classList.remove('bit-swp-drg');

            // The browser follows a drag with a click on whatever the pointer came to rest over, which would
            // open the link (or press the button) the swiper was just dragged by. The one click that belongs
            // to this drag is swallowed on the way down, before it reaches anything inside the swiper.
            //
            // A drag does not always end with a click (a cancelled one, or one whose pointer was let go
            // outside of the swiper, is followed by nothing), so the listener is taken off on the next
            // frame rather than left waiting for a click that would only come much later, from somewhere
            // else entirely.
            this.container.addEventListener('click', this.swallowClick, { capture: true, signal: this.ac.signal });

            requestAnimationFrame(() => {
                this.container.removeEventListener('click', this.swallowClick, { capture: true });
            });

            const g = this.geometry();
            const sign = this.rtl ? 1 : -1;

            // A flick carries on after the pointer is lifted, over roughly a fifth of a second of the
            // speed it was let go at, which is what makes a swipe feel like it has weight.
            const momentum = isFinite(this.velocity) ? sign * this.velocity * 200 : 0;

            let target = this.clamp(g.scroll + momentum, 0, g.max);

            // A snapping swiper comes to rest on an item rather than wherever the flick ran out, so the
            // place the momentum points at is rounded to the nearest resting place.
            if (this.options.snap && g.positions.length > 0) {
                target = this.clamp(this.alignedPos(g, this.indexAt(g, target)), 0, g.max);
            }

            this.velocity = 0;

            this.animateTo(target);
        };

        // A press that left the swiper before it ever became a drag is over: its release lands somewhere
        // else and would never reach the swiper, leaving it waiting for one and picking the drag back up
        // from a stale starting point the next time the pointer passed over it. A drag that did start
        // holds the pointer captured, so it keeps running wherever the pointer goes.
        private onPointerLeave = () => {
            if (this.dragging === false || this.moved) return;

            this.dragging = false;
            this.pointerId = -1;
        };

        private swallowClick = (e: MouseEvent) => {
            e.preventDefault();
            e.stopPropagation();
        };

        private geometryMax(): number {
            return Math.max(0, this.vertical
                ? this.container.scrollHeight - this.container.clientHeight
                : this.container.scrollWidth - this.container.clientWidth);
        }

        private onWheel = (e: WheelEvent) => {
            if (this.options.wheel === false || this.options.enabled === false) return;

            // A scroll that runs mostly along the swiper is one the browser already gives it (a trackpad
            // swiped sideways over a horizontal swiper), so it is left alone.
            if (Math.abs(e.deltaY) <= Math.abs(e.deltaX)) return;
            if (e.deltaY === 0) return;

            e.preventDefault();

            // A single flick of a trackpad fires a long tail of events, which would run through the
            // whole swiper at once without a quiet period between the moves.
            const now = e.timeStamp;
            const quiet = Math.max(200, this.effectiveDuration() * 1000);

            if (now - this.lastWheel < quiet) return;

            this.lastWheel = now;

            this.go(e.deltaY > 0, this.options.scrollCount);
        };

        // #endregion



        // #region state

        private scheduleNotify() {
            if (this.notification !== -1) return;

            // A scroll fires far more often than anything can be reported, so the look at where the
            // swiper stands is coalesced into the next frame.
            this.notification = requestAnimationFrame(() => {
                this.notification = -1;
                this.notify();
            });
        }

        private notify() {
            const state = this.state();

            // Only a state that actually reads differently is worth a round trip: a swiper being dragged
            // across a screenful fires hundreds of scroll events and moves through a handful of items.
            const key = `${state.index}|${state.page}|${state.pagesCount}|${state.atStart}|${state.atEnd}|${state.scrollable}|${Math.round(state.viewport)}`;

            if (key === this.lastKey) return;

            this.lastKey = key;

            try {
                // The call is not awaited (the swiper has nothing to do with what comes back), so the
                // rejection of a swiper that went away mid-flight is picked up here rather than being
                // left unhandled.
                this.dotnetObj.invokeMethodAsync('OnStateChange', state)
                    .catch(e => console.error("BitBlazorUI.Swiper.notify:", e));
            } catch (e) { console.error("BitBlazorUI.Swiper.notify:", e); }
        }

        private state(): SwiperState {
            const g = this.geometry();

            const scrollable = g.max > 1;
            const itemsCount = g.positions.length;

            const pages = this.pages(g);

            return {
                index: itemsCount > 0 ? this.indexAt(g, g.scroll) : 0,
                page: pages.length > 0 ? this.pageAt(pages, g.scroll) : 0,
                pagesCount: pages.length,
                atStart: g.scroll <= 1,
                atEnd: g.scroll >= g.max - 1,
                scrollable,
                viewport: g.viewport
            };
        }

        // #endregion
    }
}
