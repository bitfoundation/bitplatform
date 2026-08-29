namespace BitBlazorUI {
    // The options the browser side of a pane is driven with. They are handed over as a whole (both at
    // setup and on every change), so the instance never has to keep them in sync member by member.
    interface ScrollablePaneOptions {
        fade: boolean;
        offset: number;   // how near an edge counts as reaching it, in pixels
        throttle: number; // the shortest interval between two scroll reports, or 0 for one per frame
        scroll: boolean;
        top: boolean;
        bottom: boolean;
        left: boolean;
        right: boolean;
        autoScroll: boolean;
        autoScrollThreshold: number;
        smooth: boolean;
    }

    interface ScrollOffset {
        left: number;
        top: number;
        scrollWidth: number;
        scrollHeight: number;
        clientWidth: number;
        clientHeight: number;
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

        public static scrollToEnd(element: HTMLElement, smooth?: boolean) {
            if (!element) return;

            try {
                const m = ScrollablePane.measure(element);
                ScrollablePane.apply(element, m.maxX, m.maxY, m, smooth);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.scrollToEnd:", e); }
        }

        public static scrollToStart(element: HTMLElement, smooth?: boolean) {
            if (!element) return;

            try {
                const m = ScrollablePane.measure(element);
                ScrollablePane.apply(element, 0, 0, m, smooth);
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
        public static scrollToElement(element: HTMLElement, targetId: string, offset: number, smooth?: boolean) {
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

                const x = m.x + (rect.left - paneRect.left) - element.clientLeft - padding.x;
                const y = m.y + (rect.top - paneRect.top) - element.clientTop - padding.y;

                ScrollablePane.apply(element, x, y, m, smooth);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.scrollToElement:", e); }
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

        public static getOffset(element: HTMLElement): ScrollOffset | null {
            if (!element) return null;

            try {
                return {
                    left: element.scrollLeft,
                    top: element.scrollTop,
                    scrollWidth: element.scrollWidth,
                    scrollHeight: element.scrollHeight,
                    clientWidth: element.clientWidth,
                    clientHeight: element.clientHeight,
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

        private _frame = 0;
        private _reportTimer = 0;
        private _lastReport = 0;
        private _disposed = false;

        // The position that was last reported to .NET, so the same one is never reported twice.
        private _lastSignature = '';

        // Whether the pane was left standing at the end of each axis, which is the whole of what decides
        // if the next arrival of content pins it there. Both start out true, so a pane that is set up
        // with content already in it is pinned by its first AutoScroll rather than left at the top.
        private _stickX = true;
        private _stickY = true;

        // Set while a pinning scroll of our own is on its way to the end, so the scroll events it raises
        // along the way are not mistaken for a reader scrolling away from it.
        private _pinning = false;
        private _pinTimer = 0;

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
            this._element.addEventListener('scroll', () => this.schedule(), { passive: true, signal: ac.signal });

            this.observe();

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
            // and down is not dragged sideways by a horizontal end it never had.
            ScrollablePane.apply(this._element, pinX ? m.maxX : m.x, pinY ? m.maxY : m.y, m, smooth);

            if (pinX) this._stickX = true;
            if (pinY) this._stickY = true;

            this.pin(smooth);
        }

        public dispose() {
            // The fade attributes are the one thing this side leaves on the element, so they are taken back
            // off before anything else: a pane that stops fading keeps its element, and one that is set up
            // again would otherwise draw the fade of wherever it last stood until it is measured afresh.
            this.clearFade();

            this._disposed = true;

            this._abortController?.abort();
            this._abortController = undefined;

            this._resizeObserver?.disconnect();
            this._resizeObserver = undefined;

            this._mutationObserver?.disconnect();
            this._mutationObserver = undefined;

            if (this._frame) cancelAnimationFrame(this._frame);
            if (this._reportTimer) clearTimeout(this._reportTimer);
            if (this._pinTimer) clearTimeout(this._pinTimer);

            this._frame = 0;
            this._reportTimer = 0;
            this._pinTimer = 0;
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
            this.report();

            this._measured = true;
        }

        // Whether the pane is still standing at the end of each axis, which is what AutoScroll reads the
        // next time content arrives. It is left alone while a pinning scroll of our own is in flight: a
        // smooth one raises a scroll event on every frame of the way, and none of them are the reader.
        private track(m: ScrollMetrics) {
            if (this._pinning) return;

            const threshold = Math.max(0, this._options.autoScrollThreshold) + 1;

            this._stickX = (m.maxX - m.x) <= threshold;
            this._stickY = (m.maxY - m.y) <= threshold;
        }

        private pin(smooth: boolean) {
            this._pinning = true;

            if (this._pinTimer) clearTimeout(this._pinTimer);

            // Long enough for the move to have played out, short enough that a reader scrolling away
            // straight afterwards is still noticed. An unanimated move is over within a frame or two.
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
        private report() {
            if (this._options.scroll === false) return;

            const throttle = Math.max(0, this._options.throttle);

            if (throttle === 0) {
                this.send();
                return;
            }

            const now = Date.now();
            const elapsed = now - this._lastReport;

            if (elapsed >= throttle) {
                this.send();
                return;
            }

            if (this._reportTimer) return;

            this._reportTimer = setTimeout(() => {
                this._reportTimer = 0;
                if (this._disposed) return;

                this.send();
            }, throttle - elapsed);
        }

        // A report is only made for a position the page has not already been told about. Beyond saving a
        // round trip, this is what keeps a page that re-renders on OnScroll from re-rendering forever: a
        // render is what a refresh follows, a refresh is what a measurement follows, and a measurement
        // that reported the position it last reported would start the whole round again.
        private send() {
            const offset = ScrollablePane.getOffset(this._element);
            if (!offset) return;

            const signature = `${offset.left}|${offset.top}|${offset.scrollWidth}|${offset.scrollHeight}|${offset.clientWidth}|${offset.clientHeight}`;
            if (signature === this._lastSignature) return;

            this._lastSignature = signature;
            this._lastReport = Date.now();

            this.invoke('OnScroll', offset);
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
    }
}
