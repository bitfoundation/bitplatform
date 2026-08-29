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
        momentum: boolean; // whether a released drag carries on and slows to a stop
        wheel: boolean;
        preserve: boolean; // whether the reader's place is kept when content lands above what they see
        autoHide: boolean; // whether the Modern scrollbar is only painted while the pane is being used
        noScroll: boolean; // whether the pane is not to be moved by the reader at all
    }

    interface ScrollOffset {
        left: number;
        top: number;
        scrollWidth: number;
        scrollHeight: number;
        clientWidth: number;
        clientHeight: number;
        rtl: boolean;
        // How far the pane moved since the position that was last reported, on the screen rather than in
        // reading order. Only a report carries them; a position read on demand has nothing to move from.
        deltaLeft: number;
        deltaTop: number;
    }

    // The scroll-padding of a pane, in pixels, named after the four edges of the screen rather than after
    // the start and the end of the content - so a right to left pane reads the same as any other here.
    interface ScrollPadding {
        left: number;
        right: number;
        top: number;
        bottom: number;
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
        // The size of the CONTENT, carried along so that the one thing a change of it decides - whether an
        // auto scrolling pane has to be pinned again - costs no second layout read of its own.
        width: number;
        height: number;
        // The rest of the reading the numbers above were derived from, so that the report a measured frame
        // makes costs no second read of the same properties. `left` is the RAW scrollLeft rather than the
        // direction independent `x`, since what is reported is the position the browser actually gave -
        // elastic overscroll and all - rather than the clamped one the rest of this file works in.
        left: number;
        clientWidth: number;
        clientHeight: number;
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
        // Where the pointer was when it was last seen and when that was, and how fast it has been going
        // since - in pixels per millisecond, smoothed, so that the flick a drag ends with is the speed of
        // the last stretch of it rather than of whatever the very last two events happened to be.
        atX: number;
        atY: number;
        at: number;
        vx: number;
        vy: number;
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
                const padding = offset
                    ? { left: offset, right: offset, top: offset, bottom: offset }
                    : ScrollablePane.scrollPadding(element);

                // Where the element stands inside the content of the pane, which is where it is on the
                // screen taken back to the top left corner of the scrolled content.
                const x = m.x + (rect.left - paneRect.left) - element.clientLeft;
                const y = m.y + (rect.top - paneRect.top) - element.clientTop;

                ScrollablePane.apply(element,
                    ScrollablePane.align(x, rect.width, m.x, element.clientWidth, padding.left, padding.right, alignment, m.rtl),
                    ScrollablePane.align(y, rect.height, m.y, element.clientHeight, padding.top, padding.bottom, alignment),
                    m, smooth);
            } catch (e) { console.error("BitBlazorUI.ScrollablePane.scrollToElement:", e); }
        }

        // Where the pane has to stand along one axis for an element to be left where the alignment asks
        // for. `at` is where the element starts in the content, `size` how long it is along this axis,
        // `from` where the pane stands now and `view` how much of the axis it shows. The two paddings are
        // the ones of the LEADING and the TRAILING edge on the screen - the left and the right of the
        // horizontal axis, the top and the bottom of the vertical one - so each edge keeps the room that
        // was reserved against it rather than the room reserved against the other one.
        private static align(at: number,
                             size: number,
                             from: number,
                             view: number,
                             padLead: number,
                             padTrail: number,
                             alignment?: ScrollAlignment,
                             rtl?: boolean): number {
            const toLead = at - padLead;                   // the element brought to the leading edge
            const toTrail = at + size - view + padTrail;   // the element brought to the trailing edge

            switch (alignment) {
                case 'center':
                    return at - (view - size) / 2;

                // The start and the end of an axis are the start and the end of the CONTENT, so the two
                // of them are the other way round on the horizontal axis of a right to left pane.
                case 'end':
                    return rtl ? toLead : toTrail;

                case 'nearest':
                    // An element that is already in view is not moved to at all, and one that is not is
                    // brought only as far as the edge it fell off, which is the least the pane can move.
                    // Both edges are the ones on the screen, so this reads the same either way round.
                    if (toLead < from) return toLead;
                    if (at + size + padTrail > from + view) return toTrail;
                    return from;

                default:
                    return rtl ? toTrail : toLead;
            }
        }

        // The scroll-padding of the pane in pixels, on all four edges. The property is `auto` unless it
        // was set, and a percentage resolves against the box, so both are asked of the computed style
        // rather than parsed out of the declaration.
        private static scrollPadding(element: HTMLElement): ScrollPadding {
            const styles = getComputedStyle(element);
            const read = (value: string, against: number) => {
                if (!value || value === 'auto') return 0;
                if (value.endsWith('%')) return (parseFloat(value) || 0) * against / 100;
                return parseFloat(value) || 0;
            };

            return {
                left: read(styles.scrollPaddingLeft, element.clientWidth),
                right: read(styles.scrollPaddingRight, element.clientWidth),
                top: read(styles.scrollPaddingTop, element.clientHeight),
                bottom: read(styles.scrollPaddingBottom, element.clientHeight),
            };
        }

        public static getOffset(element: HTMLElement): ScrollOffset | null {
            if (!element) return null;

            try {
                return ScrollablePane.toOffset(ScrollablePane.measure(element));
            } catch (e) {
                console.error("BitBlazorUI.ScrollablePane.getOffset:", e);
                return null;
            }
        }

        // The position of the pane built from a measurement already in hand, so that the report a measured
        // frame makes costs no second read of the six properties that frame was decided by - and lands on
        // the same numbers rather than on whatever the layout had become a moment later.
        public static toOffset(m: ScrollMetrics): ScrollOffset {
            return {
                left: m.left,
                top: m.y,
                scrollWidth: m.width,
                scrollHeight: m.height,
                clientWidth: m.clientWidth,
                clientHeight: m.clientHeight,
                // Which way the pane reads is what tells a scrollLeft of 0 at the visual left edge of a
                // left to right pane from the same 0 at the visual RIGHT edge of a right to left one.
                rtl: m.rtl,
                // A position read on its own has nothing to have moved from; the reporting path fills
                // these in against the position it last sent.
                deltaLeft: 0,
                deltaTop: 0,
            };
        }



        // Reads the pane in the direction independent terms the rest of this file works in. A right to
        // left pane counts scrollLeft down from 0 at its right edge into the negatives, so the visual
        // left edge sits at -maxX there and the sign is folded away here rather than at every use.
        public static measure(element: HTMLElement): ScrollMetrics {
            const width = element.scrollWidth;
            const height = element.scrollHeight;
            const clientWidth = element.clientWidth;
            const clientHeight = element.clientHeight;
            const left = element.scrollLeft;
            const top = element.scrollTop;
            const maxX = Math.max(0, width - clientWidth);
            const maxY = Math.max(0, height - clientHeight);
            // Which way the pane reads only matters where there is something to scroll sideways, and asking
            // for it is a style read on every frame of every scroll of every pane that has not.
            const rtl = maxX > 0 && getComputedStyle(element).direction === 'rtl';
            const x = rtl ? maxX + left : left;

            return {
                x: Math.min(Math.max(x, 0), maxX),
                y: top,
                maxX,
                maxY,
                rtl,
                width,
                height,
                left,
                clientWidth,
                clientHeight,
            };
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
                // The class opts a whole SUBTREE out of the preference, so an ancestor carrying it counts
                // for the pane inside it - which is how every other animated component in the library reads
                // it, and what the ForceAnimation of a container around the pane is asking for.
                if (element.closest('.bit-fam')) return true;

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
        private _autoHideAbortController?: AbortController;

        // The listeners of ONE drag, which live only for as long as that drag does and sit on the document
        // rather than on the pane: a pointer that is released outside the pane - or before it has travelled
        // far enough for the capture to be taken - still has to end the drag it started.
        private _draggingAbortController?: AbortController;

        private _frame = 0;
        private _reportTimer = 0;
        private _lastReport = 0;
        private _disposed = false;

        // The position that was last reported to .NET, so the same one is never reported twice, and the
        // one it stood at then, so a report can say which way the pane has moved since.
        private _lastSignature = '';
        private _lastLeft = 0;
        private _lastTop = 0;

        // The size of the content at the last measurement, which is what an auto scrolling pane re-pins
        // itself off: it has grown, so there is somewhere new to be pinned to.
        private _contentWidth = 0;
        private _contentHeight = 0;

        // The height of the content the last time the reader's place was kept, which is a count of its
        // own: two changes can land between two frames, and each one has to be answered by what IT added
        // rather than by the total since the pane was last measured.
        private _anchorHeight = 0;

        // Whether the browser is keeping the reader's place on THIS pane on its own: the engine has scroll
        // anchoring at all AND the pane has not opted out of it in CSS. A pane carrying
        // `overflow-anchor: none` is one nothing anchors, whatever the engine is capable of, and it is the
        // one place a browser WITH anchoring still needs the compensation made for it.
        private _anchored = false;

        // How tall a line of a wheel that counts in lines rather than in pixels is on this pane, read off
        // the computed style the first time such a wheel arrives and kept, since it is a style read.
        private _lineHeight = 0;

        // Whether the READER can scroll the pane up and down at all, which is not the same as its content
        // being taller than its box: a horizontal pane clips what overflows with `overflow-y: hidden`, and
        // clipped overflow still counts towards scrollHeight. It is a style read, so it is taken at setup
        // and on every change of the options rather than on every wheel event.
        private _scrollsY = true;

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
        // along the way are not mistaken for a reader scrolling away from it, along with where that move
        // set off from - which is what tells a pane still on its way there from one the reader has since
        // pulled back past.
        private _pinning = false;
        private _pinTimer = 0;
        private _pinFromX = 0;
        private _pinFromY = 0;

        // The drag that is currently under way, if the pane is being dragged at all, and the frame of the
        // glide a released one left behind.
        private _drag?: ScrollDrag;
        private _glideFrame = 0;

        // Which edges the pane is standing at, so each one is reported as it is reached rather than on
        // every frame of the scroll that stays there. An edge is `undefined` until there is an answer to
        // give for it at all - nobody asked about it, or its axis has nothing to scroll - and the first
        // answer it does give is the one that may have to be kept quiet about; see setEdge.
        private _reached: Record<ScrollEdge, boolean | undefined> = { top: undefined, bottom: undefined, left: undefined, right: undefined };

        // Whether the pane has been measured at all yet, which is what tells a content size that GREW from
        // the first one ever read.
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

            // Where the pane already stands, so that the first report of a pane that was set up somewhere
            // other than the top does not read as a move of that whole distance.
            this._lastLeft = this._element.scrollLeft;
            this._lastTop = this._element.scrollTop;

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

            this.readAnchoring();
            this.readScrollsY();

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
                this._lastLeft = this._element.scrollLeft;
                this._lastTop = this._element.scrollTop;
            }

            // The content is watched for one set of reasons and stops being watched for another, so the
            // observer follows the options rather than being decided once at setup.
            if (this.watchesContent() !== (this._mutationObserver !== undefined)) {
                this.observeContent();
            }

            this.bind();

            this.readAnchoring();
            this.readScrollsY();

            this.refresh();
        }

        public refresh() {
            if (this._disposed) return;

            this.readScrollsY();

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

            this._pinFromX = m.x;
            this._pinFromY = m.y;

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
            this.cancelDrag();
            this.idle(false);

            this._disposed = true;

            this._abortController?.abort();
            this._abortController = undefined;

            this._dragAbortController?.abort();
            this._dragAbortController = undefined;

            this._wheelAbortController?.abort();
            this._wheelAbortController = undefined;

            this._autoHideAbortController?.abort();
            this._autoHideAbortController = undefined;

            this._draggingAbortController?.abort();
            this._draggingAbortController = undefined;

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



        // The pane and its content are watched only for what actually needs watching: a change of size is
        // what invalidates the fade, the edge callbacks, where an auto scrolling pane belongs and where
        // the reader was, none of which any scroll event is going to say. A pane that asked for none of
        // them costs one scroll listener and nothing else.
        private observe() {
            // The pane itself is always watched: one that is laid out later - inside a collapse, a tab, a
            // dialog that has not been opened yet - measures as nothing until it is on the screen, and no
            // scroll event is ever going to say otherwise.
            try {
                // A box that changed is a box whose overflow may have been what changed it - the axes a
                // pane scrolls on are written into its style attribute, and turning one off takes its
                // scrollbar out of the box - so what the sideways wheel reads is taken again here rather
                // than only when the options change, which a purely visual parameter does not.
                this._resizeObserver = new ResizeObserver(() => {
                    this.readScrollsY();
                    this.schedule();
                });
                this._resizeObserver.observe(this._element);
            } catch { /* no ResizeObserver: the scroll listener still covers every move of the pane */ }

            this.observeContent();
        }

        // The content is watched only where a change of its size can do something no scroll event will
        // say: draw a fade, reach an edge, re-pin an auto scrolling pane, or keep the reader's place when
        // what arrived landed above them. A pane that only reports where it stands pays for none of it.
        private observeContent() {
            this._mutationObserver?.disconnect();
            this._mutationObserver = undefined;

            if (this.watchesContent() === false) return;

            try {
                this._mutationObserver = new MutationObserver(records => {
                    this.preserve(records);
                    this.schedule();
                });
                // Attributes as well as nodes and text, because the change to the size of the content that
                // no added node and no rewritten text carries is exactly the one that resized what was
                // already there - a style, a class or a hidden flipped by a render. The filter keeps that
                // from meaning every attribute written anywhere in the subtree: those three are the ones a
                // size can change under.
                this._mutationObserver.observe(this._element, {
                    childList: true,
                    subtree: true,
                    characterData: true,
                    attributes: true,
                    attributeFilter: ['style', 'class', 'hidden'],
                });
            } catch { /* no MutationObserver: refresh() from .NET still covers every render */ }
        }

        private watchesContent(): boolean {
            const o = this._options;
            return o.fade || o.top || o.bottom || o.left || o.right || o.autoScroll || o.preserve;
        }

        // Binds and unbinds the two interactions that are only worth a listener while they are asked for:
        // dragging the content, and turning a vertical wheel into the sideways scroll of a strip. The
        // wheel one has to be able to prevent the default, so it cannot be a passive listener - which is
        // exactly why a pane that never asked for it must not carry one.
        private bind() {
            // A pane the reader is not to be able to move is not one either of these belongs on: both of
            // them scroll the element with the scrolling API, which `overflow: hidden` does not stop.
            const drag = this._options.drag && this._options.noScroll === false;
            const wheel = this._options.wheel && this._options.noScroll === false;

            if (drag && this._dragAbortController === undefined) {
                const ac = new AbortController();
                this._dragAbortController = ac;

                // Only the press is bound for the life of the pane. The move and the release belong to one
                // drag and are bound by dragStart for as long as it lasts, so a pane nobody is dragging
                // costs nothing per movement of a pointer across it.
                this._element.addEventListener('pointerdown', e => this.dragStart(e), { signal: ac.signal });
            } else if (drag === false && this._dragAbortController) {
                this._dragAbortController.abort();
                this._dragAbortController = undefined;
                this.cancelDrag();
            }

            if (wheel && this._wheelAbortController === undefined) {
                const ac = new AbortController();
                this._wheelAbortController = ac;

                this._element.addEventListener('wheel', e => this.wheel(e), { passive: false, signal: ac.signal });
            } else if (wheel === false && this._wheelAbortController) {
                this._wheelAbortController.abort();
                this._wheelAbortController = undefined;
            }

            if (this._options.autoHide && this._autoHideAbortController === undefined) {
                const ac = new AbortController();
                this._autoHideAbortController = ac;

                // enter and leave rather than over and out: the pane is one region here, and a pointer
                // crossing from the content onto something inside it is not a pointer that left the pane.
                this._element.addEventListener('pointerenter', () => this.idle(false), { passive: true, signal: ac.signal });
                this._element.addEventListener('pointerleave', () => this.idle(true), { passive: true, signal: ac.signal });

                // focusin and focusout rather than focus and blur, because what keeps the scrollbar on the
                // screen is the focus being anywhere INSIDE the pane - and those two do not bubble.
                this._element.addEventListener('focusin', () => this.idle(false), { passive: true, signal: ac.signal });
                this._element.addEventListener('focusout', () => this.idle(true), { passive: true, signal: ac.signal });

                // A pane that is already being pointed at or typed in when the flag is turned on is one the
                // scrollbar belongs on: the two events that would say so have already been and gone.
                this.idle(this.used() === false);
            } else if (this._options.autoHide === false && this._autoHideAbortController) {
                this._autoHideAbortController.abort();
                this._autoHideAbortController = undefined;

                this.idle(false);
            }
        }

        // Whether the pane is being used right now, for the one moment nothing was listening to say so.
        private used(): boolean {
            try {
                return this._element.matches(':hover') || this._element.contains(document.activeElement);
            } catch {
                return false; // :hover is unmatchable in a browser that never had a pointer
            }
        }

        // Whether the Modern scrollbar of an auto hiding pane is currently painted, written onto the
        // element for the stylesheet to read. It is driven from here rather than from :hover and
        // :focus-within in the stylesheet, because Chromium does not repaint a ::-webkit-scrollbar
        // pseudo-element when the state of the element it belongs to changes: the thumb would stay as it
        // was last painted until something else on the pane forced a repaint - which is a scrollbar that
        // never appears, and then one that never goes away again. Writing an attribute invalidates the
        // element itself, which is a repaint of its scrollbars with it.
        //
        // An attribute, and not a class or an inline custom property, for the reason the fade uses one:
        // Blazor rewrites both of those whole on every render and would wipe whatever this side added.
        private idle(idle: boolean) {
            if (idle) {
                this._element.setAttribute('data-bit-scp-idle', '');
            } else {
                this._element.removeAttribute('data-bit-scp-idle');
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

            // Whether the CONTENT grew, which is the one thing an auto scrolling pane has to answer
            // without waiting for a render: an image that finished loading, a line of a stream that
            // arrived, anything appended by something other than the renderer.
            const grew = this._measured && (m.width > this._contentWidth || m.height > this._contentHeight);

            // The re-pinning is answered BEFORE the stick flags are taken again, and this is the whole of
            // why: what decides whether a pane follows its content is where the reader had left it, and
            // this measurement is one the growth has already happened in. Reading the flags off it first
            // would find every pane whose content grew by more than the threshold to have been "scrolled
            // away from" - by the very arrival it is supposed to be following.
            //
            // Only a pane the reader left standing at the end is pinned, which is what autoScroll decides
            // for itself; the move it may make raises no measurement of its own unless it actually moves
            // the pane, so this cannot run away with itself.
            if (grew && this._options.autoScroll) {
                this.autoScroll(false);
            }

            this.track(m);
            this.fade(m);
            this.edges(m);
            this.report(m);

            this._contentWidth = m.width;
            this._contentHeight = m.height;
            this._anchorHeight = m.height;
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
            if (this._pinning) {
                // A pinning move of ours only ever goes TOWARDS the end, so a pane that has come back
                // past where one set off from is the reader having pulled it there - and the pinning
                // gives way to them at once rather than after the timer that covers the ordinary case.
                // Without this a log written to faster than a pin resolves would never let go.
                const back = m.y < this._pinFromY - 1
                    || (m.rtl ? m.x > this._pinFromX + 1 : m.x < this._pinFromX - 1);

                if (back === false) return;

                this._pinning = false;

                if (this._pinTimer) {
                    clearTimeout(this._pinTimer);
                    this._pinTimer = 0;
                }
            }

            const threshold = Math.max(0, this._options.autoScrollThreshold) + BitScrollablePane._edgeTolerance;

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

        // Whether this pane is anchored by the browser, read off the computed style rather than off the
        // engine alone: `overflow-anchor: none` on the pane turns the browser's own anchoring off, and a
        // pane that opted out of it is exactly one that needs the compensation made for it. It is a style
        // read, so it is taken once at setup and once per change of the options rather than per mutation,
        // and only for a pane that asked to keep the reader's place at all.
        private readAnchoring() {
            if (BitScrollablePane._supportsScrollAnchoring === false || this._options.preserve === false) {
                this._anchored = false;
                return;
            }

            try {
                this._anchored = getComputedStyle(this._element).getPropertyValue('overflow-anchor').trim() !== 'none';
            } catch {
                this._anchored = true; // the style cannot be read, so the browser's own anchoring is the safer half to trust
            }
        }

        // Whether the reader has a vertical axis to scroll at all, which is what the sideways wheel asks
        // about and is NOT the same question as "is the content taller than the box": a horizontal pane
        // clips what overflows with `overflow-y: hidden`, and clipped overflow still counts towards
        // scrollHeight - so a strip whose own horizontal scrollbar makes it one pixel too short would
        // otherwise be read as a pane the reader can scroll up and down, and the wheel handed back to a
        // page for an axis that cannot move.
        private readScrollsY() {
            if (this._options.wheel === false) {
                this._scrollsY = true;
                return;
            }

            try {
                const overflow = getComputedStyle(this._element).overflowY;

                this._scrollsY = overflow !== 'hidden' && overflow !== 'clip' && overflow !== 'visible';
            } catch {
                this._scrollsY = true; // the style cannot be read, so the page keeps its wheel
            }
        }

        // Keeps the reader's place when content lands ABOVE what they are looking at: without it the
        // arrival of a page of older messages at the top of a list pushes everything the reader was
        // reading down the screen by the height of what arrived.
        //
        // Where the browser anchors the scroll itself this does nothing at all - two compensations for
        // one change would move the pane twice - so it is the browsers WITHOUT scroll anchoring (Safari,
        // most of all) that it brings up to the behavior of the rest.
        //
        // It runs in the mutation callback, which is a microtask after the change and before the frame
        // that would have painted it, so the pane is put back before anything is drawn out of place.
        private preserve(records: MutationRecord[]) {
            if (this._disposed || this._options.preserve === false) return;
            if (this._anchored) return;

            // Nothing has been measured yet, so there is no height for this one to have grown from - and
            // a pane that has not been laid out has no place worth keeping either.
            if (this._measured === false) return;

            try {
                const top = this._element.scrollTop;

                // A pane standing at the top of its content has no place to keep: what arrives above it
                // is what it is already looking at.
                if (top <= 0) return;

                const height = this._element.scrollHeight;
                const grew = height - this._anchorHeight;

                this._anchorHeight = height;

                if (grew <= 0) return;
                if (this.landedAbove(records) === false) return;

                // Put back rather than scrolled to: this is the pane standing still, so it must not be
                // animated by the scroll-behavior a Smooth pane carries - which assigning scrollTop, or
                // asking for the default behavior, would be.
                this._element.scrollTo({ top: top + grew, behavior: 'instant' });

                // And standing still is what the next report has to say as well. The position the last one
                // was made from moves with the content, or the compensation made here would be reported as
                // the reader having scrolled down by the height of what arrived above them.
                this._lastTop += grew;
            } catch { /* the element is gone; there is nothing left to keep in place */ }
        }

        // Whether any of what changed sits above what the reader is looking at. An element that starts
        // above the top edge of the pane is above the fold whether or not it reaches down into it, which
        // is exactly the content whose height the reader is pushed down the screen by.
        private landedAbove(records: MutationRecord[]): boolean {
            const paneTop = this._element.getBoundingClientRect().top;

            // A render can add a great many nodes at once and each one asked about is a rectangle read,
            // so only the first of them are looked at: content that arrived above the reader arrived as
            // a block of it, and the first node of that block is one of these.
            let budget = BitScrollablePane._anchorBudget;

            for (const record of records) {
                // A text that grew carries no added node of its own, so the element holding it is what
                // says where the change was.
                const nodes: Node[] = record.addedNodes.length ? Array.from(record.addedNodes) : [record.target];

                for (const node of nodes) {
                    const element = node.nodeType === Node.ELEMENT_NODE ? node as Element : node.parentElement;
                    if (!element || element === this._element) continue;

                    const rect = element.getBoundingClientRect();
                    if (rect.height > 0 && rect.top < paneTop) return true;

                    if (--budget <= 0) return false;
                }
            }

            return false;
        }

        // The fade is drawn from data attributes rather than from a class or an inline custom property,
        // because those two are written by Blazor on every render of the component and anything this
        // side added to them would be wiped. Nothing removes an attribute the renderer never knew about.
        private fade(m: ScrollMetrics) {
            if (this._options.fade === false) return;

            // Within a pixel of an edge is at it: a scroll offset is fractional at a fractional zoom level
            // and on a scaled display while the maxima are derived from whole numbers, so an exact
            // comparison would leave the fade of an edge the pane is visibly standing at still painted.
            const slack = BitScrollablePane._edgeTolerance;

            this.setFade('top', m.y > slack);
            this.setFade('bottom', m.y < m.maxY - slack);
            this.setFade('left', m.x > slack);
            this.setFade('right', m.x < m.maxX - slack);
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
            // The pixel of slack every comparison of a scroll offset against an edge in this component
            // gives. Without it the default offset of 0 is a test a fractional scroll position can never
            // pass, and the endless list whose next page this callback fetches never fetches one.
            const slack = BitScrollablePane._edgeTolerance;
            const offset = Math.max(0, this._options.offset) + slack;

            this.setEdge('top', this._options.top && m.maxY > 0, m.y, offset, slack);
            this.setEdge('bottom', this._options.bottom && m.maxY > 0, m.maxY - m.y, offset, slack);
            this.setEdge('left', this._options.left && m.maxX > 0, m.x, offset, slack);
            this.setEdge('right', this._options.right && m.maxX > 0, m.maxX - m.x, offset, slack);
        }

        // `live` is whether this edge has an answer to give at all: one nobody asked about, and either edge
        // of an axis with nothing to scroll, has none rather than a false one. `distance` is how far the
        // pane still is from it, `offset` how near counts as having reached it, and `slack` how near counts
        // as standing ON it.
        private setEdge(edge: ScrollEdge, live: boolean, distance: number, offset: number, slack: number) {
            if (live === false) {
                // The edge goes back to having no answer rather than to a false one, so that whenever it
                // has one again - the flag turned on, the first page finally laid out - that answer is
                // read as the pane's starting position rather than as an edge it has just arrived at.
                this._reached[edge] = undefined;
                return;
            }

            const reached = distance <= offset;
            const previous = this._reached[edge];

            if (previous === reached) return;

            this._reached[edge] = reached;

            if (reached === false) return;

            // The first answer an edge ever gives is kept quiet about where the pane is standing AT that
            // edge: every pane starts at the top of its content, and a page told about that would fetch
            // what comes before its first item before anything had been scrolled.
            //
            // Standing at it, though, and not merely within the offset of it. A list whose first page is
            // too short to fill the pane starts within a screenful of its bottom without being at it, and
            // that is the endless list asking for its next page rather than an artifact of where panes
            // start - so it is reported, and the list is not left waiting for a growth that only the
            // report it never got would have brought.
            if (previous === undefined && distance <= slack) return;

            this.invoke('OnReached', edge);
        }

        // The scroll position is reported no more often than the interval asked for, with the last one
        // always following: a reader who stops mid-scroll leaves the page holding where they stopped
        // rather than wherever the last interval happened to fall.
        private report(m: ScrollMetrics) {
            if (this._options.scroll === false) return;

            const throttle = Math.max(0, this._options.throttle);

            if (throttle === 0) {
                this.sendPosition(m);
                return;
            }

            const now = Date.now();
            const elapsed = now - this._lastReport;

            if (elapsed >= throttle) {
                this.sendPosition(m);
                return;
            }

            if (this._reportTimer) return;

            this._reportTimer = setTimeout(() => {
                this._reportTimer = 0;
                if (this._disposed) return;

                // The pane has not been measured since, so this one is read afresh rather than reported off
                // a measurement that is no longer where the pane stands.
                this.sendPosition();
            }, throttle - elapsed);
        }

        // A report is only made for a position the page has not already been told about. Beyond saving a
        // round trip, this is what keeps a page that re-renders on OnScroll from re-rendering forever: a
        // render is what a refresh follows, a refresh is what a measurement follows, and a measurement
        // that reported the position it last reported would start the whole round again.
        //
        // The measurement of the frame this report belongs to is passed in where there is one, so that the
        // report costs no second read of the properties that frame has just read.
        private sendPosition(m?: ScrollMetrics) {
            // A report the throttle has been holding on to is dropped where the reporting was turned off
            // while it waited, rather than delivered to a page that has stopped listening for it.
            if (this._options.scroll === false) return;

            const offset = m ? ScrollablePane.toOffset(m) : ScrollablePane.getOffset(this._element);
            if (!offset) return;

            const signature = `${offset.left}|${offset.top}|${offset.scrollWidth}|${offset.scrollHeight}|${offset.clientWidth}|${offset.clientHeight}|${offset.rtl}`;
            if (signature === this._lastSignature) return;

            // How far the pane moved since the position the page was last told about, on the screen
            // rather than in reading order, so that a page that only wants to know WHICH WAY the reader
            // is going does not have to keep the previous position of its own. The very first report of
            // a pane has nothing to have moved from and carries no direction.
            if (this._lastSignature) {
                const maxLeft = Math.max(0, offset.scrollWidth - offset.clientWidth);
                // Which way the pane reads is the only thing that folds the sign of a scrollLeft away. A
                // NEGATIVE one on a pane that reads left to right is not a right to left reading, it is the
                // elastic overscroll of a pane being bounced past its left edge - and folding that one over
                // would report the pane as standing at the far end of its content.
                const visual = (left: number) => offset.rtl ? maxLeft + left : left;

                offset.deltaLeft = visual(offset.left) - visual(this._lastLeft);
                offset.deltaTop = offset.top - this._lastTop;
            }

            this._lastSignature = signature;
            this._lastLeft = offset.left;
            this._lastTop = offset.top;
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

            // One pointer drags the pane: a second one pressed while the first is down is left to
            // whatever is under it rather than taking the drag over halfway through.
            if (this._drag) return;

            // A drag that starts on something the pointer is meant to be doing something else with - a
            // field being selected in, a control being pressed - is not a drag of the pane.
            const target = e.target as Element | null;
            if (target?.closest?.(BitScrollablePane._noDragSelector)) return;

            const m = ScrollablePane.measure(this._element);
            if (m.maxX <= 0 && m.maxY <= 0) return;

            // A pointer put down on a pane that is still gliding stops it where it is, the way it does on
            // every scrolling surface of every platform.
            this.stopGlide();

            this._drag = {
                id: e.pointerId,
                x: e.clientX,
                y: e.clientY,
                left: this._element.scrollLeft,
                top: this._element.scrollTop,
                moved: false,
                atX: e.clientX,
                atY: e.clientY,
                at: performance.now(),
                vx: 0,
                vy: 0,
            };

            // The rest of the gesture is listened for on the DOCUMENT and only for as long as it lasts. On
            // the element it would be missed altogether by a pointer that leaves the pane before it has
            // travelled the few pixels that take the capture and is released outside - which leaves a drag
            // standing that the next movement across the pane, with no button held at all, would take up.
            const ac = new AbortController();
            this._draggingAbortController = ac;

            document.addEventListener('pointermove', event => this.dragMove(event), { signal: ac.signal });
            document.addEventListener('pointerup', event => this.dragEnd(event), { signal: ac.signal });
            document.addEventListener('pointercancel', event => this.dragEnd(event), { signal: ac.signal });
        }

        private dragMove(e: PointerEvent) {
            const drag = this._drag;
            if (!drag || drag.id !== e.pointerId) return;

            // A release nothing reported - a native drag taking the pointer over, a window that lost the
            // focus while the button was down - is a drag that is over whatever this side last saw.
            if (e.buttons === 0) {
                this.cancelDrag();
                return;
            }

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

            this.sample(drag, e);

            this._element.scrollTo({ left: drag.left - dx, top: drag.top - dy, behavior: 'instant' });
        }

        // How fast the pointer is going, kept as it moves so that a release has a speed to carry on at.
        // It is an average weighted by time rather than by event, so that the flick a drag ends with reads
        // as a flick rather than as the average of the whole gesture, and so that a rate of events the
        // pointer chose does not decide how quickly the answer follows it.
        private sample(drag: ScrollDrag, e: PointerEvent) {
            if (this._options.momentum === false) return;

            const now = performance.now();
            const elapsed = now - drag.at;

            if (elapsed <= 0) return;

            const weight = elapsed / (elapsed + BitScrollablePane._velocityWindow);

            drag.vx += ((e.clientX - drag.atX) / elapsed - drag.vx) * weight;
            drag.vy += ((e.clientY - drag.atY) / elapsed - drag.vy) * weight;

            drag.atX = e.clientX;
            drag.atY = e.clientY;
            drag.at = now;
        }

        private dragEnd(e: PointerEvent) {
            const drag = this._drag;
            if (!drag || drag.id !== e.pointerId) return;

            this._drag = undefined;

            this._draggingAbortController?.abort();
            this._draggingAbortController = undefined;

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

            this.startGlide(drag);
        }

        // A released drag carries on at the speed it was let go at and slows to a stop, which is what
        // every scrolling surface of every platform does with a flick and what a strip of cards dragged
        // with a mouse is otherwise missing.
        private startGlide(drag: ScrollDrag) {
            if (this._options.momentum === false) return;

            // A pointer held still before it was lifted was not a flick, however fast the drag that led
            // up to it was. Nothing says so but the gap since the last move.
            if (performance.now() - drag.at > BitScrollablePane._flickWindow) return;

            // The content moves the other way from the pointer, so a drag to the left is a scroll to the
            // right - the same sign the drag itself was applied with.
            let vx = -drag.vx;
            let vy = -drag.vy;

            if (Math.hypot(vx, vy) < BitScrollablePane._flickSpeed) return;

            // A pane that always comes to rest on an item has its own idea of where a scroll ends, and a
            // glide of ours would be pulled back onto a snap position on every frame of it.
            try {
                if (getComputedStyle(this._element).scrollSnapType.includes('mandatory')) return;
            } catch { /* no computed style to read: the glide is the better guess */ }

            let last = performance.now();

            const step = () => {
                this._glideFrame = 0;

                // Anything the reader does themselves takes the pane back off us.
                if (this._disposed || this._drag || this._options.drag === false || this._options.momentum === false) return;

                const now = performance.now();
                // A frame the tab was in the background for is not a second of travel.
                const elapsed = Math.min(now - last, BitScrollablePane._glideStep);
                last = now;

                const m = ScrollablePane.measure(this._element);
                const left = this._element.scrollLeft;
                const top = this._element.scrollTop;

                ScrollablePane.apply(this._element, m.x + vx * elapsed, m.y + vy * elapsed, m, false);

                const decay = Math.pow(BitScrollablePane._glideFriction, elapsed / 16);

                vx *= decay;
                vy *= decay;

                // The glide is over once it has run out of speed, or out of anywhere to go - and the pane
                // not having moved at all is what says the second of those, since the browser clamps a
                // move it cannot make.
                if (Math.hypot(vx, vy) < BitScrollablePane._glideFloor) return;
                if (this._element.scrollLeft === left && this._element.scrollTop === top) return;

                this._glideFrame = requestAnimationFrame(step);
            };

            this._glideFrame = requestAnimationFrame(step);
        }

        private stopGlide() {
            if (this._glideFrame === 0) return;

            cancelAnimationFrame(this._glideFrame);
            this._glideFrame = 0;
        }

        // Puts a drag down without finishing it, for the two ways one can end that the pointer itself
        // does not report: the pane stopping being draggable, and the whole instance being torn down.
        private cancelDrag() {
            const drag = this._drag;

            this._drag = undefined;

            this._draggingAbortController?.abort();
            this._draggingAbortController = undefined;

            this.stopGlide();

            this._element.removeAttribute('data-bit-scp-drag');

            if (!drag) return;

            try { this._element.releasePointerCapture(drag.id); } catch { /* the pointer is already gone */ }
        }

        // A wheel mouse has no horizontal axis, so a pane that only scrolls sideways would be unreachable
        // with one. This is deliberately the narrowest reading of that: only a wheel that carries nothing
        // horizontal of its own, only over a pane that has somewhere to go sideways and nowhere to go up
        // or down, and only until that pane reaches the end it is being pushed towards - after which the
        // page gets the scroll back rather than being left stuck under the pointer.
        private wheel(e: WheelEvent) {
            if (this._disposed || this._options.wheel === false) return;
            if (e.ctrlKey || e.deltaX !== 0 || e.deltaY === 0) return;

            const slack = BitScrollablePane._edgeTolerance;

            const m = ScrollablePane.measure(this._element);
            if (m.maxX <= 0) return;

            // "Nowhere to go up or down" means nowhere the READER can go, which is not the same as the
            // content fitting: a horizontal pane clips what overflows vertically, and clipped overflow
            // still counts towards scrollHeight - so a strip whose own horizontal scrollbar leaves its
            // content a pixel too tall for its box is still a strip with one axis, and handing the wheel
            // back there would leave it unreachable with a wheel mouse altogether.
            if (this._scrollsY && m.maxY > slack) return;

            // A delta is in pixels, in lines, or in pages, and only the first of the three can be used as
            // it stands. A line is the line of the pane itself rather than a constant, since a pane of
            // large type would otherwise be scrolled a fraction of what a pane of small type is.
            const delta = e.deltaY * (e.deltaMode === 1 ? this.lineHeight()
                : e.deltaMode === 2 ? this._element.clientWidth : 1);

            // A wheel forwards means further into the content, which is towards the visual left edge of
            // a right to left pane and towards the right one of every other.
            const step = m.rtl ? -delta : delta;

            // Within a pixel of the end is at it, or the pane the reader has already pushed as far as it
            // goes keeps swallowing the wheel and the page behind it can never be scrolled.
            if (step < 0 && m.x <= slack) return;
            if (step > 0 && m.x >= m.maxX - slack) return;

            e.preventDefault();

            ScrollablePane.apply(this._element, m.x + step, m.y, m, false);
        }

        // How tall a line of this pane is, for the wheels that count in lines rather than in pixels. It is
        // a style read, so it is taken once and kept: a line-height of `normal` has no length of its own
        // and is taken as the usual fraction over the font size.
        private lineHeight(): number {
            if (this._lineHeight > 0) return this._lineHeight;

            try {
                const styles = getComputedStyle(this._element);
                const line = parseFloat(styles.lineHeight);

                this._lineHeight = line > 0
                    ? line
                    : Math.round((parseFloat(styles.fontSize) || 0) * 1.2) || BitScrollablePane._defaultLineHeight;
            } catch {
                this._lineHeight = BitScrollablePane._defaultLineHeight;
            }

            return this._lineHeight;
        }

        private invoke(method: string, arg: any) {
            try {
                // The rejection is consumed rather than left to surface as an unhandled one in the console
                // of an application that did nothing wrong: a circuit that dropped, or a reference disposed
                // between the frame that measured and the call that reports it, is nothing the page can act
                // on - and the synchronous catch below can never see it, since this call reports its
                // failures by rejecting the promise it returns.
                this._dotnetObj.invokeMethodAsync(method, arg).catch(() => { });
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

        // How near an edge still counts as standing ON it, in pixels. A scroll offset is fractional at a
        // fractional zoom level and on a scaled display, while the maxima it is compared against are
        // derived from sizes the browser rounds to whole numbers - so a pane that has been scrolled as far
        // as it goes comes to rest a fraction of a pixel short of its own maximum, and every exact
        // comparison against an edge is one it can never pass. It is the same pixel of slack
        // BitScrollOffset gives on the .NET side, and it is on top of whatever ReachOffset asks for.
        private static _edgeTolerance = 1;

        // How many of the nodes a change added are asked where they landed before the answer is taken to
        // be "not above the reader". Each one is a rectangle read, and a render can add a great many.
        private static _anchorBudget = 32;

        // How long a stretch of a drag its speed is averaged over, in milliseconds. Short enough that the
        // flick a gesture ends with is the answer, long enough that one stray event is not.
        private static _velocityWindow = 40;

        // How long after the last movement of the pointer a release still counts as a flick, and how fast
        // it has to have been going - in pixels per millisecond - to be worth carrying on at all.
        private static _flickWindow = 90;
        private static _flickSpeed = 0.12;

        // How much of its speed a glide keeps per frame at 60Hz, the longest step it is advanced by (so a
        // frame the tab spent in the background is not a leap), and the speed it is left standing at.
        private static _glideFriction = 0.94;
        private static _glideStep = 34;
        private static _glideFloor = 0.02;

        // How long after the last scroll event a scroll counts as over, where the browser has no scrollend
        // event of its own. Long enough to bridge the gaps in a slow drag, short enough not to be noticed.
        private static _endDelay = 140;

        // What a line of a wheel that counts in lines rather than in pixels - which is what Firefox
        // reports - is worth on a pane whose own line height cannot be read.
        private static _defaultLineHeight = 16;

        private static _hasScrollEnd = typeof window !== 'undefined' && 'onscrollend' in window;

        // Whether the ENGINE keeps the reader's place on its own when content lands above what they are
        // looking at. Every one but WebKit does, and the one that does not is the reason PreserveScroll
        // exists - so the compensation is only ever made where there is none already. Which PANE it is
        // made on is readAnchoring's answer: an engine that supports anchoring is not the same as a pane
        // that is anchored by it.
        private static _supportsScrollAnchoring = typeof CSS !== 'undefined'
            && typeof CSS.supports === 'function'
            && CSS.supports('overflow-anchor', 'auto');
    }
}
