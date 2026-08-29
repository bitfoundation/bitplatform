namespace BitBlazorUI {
    // The options the browser side of a pane is driven with. They are handed over as a whole (both at
    // setup and on every change), so the instance never has to keep them in sync member by member.
    interface ScrollablePaneOptions {
        fade: boolean;
        offset: number;   // how near an edge counts as reaching it, in pixels
        throttle: number; // the shortest interval between two scroll reports, or 0 for one per frame
        scroll: boolean;
        scrollStart: boolean;
        scrollEnd: boolean;
        top: boolean;
        bottom: boolean;
        left: boolean;
        right: boolean;
        autoScroll: boolean;
        autoScrollThreshold: number;
        smooth: boolean;
        drag: boolean;
        wheel: boolean;
    }

    interface ScrollOffset {
        left: number;
        top: number;
        scrollWidth: number;
        scrollHeight: number;
        clientWidth: number;
        clientHeight: number;
        rtl: boolean;
    }

    // Where the pane stands along both axes, in the direction independent terms everything below is
    // decided by: `x` runs from 0 at the visual left edge of the content to `maxX` at its right one,
    // whichever way the pane reads.
    interface ScrollMetrics {
        x: number;
        y: number;
        maxX: number;
        maxY: number;
        rtl: boolean;
    }

    type ScrollEdge = 'top' | 'bottom' | 'left' | 'right';

    type ScrollAlignment = 'start' | 'center' | 'end' | 'nearest';

    // What a drag of the pane started from: where the pointer went down, where the pane stood then, and
    // whether the pointer has since moved far enough for this to be a drag rather than a click.
    interface ScrollDrag {
        id: number;
        x: number;
        y: number;
        left: number;
        top: number;
        moved: boolean;
    }

    export class ScrollablePane {
        private static _panes: Record<string, BitScrollablePane> = {};

        public static setup(id: string, element: HTMLElement, dotnetObj: DotNetObject, options: ScrollablePaneOptions) {
            if (!element) return;

            // A setup landing on an id that is still registered (a re-render that re-created the element,
            // for one) would leave the previous listener and observers behind.
            ScrollablePane.dispose(id);

            const pane = new BitScrollablePane(element, dotnetObj, options);

            ScrollablePane._panes[id] = pane;

            pane.init();
        }

        public static update(id: string, options: ScrollablePaneOptions) {
            ScrollablePane._panes[id]?.update(options);
        }

        // Re-measures the pane and reports whatever changed. The instance watches the element and its
        // content on its own, so this is only for the changes neither observer can see - a font that
        // finished loading, an image that settled - and for the render .NET has just finished.
        public static refresh(id: string) {
            ScrollablePane._panes[id]?.refresh();
        }

        // Pins the pane to the end of its content, but only while it was left standing there: a reader
        // who scrolled up to look at something is not dragged back down by the next arrival. `force`
        // pins it regardless, which is what the first application of AutoScroll does.
        public static autoScroll(id: string, force: boolean) {
            ScrollablePane._panes[id]?.autoScroll(force);
        }

        public static dispose(id: string) {
            const pane = ScrollablePane._panes[id];
            if (!pane) return;

            pane.dispose();

            delete ScrollablePane._panes[id];
        }



        // The calls below act on an element rather than on a registered pane, so the public scrolling API
        // of the component works whether or not anything asked for a browser side instance.

        // The two ends are the ends of the CONTENT rather than the two sides of the screen, so the
        // horizontal one of them is the visual left edge of a right to left pane and the visual right
        // edge of every other one.
        public static scrollToEnd(element: HTMLElement, smooth?: boolean) {
            if (!element) return;

            try {
                const m = ScrollablePane.measure(element);
                ScrollablePane.apply(element, m.rtl ? 0 : m.maxX, m.maxY, m, smooth);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.scrollToEnd:", e); }
        }

        public static scrollToStart(element: HTMLElement, smooth?: boolean) {
            if (!element) return;

            try {
                const m = ScrollablePane.measure(element);
                ScrollablePane.apply(element, m.rtl ? m.maxX : 0, 0, m, smooth);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.scrollToStart:", e); }
        }

        // A null offset leaves that axis where it stands, which is what makes this one call able to
        // serve "scroll to the top", "scroll to this column" and "scroll to both" alike.
        public static scrollTo(element: HTMLElement, left: number | null, top: number | null, smooth?: boolean) {
            if (!element) return;

            try {
                const m = ScrollablePane.measure(element);
                ScrollablePane.apply(element, left ?? m.x, top ?? m.y, m, smooth);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.scrollTo:", e); }
        }

        public static scrollBy(element: HTMLElement, x: number, y: number, smooth?: boolean) {
            if (!element) return;

            try {
                const m = ScrollablePane.measure(element);
                ScrollablePane.apply(element, m.x + (x || 0), m.y + (y || 0), m, smooth);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.scrollBy:", e); }
        }

        // Brings a descendant of the pane into view by scrolling the pane itself, rather than by calling
        // scrollIntoView on the element: that would scroll every scrolling ancestor the pane sits in as
        // well, which moves the page under a reader who only asked the pane to move.
        public static scrollToElement(element: HTMLElement, targetId: string, offset: number, smooth?: boolean, alignment?: ScrollAlignment) {
            if (!element || !targetId) return;

            try {
                const target = element.querySelector<HTMLElement>(`#${CSS.escape(targetId)}`);
                if (!target) return;

                const m = ScrollablePane.measure(element);
                const paneRect = element.getBoundingClientRect();
                const rect = target.getBoundingClientRect();

                // Without an offset of its own the move keeps whatever room scroll-padding asks for, which
                // is what the browser's own scrolling honors: a pane with a sticky header of its own gets
                // the same clearance here that a fragment navigation into it would have got.
                const padding = offset ? { x: offset, y: offset } : ScrollablePane.scrollPadding(element);

                // Where the element stands inside the content of the pane, which is where it is on the
                // screen taken back to the top left corner of the scrolled content.
                const x = m.x + (rect.left - paneRect.left) - element.clientLeft;
                const y = m.y + (rect.top - paneRect.top) - element.clientTop;

                ScrollablePane.apply(element,
                    ScrollablePane.align(x, rect.width, m.x, element.clientWidth, padding.x, alignment, m.rtl),
                    ScrollablePane.align(y, rect.height, m.y, element.clientHeight, padding.y, alignment),
                    m, smooth);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.scrollToElement:", e); }
        }

        // Where the pane has to stand along one axis for an element to be left where the alignment asks
        // for. `at` is where the element starts in the content, `size` how long it is along this axis,
        // `from` where the pane stands now and `view` how much of the axis it shows.
        private static align(at: number, size: number, from: number, view: number, padding: number, alignment?: ScrollAlignment, rtl?: boolean): number {
            const start = at - padding;

            // The start and the end of an axis are the start and the end of the CONTENT, so the two of
            // them are the other way round on the horizontal axis of a right to left pane.
            if (rtl && alignment === 'start') return at + size - view + padding;
            if (rtl && alignment === 'end') return start;

            switch (alignment) {
                case 'center':
                    return at - (view - size) / 2;

                case 'end':
                    return at + size - view + padding;

                case 'nearest':
                    // An element that is already in view is not moved to at all, and one that is not is
                    // brought only as far as the edge it fell off, which is the least the pane can move.
                    if (start < from) return start;
                    if (at + size + padding > from + view) return at + size - view + padding;
                    return from;

                default:
                    return start;
            }
        }

        // The scroll-padding of the pane in pixels, along the two axes a move can be made on. The property
        // is `auto` unless it was set, and a percentage resolves against the box, so both are asked of the
        // computed style rather than parsed out of the declaration.
        private static scrollPadding(element: HTMLElement): { x: number, y: number } {
            const styles = getComputedStyle(element);
            const read = (value: string, against: number) => {
                if (!value || value === 'auto') return 0;
                if (value.endsWith('%')) return (parseFloat(value) || 0) * against / 100;
                return parseFloat(value) || 0;
            };

            const rtl = styles.direction === 'rtl';

            return {
                x: read(rtl ? styles.scrollPaddingRight : styles.scrollPaddingLeft, element.clientWidth),
                y: read(styles.scrollPaddingTop, element.clientHeight),
            };
        }

        // `rtl` is taken from a measurement that has already been made where there is one, since asking
        // the computed style for it is a style read and a reporting pane is measured on every frame of
        // every scroll.
        public static getOffset(element: HTMLElement, rtl?: boolean): ScrollOffset | null {
            if (!element) return null;

            try {
                const scrollWidth = element.scrollWidth;
                const clientWidth = element.clientWidth;

                return {
                    left: element.scrollLeft,
                    top: element.scrollTop,
                    scrollWidth,
                    scrollHeight: element.scrollHeight,
                    clientWidth,
                    clientHeight: element.clientHeight,
                    // Which way the pane reads is what tells a scrollLeft of 0 at the visual left edge of a
                    // left to right pane from the same 0 at the visual RIGHT edge of a right to left one.
                    // It is only asked for where there is something to scroll sideways, since an axis with
                    // nothing to scroll reads the same either way.
                    rtl: rtl ?? (scrollWidth > clientWidth && getComputedStyle(element).direction === 'rtl'),
                };
            } catch (e) {
                console.error("BitBlazorUI.ScrollablePane.getOffset:", e);
                return null;
            }
        }



        // Reads the pane in the direction independent terms the rest of this file works in. A right to
        // left pane counts scrollLeft down from 0 at its right edge into the negatives, so the visual
        // left edge sits at -maxX there and the sign is folded away here rather than at every use.
        public static measure(element: HTMLElement): ScrollMetrics {
            const maxX = Math.max(0, element.scrollWidth - element.clientWidth);
            const maxY = Math.max(0, element.scrollHeight - element.clientHeight);
            // Which way the pane reads only matters where there is something to scroll sideways, and asking
            // for it is a style read on every frame of every scroll of every pane that has not.
            const rtl = maxX > 0 && getComputedStyle(element).direction === 'rtl';
            const x = rtl ? maxX + element.scrollLeft : element.scrollLeft;

            return { x: Math.min(Math.max(x, 0), maxX), y: element.scrollTop, maxX, maxY, rtl };
        }

        // Scrolls to a direction independent position, turning it back into the signed scrollLeft the
        // browser expects. 'instant' rather than 'auto' is passed for an unanimated move, since 'auto'
        // defers to the scroll-behavior of the element - which the Smooth parameter may have set.
        public static apply(element: HTMLElement, x: number, y: number, m: ScrollMetrics, smooth?: boolean) {
            try {
                const clampedX = Math.min(Math.max(x, 0), m.maxX);
                const left = m.rtl ? clampedX - m.maxX : clampedX;
                const behavior = (smooth && ScrollablePane.animates(element)) ? 'smooth' : 'instant';

                element.scrollTo({ left, top: Math.min(Math.max(y, 0), m.maxY), behavior });
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.apply:", e); }
        }

        // Whether an animated move is wanted here at all. The stylesheet takes scroll-behavior back off
        // under the reduced motion preference, but a behavior passed to scrollTo overrides the property
        // rather than reading it - so the preference has to be asked about here as well, or the moves this
        // component makes itself would be the one animation on the page that ignored it. A pane carrying
        // the library's force-animation class has opted out of the preference and keeps the animation.
        private static animates(element: HTMLElement): boolean {
            try {
                if (element.classList.contains('bit-fam')) return true;

                return matchMedia('(prefers-reduced-motion: reduce)').matches === false;
            } catch {
                return true;
            }
        }
    }

    class BitScrollablePane {
        private _element: HTMLElement;
        private _dotnetObj: DotNetObject;
        private _options: ScrollablePaneOptions;

        private _abortController?: AbortController;
        private _resizeObserver?: ResizeObserver;
        private _mutationObserver?: MutationObserver;

        // The two optional interactions are bound and unbound as their flags flip, so a pane that never
        // asked for either pays for neither - the wheel one most of all, since it cannot be passive.
        private _dragAbortController?: AbortController;
        private _wheelAbortController?: AbortController;

        private _frame = 0;
        private _reportTimer = 0;
        private _lastReport = 0;
        private _disposed = false;

        // The position that was last reported to .NET, so the same one is never reported twice.
        private _lastSignature = '';

        // Whether a scroll is currently running, and the fallback that decides it has stopped where the
        // browser has no scrollend event of its own to say so.
        private _scrolling = false;
        private _endTimer = 0;

        // Whether the pane was left standing at the end of each axis, which is the whole of what decides
        // if the next arrival of content pins it there. Both start out true, so a pane that is set up
        // with content already in it is pinned by its first AutoScroll rather than left at the top.
        private _stickX = true;
        private _stickY = true;

        // Set while a pinning scroll of our own is on its way to the end, so the scroll events it raises
        // along the way are not mistaken for a reader scrolling away from it.
        private _pinning = false;
        private _pinTimer = 0;

        // The drag that is currently under way, if the pane is being dragged at all.
        private _drag?: ScrollDrag;

        // Which edges the pane is standing at, so each one is reported as it is reached rather than on
        // every frame of the scroll that stays there.
        private _reached: Record<ScrollEdge, boolean> = { top: false, bottom: false, left: false, right: false };

        // Whether the pane has been measured at all yet. The first measurement only records which edges the
        // pane starts at: every pane starts at the top, and a page that was told about it would fetch what
        // comes before its first item before anything had been scrolled.
        private _measured = false;

        // The fade attributes currently on the element, so the DOM is only written to when one flips.
        private _faded: Record<ScrollEdge, boolean> = { top: false, bottom: false, left: false, right: false };

        constructor(element: HTMLElement, dotnetObj: DotNetObject, options: ScrollablePaneOptions) {
            this._element = element;
            this._dotnetObj = dotnetObj;
            this._options = options;
        }

        public init() {
            const ac = new AbortController();
            this._abortController = ac;

            // Passive: nothing here ever prevents the scroll, and saying so keeps the browser from
            // waiting on this listener before it paints the next frame.
            this._element.addEventListener('scroll', () => this.scrolled(), { passive: true, signal: ac.signal });

            // Where the browser has it, the end of a scroll is the browser's own answer rather than a
            // guess at one: the finger lifted, the thumb released, the momentum spent, the animation over.
            if (BitScrollablePane._hasScrollEnd) {
                this._element.addEventListener('scrollend', () => this.ended(), { passive: true, signal: ac.signal });
            }

            this.observe();
            this.bind();

            // The first measurement is taken once the browser has laid the content out, so a pane that
            // starts out with nothing to scroll does not paint a fade over an edge it will not have.
            this.schedule();
        }

        public update(options: ScrollablePaneOptions) {
            if (this._disposed) return;

            const previous = this._options;

            this._options = options;

            // A pane that stopped fading has to have the attributes taken back off, since nothing else
            // will: the stylesheet only stops reading them, and the element keeps whatever it was left with.
            if (previous.fade && options.fade === false) {
                this.clearFade();
            }

            // A pane that has just been asked to report its position has never reported this one, whatever
            // was last sent before the reporting was turned off.
            if (previous.scroll === false && options.scroll) {
                this._lastSignature = '';
            }

            this.bind();

            this.refresh();
        }

        public refresh() {
            if (this._disposed) return;

            this.schedule();
        }

        public autoScroll(force: boolean) {
            if (this._disposed) return;

            const m = ScrollablePane.measure(this._element);

            const pinX = force || (this._stickX && m.maxX > 0);
            const pinY = force || (this._stickY && m.maxY > 0);

            if (pinX === false && pinY === false) return;

            const smooth = this._options.smooth && force === false;

            // The move is only made where there is somewhere to move to, so a pane that only scrolls up
            // and down is not dragged sideways by a horizontal end it never had. The end of the content
            // is the visual LEFT edge of a right to left pane, which is where it is pinned to there.
            const endX = m.rtl ? 0 : m.maxX;

            ScrollablePane.apply(this._element, pinX ? endX : m.x, pinY ? m.maxY : m.y, m, smooth);

            if (pinX) this._stickX = true;
            if (pinY) this._stickY = true;

            this.pin(smooth);
        }

        public dispose() {
            // The attributes are the one thing this side leaves on the element, so they are taken back off
            // before anything else: a pane that stops fading keeps its element, and one that is set up
            // again would otherwise draw the fade of wherever it last stood until it is measured afresh.
            this.clearFade();
            this._element.removeAttribute('data-bit-scp-drag');

            this._disposed = true;

            this._abortController?.abort();
            this._abortController = undefined;

            this._dragAbortController?.abort();
            this._dragAbortController = undefined;

            this._wheelAbortController?.abort();
            this._wheelAbortController = undefined;

            this._resizeObserver?.disconnect();
            this._resizeObserver = undefined;

            this._mutationObserver?.disconnect();
            this._mutationObserver = undefined;

            if (this._frame) cancelAnimationFrame(this._frame);
            if (this._reportTimer) clearTimeout(this._reportTimer);
            if (this._pinTimer) clearTimeout(this._pinTimer);
            if (this._endTimer) clearTimeout(this._endTimer);

            this._frame = 0;
            this._reportTimer = 0;
            this._pinTimer = 0;
            this._endTimer = 0;
            this._drag = undefined;
        }



        // The content of the pane is watched only for what actually needs watching: the fade and the
        // edge callbacks are the two things a change of size can invalidate without anyone scrolling.
        // A pane that asked for neither costs one scroll listener and nothing else.
        private observe() {
            // The pane itself is always watched: one that is laid out later - inside a collapse, a tab, a
            // dialog that has not been opened yet - measures as nothing until it is on the screen, and no
            // scroll event is ever going to say otherwise.
            try {
                this._resizeObserver = new ResizeObserver(() => this.schedule());
                this._resizeObserver.observe(this._element);
            } catch { /* no ResizeObserver: the scroll listener still covers every move of the pane */ }

            // The content is watched only where a change of its size can invalidate something: the fade and
            // the edge callbacks. A pane that only reports where it stands pays for none of it.
            if (this.watchesContent() === false) return;

            try {
                this._mutationObserver = new MutationObserver(() => this.schedule());
                this._mutationObserver.observe(this._element, { childList: true, subtree: true, characterData: true });
            } catch { /* no MutationObserver: refresh() from .NET still covers every render */ }
        }

        private watchesContent(): boolean {
            const o = this._options;
            return o.fade || o.top || o.bottom || o.left || o.right;
        }

        // Binds and unbinds the two interactions that are only worth a listener while they are asked for:
        // dragging the content, and turning a vertical wheel into the sideways scroll of a strip. The
        // wheel one has to be able to prevent the default, so it cannot be a passive listener - which is
        // exactly why a pane that never asked for it must not carry one.
        private bind() {
            if (this._options.drag && this._dragAbortController === undefined) {
                const ac = new AbortController();
                this._dragAbortController = ac;

                this._element.addEventListener('pointerdown', e => this.dragStart(e), { signal: ac.signal });
                this._element.addEventListener('pointermove', e => this.dragMove(e), { signal: ac.signal });
                this._element.addEventListener('pointerup', e => this.dragEnd(e), { signal: ac.signal });
                this._element.addEventListener('pointercancel', e => this.dragEnd(e), { signal: ac.signal });
            } else if (this._options.drag === false && this._dragAbortController) {
                this._dragAbortController.abort();
                this._dragAbortController = undefined;
                this._drag = undefined;
                this._element.removeAttribute('data-bit-scp-drag');
            }

            if (this._options.wheel && this._wheelAbortController === undefined) {
                const ac = new AbortController();
                this._wheelAbortController = ac;

                this._element.addEventListener('wheel', e => this.wheel(e), { passive: false, signal: ac.signal });
            } else if (this._options.wheel === false && this._wheelAbortController) {
                this._wheelAbortController.abort();
                this._wheelAbortController = undefined;
            }
        }

        // Every path into the measuring is coalesced onto the next animation frame, so a burst of scroll
        // events, a resize and a mutation landing together are one measurement rather than three.
        private schedule() {
            if (this._disposed || this._frame) return;

            this._frame = requestAnimationFrame(() => {
                this._frame = 0;
                if (this._disposed) return;

                this.measured();
            });
        }

        private measured() {
            const m = ScrollablePane.measure(this._element);

            this.track(m);
            this.fade(m);
            this.edges(m);
            this.report(m);

            this._measured = true;
        }

        // Every scroll event of the element, whoever caused it. The start of a scroll is reported once
        // rather than on every event of it, and where the browser has no scrollend of its own the end of
        // one is a short idle after the last event.
        private scrolled() {
            this.began();

            this.schedule();

            if (BitScrollablePane._hasScrollEnd) return;

            if (this._endTimer) clearTimeout(this._endTimer);

            this._endTimer = setTimeout(() => { this._endTimer = 0; this.ended(); }, BitScrollablePane._endDelay);
        }

        private began() {
            if (this._scrolling) return;

            this._scrolling = true;

            if (this._options.scrollStart === false) return;

            this.send('OnScrollStart');
        }

        private ended() {
            if (this._endTimer) {
                clearTimeout(this._endTimer);
                this._endTimer = 0;
            }

            // A pinning move of our own is over the moment the scrolling is, which is a better answer than
            // the timer that has to stand in for it where a move raises no scroll event at all.
            if (this._pinTimer) {
                clearTimeout(this._pinTimer);
                this._pinTimer = 0;
            }
            this._pinning = false;

            if (this._scrolling === false) return;

            this._scrolling = false;

            if (this._options.scrollEnd === false) return;

            this.send('OnScrollEnd');
        }

        // Whether the pane is still standing at the end of each axis, which is what AutoScroll reads the
        // next time content arrives. It is left alone while a pinning scroll of our own is in flight: a
        // smooth one raises a scroll event on every frame of the way, and none of them are the reader.
        private track(m: ScrollMetrics) {
            if (this._pinning) return;

            const threshold = Math.max(0, this._options.autoScrollThreshold) + 1;

            // How far the pane still is from the END of its content, which on the horizontal axis of a
            // right to left pane is the distance back to the visual left edge.
            this._stickX = (m.rtl ? m.x : (m.maxX - m.x)) <= threshold;
            this._stickY = (m.maxY - m.y) <= threshold;
        }

        private pin(smooth: boolean) {
            this._pinning = true;

            if (this._pinTimer) clearTimeout(this._pinTimer);

            // The scrollend event ends this the moment the move actually is over; the timer is what covers
            // a move that raised no scroll event at all - the pane was already where it was pinned to.
            this._pinTimer = setTimeout(() => { this._pinning = false; this._pinTimer = 0; }, smooth ? 700 : 80);
        }

        // The fade is drawn from data attributes rather than from a class or an inline custom property,
        // because those two are written by Blazor on every render of the component and anything this
        // side added to them would be wiped. Nothing removes an attribute the renderer never knew about.
        private fade(m: ScrollMetrics) {
            if (this._options.fade === false) return;

            this.setFade('top', m.y > 0);
            this.setFade('bottom', m.y < m.maxY);
            this.setFade('left', m.x > 0);
            this.setFade('right', m.x < m.maxX);
        }

        private setFade(edge: ScrollEdge, on: boolean) {
            if (this._faded[edge] === on) return;

            this._faded[edge] = on;

            const attribute = BitScrollablePane._fadeAttributes[edge];

            if (on) {
                this._element.setAttribute(attribute, '');
            } else {
                this._element.removeAttribute(attribute);
            }
        }

        private clearFade() {
            (Object.keys(this._faded) as ScrollEdge[]).forEach(edge => this.setFade(edge, false));
        }

        // Each edge is reported as it is reached and re-armed once the pane has left it, so a reader who
        // comes to rest at the bottom is one call rather than one per frame of getting there. An axis
        // with nothing to scroll reports neither of its edges: it stands at both at once, and a pane that
        // announced that on setup would load a next page for a list that has not been scrolled at all.
        private edges(m: ScrollMetrics) {
            const offset = Math.max(0, this._options.offset);

            this.setEdge('top', this._options.top && m.maxY > 0 && m.y <= offset);
            this.setEdge('bottom', this._options.bottom && m.maxY > 0 && (m.maxY - m.y) <= offset);
            this.setEdge('left', this._options.left && m.maxX > 0 && m.x <= offset);
            this.setEdge('right', this._options.right && m.maxX > 0 && (m.maxX - m.x) <= offset);
        }

        private setEdge(edge: ScrollEdge, reached: boolean) {
            if (this._reached[edge] === reached) return;

            this._reached[edge] = reached;

            if (reached === false || this._measured === false) return;

            this.invoke('OnReached', edge);
        }

        // The scroll position is reported no more often than the interval asked for, with the last one
        // always following: a reader who stops mid-scroll leaves the page holding where they stopped
        // rather than wherever the last interval happened to fall.
        private report(m: ScrollMetrics) {
            if (this._options.scroll === false) return;

            const throttle = Math.max(0, this._options.throttle);

            if (throttle === 0) {
                this.sendPosition(m.rtl);
                return;
            }

            const now = Date.now();
            const elapsed = now - this._lastReport;

            if (elapsed >= throttle) {
                this.sendPosition(m.rtl);
                return;
            }

            if (this._reportTimer) return;

            this._reportTimer = setTimeout(() => {
                this._reportTimer = 0;
                if (this._disposed) return;

                // The pane has not been measured since, so the direction is asked for afresh rather than
                // taken from a measurement that may no longer be the one this report carries.
                this.sendPosition();
            }, throttle - elapsed);
        }

        // A report is only made for a position the page has not already been told about. Beyond saving a
        // round trip, this is what keeps a page that re-renders on OnScroll from re-rendering forever: a
        // render is what a refresh follows, a refresh is what a measurement follows, and a measurement
        // that reported the position it last reported would start the whole round again.
        private sendPosition(rtl?: boolean) {
            // A report the throttle has been holding on to is dropped where the reporting was turned off
            // while it waited, rather than delivered to a page that has stopped listening for it.
            if (this._options.scroll === false) return;

            const offset = ScrollablePane.getOffset(this._element, rtl);
            if (!offset) return;

            const signature = `${offset.left}|${offset.top}|${offset.scrollWidth}|${offset.scrollHeight}|${offset.clientWidth}|${offset.clientHeight}|${offset.rtl}`;
            if (signature === this._lastSignature) return;

            this._lastSignature = signature;
            this._lastReport = Date.now();

            this.invoke('OnScroll', offset);
        }

        // Where the pane stands, handed to one of the callbacks that carries a position of its own.
        private send(method: string) {
            const offset = ScrollablePane.getOffset(this._element);
            if (!offset) return;

            this.invoke(method, offset);
        }



        // Dragging the content of the pane with a pointer, for the mouse and the pen that have no other
        // way of scrolling a strip sideways. A touch already drags the pane itself, so it is left alone.
        private dragStart(e: PointerEvent) {
            if (this._disposed || this._options.drag === false) return;
            if (e.pointerType === 'touch' || e.button !== 0) return;

            // A drag that starts on something the pointer is meant to be doing something else with - a
            // field being selected in, a control being pressed - is not a drag of the pane.
            const target = e.target as Element | null;
            if (target?.closest?.(BitScrollablePane._noDragSelector)) return;

            const m = ScrollablePane.measure(this._element);
            if (m.maxX <= 0 && m.maxY <= 0) return;

            this._drag = {
                id: e.pointerId,
                x: e.clientX,
                y: e.clientY,
                left: this._element.scrollLeft,
                top: this._element.scrollTop,
                moved: false,
            };
        }

        private dragMove(e: PointerEvent) {
            const drag = this._drag;
            if (!drag || drag.id !== e.pointerId) return;

            const dx = e.clientX - drag.x;
            const dy = e.clientY - drag.y;

            // Nothing happens until the pointer has actually travelled, so a click on something inside the
            // pane is still a click and a press that never moved never took the pane anywhere.
            if (drag.moved === false) {
                if (Math.abs(dx) < BitScrollablePane._dragThreshold && Math.abs(dy) < BitScrollablePane._dragThreshold) return;

                drag.moved = true;

                this._element.setAttribute('data-bit-scp-drag', '');

                try { this._element.setPointerCapture(drag.id); } catch { /* the pointer is already gone */ }
            }

            // The default of a pointer move over text is a selection, which is not what a drag of the pane
            // is for. The raw offsets are used rather than the direction independent ones, since a delta
            // needs no direction: the browser clamps whatever it is given to what the pane can reach.
            e.preventDefault();

            this._element.scrollTo({ left: drag.left - dx, top: drag.top - dy, behavior: 'instant' });
        }

        private dragEnd(e: PointerEvent) {
            const drag = this._drag;
            if (!drag || drag.id !== e.pointerId) return;

            this._drag = undefined;

            if (drag.moved === false) return;

            this._element.removeAttribute('data-bit-scp-drag');

            try { this._element.releasePointerCapture(drag.id); } catch { /* the pointer is already gone */ }

            // The click a real drag ends with belongs to the drag, not to whatever card or link happened
            // to be under the pointer when it stopped. It is swallowed on the way down, and the listener
            // is taken off again straight after in case no click follows at all.
            const swallow = (event: Event) => {
                event.preventDefault();
                event.stopPropagation();
            };

            this._element.addEventListener('click', swallow, { capture: true, once: true });

            setTimeout(() => this._element.removeEventListener('click', swallow, true));
        }

        // A wheel mouse has no horizontal axis, so a pane that only scrolls sideways would be unreachable
        // with one. This is deliberately the narrowest reading of that: only a wheel that carries nothing
        // horizontal of its own, only over a pane that has somewhere to go sideways and nowhere to go up
        // or down, and only until that pane reaches the end it is being pushed towards - after which the
        // page gets the scroll back rather than being left stuck under the pointer.
        private wheel(e: WheelEvent) {
            if (this._disposed || this._options.wheel === false) return;
            if (e.ctrlKey || e.deltaX !== 0 || e.deltaY === 0) return;

            const m = ScrollablePane.measure(this._element);
            if (m.maxX <= 0 || m.maxY > 0) return;

            // A delta is in pixels, in lines, or in pages, and only the first of the three can be used as
            // it stands.
            const delta = e.deltaY * (e.deltaMode === 1 ? BitScrollablePane._lineHeight
                : e.deltaMode === 2 ? this._element.clientWidth : 1);

            // A wheel forwards means further into the content, which is towards the visual left edge of
            // a right to left pane and towards the right one of every other.
            const step = m.rtl ? -delta : delta;

            if (step < 0 && m.x <= 0) return;
            if (step > 0 && m.x >= m.maxX) return;

            e.preventDefault();

            ScrollablePane.apply(this._element, m.x + step, m.y, m, false);
        }

        private invoke(method: string, arg: any) {
            try {
                this._dotnetObj.invokeMethodAsync(method, arg);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane:", e); }
        }

        private static _fadeAttributes: Record<ScrollEdge, string> = {
            top: 'data-bit-scp-ft',
            bottom: 'data-bit-scp-fb',
            left: 'data-bit-scp-fl',
            right: 'data-bit-scp-fr',
        };

        // Anything the pointer is meant to be doing something else with, plus an opt out for whatever the
        // consumer knows a drag must not start on.
        private static _noDragSelector = 'input,textarea,select,button,a,audio,video,[contenteditable=""],[contenteditable="true"],[draggable="true"],[data-bit-scp-nodrag]';

        private static _dragThreshold = 4;

        // How long after the last scroll event a scroll counts as over, where the browser has no scrollend
        // event of its own. Long enough to bridge the gaps in a slow drag, short enough not to be noticed.
        private static _endDelay = 140;

        // A line of a wheel that counts in lines rather than in pixels, which is what Firefox reports.
        private static _lineHeight = 16;

        private static _hasScrollEnd = typeof window !== 'undefined' && 'onscrollend' in window;
    }
}
