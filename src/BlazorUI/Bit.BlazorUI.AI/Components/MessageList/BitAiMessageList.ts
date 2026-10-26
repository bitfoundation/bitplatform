namespace BitBlazorUI {
    // The browser-side half of BitAiMessageList: it decides whether the reader is at the newest message, follows a reply
    // as it grows while they are, keeps what they are reading in place while they are not, and asks .NET for the older
    // messages a long transcript leaves out as the reader scrolls up to them.
    export class AiMessageList {
        // The distance from the end, in pixels, that a reader scrolling back down only has to come within to be at the
        // newest message again: they are taken the rest of the way, and followed from there.
        private static readonly THRESHOLD = 48;

        private static _observers = new Map<string, AiMessageListObserver>();

        // The list has been scrolled to its newest message by the time it is observed, so the observer starts pinned, as
        // .NET does, and reports only a transition away from there. autoScroll is BitAiUiOptions.AutoScroll: without it
        // the view never follows the content on its own, whatever the reader's position.
        public static observe(id: string, element: HTMLElement, dotnetObj: DotNetObject, autoScroll: boolean) {
            AiMessageList.dispose(id);

            if (!element) return;

            AiMessageList._observers.set(id, new AiMessageListObserver(element, dotnetObj, AiMessageList.THRESHOLD, autoScroll !== false));
        }

        // The options of a list can change while it is open (a BitAiOptionsProvider re-rendering with others).
        public static setAutoScroll(id: string, autoScroll: boolean) {
            AiMessageList._observers.get(id)?.setAutoScroll(autoScroll !== false);
        }

        // Takes the view to the newest message, which .NET does as a conversation opens and from ScrollToLatestAsync, and
        // follows the content from there: .NET has already taken the list as pinned, so the observer is told the same
        // rather than left to find it out a scroll event later (or never, when the view is already there).
        public static scrollToBottom(id: string, element: HTMLElement, smooth: boolean) {
            if (!element) return;

            // An explicit behavior wins over the stylesheet, so the reduced-motion preference is honored here too: the
            // motion factor the stylesheets scale every duration by is 0 under it, and 1 again inside a ForceAnimation
            // (.bit-fam) subtree or wherever an app restores the motion tokens.
            const animate = smooth && AiMessageList._motionFactor(element) > 0;
            const observer = AiMessageList._observers.get(id);

            if (observer) {
                observer.scrollToEnd(animate);
            } else {
                element.scrollTo({ top: element.scrollHeight, behavior: animate ? 'smooth' : 'auto' });
            }
        }

        public static dispose(id: string) {
            const observer = AiMessageList._observers.get(id);
            if (!observer) return;

            AiMessageList._observers.delete(id);
            observer.dispose();
        }

        private static _motionFactor(element: HTMLElement): number {
            try {
                const value = parseFloat(getComputedStyle(element).getPropertyValue('--bit-mot-duration-factor'));

                if (isFinite(value)) return Math.max(0, value);
            } catch { }

            return 1;
        }
    }

    // Watches one transcript.
    //
    // Whether the reader is at the newest message (pinned) is decided from the scroll position, at the moment it changes:
    // in the scroll listener, and again right before the view is moved to the end here, since WebKit moves the view at
    // once and dispatches the scroll a frame later. Every position the view is moved to here is remembered, so a move up
    // from the last one is the reader's - a wheel, a key, a drag, a touch - or the page's (a scrollIntoView, a find in
    // the page), and leaves the newest message whatever the threshold says; the end of content that shrank under the
    // view, where the browser holds it, is still the end. Coming back to within the threshold of the end by scrolling
    // down pins again; so does a change of the content leaving the view at the very end (a cleared transcript). A
    // gesture that may scroll up holds the view where it is for a moment, since a wheel's scroll runs off the main thread
    // and reaches the scroll position a frame or two later, so it is never written over in between.
    //
    // While pinned (and auto-scrolling), every change of the content or of the size of the list - a reply growing, a
    // composer under the list growing - takes the view to the end before it is painted. Otherwise the first message in
    // view is the anchor: it is kept where it was as content above it changes, which is how the older messages the list
    // draws as the reader reaches the start of what it draws come in without moving what is being read. The browser's
    // own scroll anchoring is off for the list (its stylesheet), so the two never correct the same change twice.
    //
    // An upward move is the reader's only when they make it - a gesture holds the view, or the page moved the list (its
    // scrollTop set, its scrollTo / scrollBy called, the focus moved into it). Any other is the layout's: an engine holds the
    // view where it can while it lays out a block replaced near the end of the content (WebKit, under an ancestor with a
    // container query), and the view keeps following through it rather than being read as left behind.
    //
    // The position is reported to .NET only when it changes, so scrolling costs at most one interop call per transition
    // rather than one per frame.
    class AiMessageListObserver {
        // The view is at the very end, however it got there.
        private static readonly END = 2;
        // How long a gesture that may scroll the view up holds it before it is followed again, in milliseconds.
        private static readonly HOLD = 250;
        // The keys that scroll a page up.
        private static readonly UP_KEYS = ['PageUp', 'ArrowUp', 'Home'];
        // The methods a page scrolls an element with.
        private static readonly SCROLL_METHODS = ['scrollTo', 'scrollBy', 'scroll'];

        private _pinned = true;
        private _top: number;
        private _anchor: Element | null = null;
        private _anchorOffset = 0;
        private _anchorScroll = 0;
        private _holdUntil = 0;
        private _holdTimer = 0;
        private _touching = false;
        private _dragging = false;
        private _older: string | null = null;
        private _askedOlder = false;
        private _observed: Element | null = null;
        private _disposed = false;
        // The view is being moved here, which is no move of the page's.
        private _writing = false;
        // The page moved the list since the position was last read.
        private _pageMoved = false;
        private _resize: ResizeObserver;
        private _mutation: MutationObserver;
        private _onScroll: () => void;
        private _onWheel: (e: WheelEvent) => void;
        private _onKeyDown: (e: KeyboardEvent) => void;
        private _onTouchStart: () => void;
        private _onTouchEnd: () => void;
        private _onPointerDown: (e: PointerEvent) => void;
        private _onPointerUp: () => void;
        private _onHoldEnd: () => void;
        private _onFocusIn: () => void;

        constructor(private _element: HTMLElement, private _dotnetObj: DotNetObject, private _threshold: number, private _autoScroll: boolean) {
            this._top = _element.scrollTop;

            this._onScroll = () => this._scrolled();
            this._onWheel = e => { if (e.deltaY < 0) this._hold(); };
            this._onKeyDown = e => this._keyDown(e);
            this._onTouchStart = () => { this._touching = true; };
            this._onTouchEnd = () => {
                this._touching = false;
                this._hold();
            };
            this._onPointerDown = e => this._pointerDown(e);
            this._onPointerUp = () => this._pointerUp();
            this._onHoldEnd = () => this._holdEnded();
            this._onFocusIn = () => this._pageMove();

            this._resize = new ResizeObserver(() => this._changed());

            this._mutation = new MutationObserver(() => {
                this._retarget();
                this._changed();
            });

            // No report as it starts: the list was scrolled to its newest message before it was observed, and measured
            // here, ahead of the layout of what the first render added, it would read as a reader who scrolled away and
            // came straight back (a false and then a true on every open). A growth since then is followed (or, without
            // auto-scroll, measured) from the observers' first callbacks.
            _element.addEventListener('scroll', this._onScroll, { passive: true });
            _element.addEventListener('wheel', this._onWheel, { passive: true });
            _element.addEventListener('touchstart', this._onTouchStart, { passive: true });
            _element.addEventListener('touchend', this._onTouchEnd, { passive: true });
            _element.addEventListener('touchcancel', this._onTouchEnd, { passive: true });
            _element.addEventListener('pointerdown', this._onPointerDown, { passive: true });
            _element.ownerDocument.addEventListener('keydown', this._onKeyDown, { passive: true });
            _element.addEventListener('focusin', this._onFocusIn);

            this._watchPageMoves();

            // The list itself is watched as well as its content: a composer growing under it shrinks the list, which
            // moves the newest message out of view as much as a reply growing does.
            this._resize.observe(_element);
            this._retarget();
            this._mutation.observe(_element, { childList: true, subtree: true, characterData: true });

            if (_autoScroll === false) this._recordAnchor();
        }

        public setAutoScroll(autoScroll: boolean) {
            if (this._disposed || autoScroll === this._autoScroll) return;

            this._autoScroll = autoScroll;

            if (autoScroll) {
                this._changed();
            } else {
                this._recordAnchor();
            }
        }

        // Takes the view to the newest message for .NET, which counts the list as pinned already.
        public scrollToEnd(smooth: boolean) {
            if (this._disposed) return;

            this._writing = true;

            try {
                this._element.scrollTo({ top: this._element.scrollHeight, behavior: smooth ? 'smooth' : 'auto' });
            } finally {
                this._writing = false;
            }

            this.repin();
        }

        // The view was just taken to the newest message for .NET, which counts the list as pinned already.
        public repin() {
            if (this._disposed) return;

            this._pinned = true;
            this._top = this._element.scrollTop;
            this._anchor = null;
        }

        private _scrolled() {
            if (this._disposed) return;

            // A move up while following that neither a gesture nor the page made is the layout's (see the class): the view
            // is taken on to the end rather than read as the reader leaving.
            const el = this._element;

            if (this._following() && this._held() === false && this._pageMoved === false && el.scrollTop < this._top - 1
                && el.scrollHeight - el.scrollTop - el.clientHeight > AiMessageListObserver.END) {
                this._toEnd();
                this._checkStart();
                return;
            }

            this._sync();

            if (this._following() === false) this._recordAnchor();

            this._checkStart();
        }

        // Reads what moved the view since it was last looked at or moved here.
        private _sync() {
            const el = this._element;
            const top = el.scrollTop;
            const moved = top - this._top;
            const distance = el.scrollHeight - top - el.clientHeight;

            this._top = top;
            this._pageMoved = false;

            if (distance <= AiMessageListObserver.END) {
                this._setPinned(true);
            } else if (moved < -1) {
                this._setPinned(false);
            } else if (moved > 1 && distance <= this._threshold) {
                this._setPinned(true);
            }
        }

        // The content or the size of the list changed: follow it, or keep the reader's place in it.
        private _changed() {
            if (this._disposed) return;

            // Following, a scroll of the reader's the listener has not been told of yet takes the view away before it
            // is moved to the end. Not following, the reader's place is kept through the change first - that scroll of
            // theirs with it - and the view is looked at after.
            if (this._following()) {
                const el = this._element;
                const top = el.scrollTop;

                // A move up nobody made is the layout's, and is followed through (see the class).
                if (this._held() || this._pageMoved || top >= this._top - 1 || el.scrollHeight - top - el.clientHeight <= AiMessageListObserver.END) {
                    this._sync();
                }
            }

            if (this._following() === false) {
                this._restoreAnchor();
                this._measure();
            }

            if (this._following() && this._held() === false) {
                this._toEnd();
            }

            this._checkStart();
        }

        // After a change the reader did not make: the view left at the very end (the transcript emptied or shrank under
        // it) is at the newest message again; and without auto-scroll, the newest message growing away from a reader who
        // stays put is a transition like a scroll.
        private _measure() {
            const el = this._element;
            const distance = el.scrollHeight - el.scrollTop - el.clientHeight;

            if (distance <= AiMessageListObserver.END) {
                this._setPinned(true);
            } else if (this._pinned && this._autoScroll === false && distance > this._threshold) {
                this._setPinned(false);
            }
        }

        private _following() {
            return this._pinned && this._autoScroll;
        }

        private _setPinned(pinned: boolean) {
            if (pinned === this._pinned) return;

            this._pinned = pinned;

            // From here on, what the reader is looking at is what stays in place.
            if (pinned === false) this._recordAnchor();

            this._dotnetObj.invokeMethodAsync('OnPinnedChange', pinned).catch(() => { });
        }

        private _toEnd() {
            const el = this._element;

            this._writing = true;

            try {
                el.scrollTop = el.scrollHeight;
            } finally {
                this._writing = false;
            }

            this._top = el.scrollTop;
            this._pageMoved = false;
        }

        // The anchor is the first element of the content still in view at the top of the list, where it is from the top
        // of the list, and where the view was then. A list keeps its messages in document order, so the first one is
        // found by halving.
        private _recordAnchor() {
            this._anchor = null;
            this._anchorScroll = this._element.scrollTop;

            const content = this._element.firstElementChild;
            if (!content) return;

            const items = content.children;
            const top = this._element.getBoundingClientRect().top;

            let low = 0;
            let high = items.length - 1;
            let found = -1;

            while (low <= high) {
                const middle = (low + high) >> 1;

                if (items[middle].getBoundingClientRect().bottom > top) {
                    found = middle;
                    high = middle - 1;
                } else {
                    low = middle + 1;
                }
            }

            if (found < 0) return;

            this._anchor = items[found];
            this._anchorOffset = this._anchor.getBoundingClientRect().top - top;
        }

        // Makes up for what the content above the anchor did to it since it was recorded. The view may have moved in the
        // meantime too - by a scroll of the reader's the listener has not been told of yet, which is kept, or by the
        // browser holding it at the end of content that shrank under it, which is undone - so where the anchor is now is
        // read against that. What the view is moved by here is added to the last position the listener saw as well, so
        // the move of the reader's it has not been told of still reads as theirs when it is.
        private _restoreAnchor() {
            const anchor = this._anchor;
            const el = this._element;

            if (anchor && anchor.isConnected && anchor.parentElement === el.firstElementChild) {
                const top = el.scrollTop;
                const moved = top - this._anchorScroll;
                const shift = anchor.getBoundingClientRect().top - el.getBoundingClientRect().top - this._anchorOffset + moved;
                const held = moved < 0 && el.scrollHeight - top - el.clientHeight <= AiMessageListObserver.END;
                const target = (held ? this._anchorScroll : top) + shift;

                if (Math.abs(target - top) >= 1) {
                    this._writing = true;

                    try {
                        el.scrollTop = target;
                    } finally {
                        this._writing = false;
                    }

                    this._top += el.scrollTop - top;
                }
            }

            this._recordAnchor();
        }

        // Past BitAiUiOptions.VirtualizeThreshold the list draws only its newest messages, and says on its root how many
        // older ones it left out (data-bit-aimsl-old). A reader within a screen of the start of what is drawn asks for
        // the next page of them - once: the attribute changing is what says the page has been drawn, and the next one can
        // be asked for.
        private _checkStart() {
            const el = this._element;
            const older = el.getAttribute('data-bit-aimsl-old');

            if (older !== this._older) {
                this._older = older;
                this._askedOlder = false;
            }

            if (!older || this._askedOlder || el.scrollTop > el.clientHeight) return;

            this._askedOlder = true;
            this._dotnetObj.invokeMethodAsync('OnStartReached').catch(() => { this._askedOlder = false; });
        }

        private _keyDown(e: KeyboardEvent) {
            if (AiMessageListObserver.UP_KEYS.indexOf(e.key) < 0 && (e.key !== ' ' || e.shiftKey === false)) return;

            // A key pressed in the transcript, or on nothing in particular - the body, where a click on the text of a
            // message leaves the focus - scrolls the transcript; one pressed anywhere else on the page does not.
            const target = e.target as Node | null;
            const doc = this._element.ownerDocument;

            if (target && target !== doc.body && target !== doc.documentElement && this._element.contains(target) === false) return;

            this._hold();
        }

        // A press on the list itself rather than on its content is a press on its scrollbar (or on the gutter beside the
        // column): the view is the reader's until it is released.
        private _pointerDown(e: PointerEvent) {
            if (e.target !== this._element || this._dragging) return;

            this._dragging = true;

            const win = this._element.ownerDocument.defaultView;
            win?.addEventListener('pointerup', this._onPointerUp, true);
            win?.addEventListener('pointercancel', this._onPointerUp, true);
        }

        private _pointerUp() {
            this._releasePointer();
            this._changed();
        }

        private _releasePointer() {
            this._dragging = false;

            const win = this._element.ownerDocument.defaultView;
            win?.removeEventListener('pointerup', this._onPointerUp, true);
            win?.removeEventListener('pointercancel', this._onPointerUp, true);
        }

        // A gesture that may scroll the view up has begun: the view is not moved here until the scroll it makes has had
        // time to show in the position. If it moved nothing, following goes on from wherever the content is by then.
        private _hold() {
            if (this._disposed) return;

            this._holdUntil = performance.now() + AiMessageListObserver.HOLD;

            if (this._holdTimer) return;

            this._holdTimer = window.setTimeout(this._onHoldEnd, AiMessageListObserver.HOLD);
        }

        private _holdEnded() {
            this._holdTimer = 0;

            if (this._disposed) return;

            const left = this._holdUntil - performance.now();

            if (left > 0) {
                this._holdTimer = window.setTimeout(this._onHoldEnd, left);
                return;
            }

            this._changed();
        }

        private _held() {
            return this._dragging || this._touching || performance.now() < this._holdUntil;
        }

        // The page moved the list: what it moved it to is where the reader is.
        private _pageMove() {
            if (this._disposed || this._writing) return;

            this._pageMoved = true;
            this._hold();
        }

        // The list's own scroll position and scroll methods, through which a page moves it, report the move before it is
        // made. Own properties of the element, so taking them off again (dispose) leaves the element as it was.
        private _watchPageMoves() {
            const el = this._element as any;
            const self = this;
            const scrollTop = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollTop');

            if (scrollTop?.get && scrollTop.set) {
                const get = scrollTop.get;
                const set = scrollTop.set;

                Object.defineProperty(el, 'scrollTop', {
                    configurable: true,
                    get() { return get.call(this); },
                    set(value: number) {
                        self._pageMove();
                        set.call(this, value);
                    },
                });
            }

            // The method is looked up as it is called rather than kept from now, so one the page puts on the prototype later
            // (a polyfill, a spy) is the one that runs.
            for (const name of AiMessageListObserver.SCROLL_METHODS) {
                if (typeof el[name] !== 'function') continue;

                el[name] = function (this: HTMLElement, ...args: any[]) {
                    self._pageMove();
                    return Object.getPrototypeOf(this)[name].apply(this, args);
                };
            }
        }

        private _unwatchPageMoves() {
            const el = this._element as any;

            delete el.scrollTop;

            for (const name of AiMessageListObserver.SCROLL_METHODS) {
                delete el[name];
            }
        }

        // The content element is watched for growth that changes no element's box the mutation observer would catch
        // (an image loading), and re-targeted if it is replaced.
        private _retarget() {
            const content = this._element.firstElementChild;
            if (content === this._observed) return;

            if (this._observed) {
                this._resize.unobserve(this._observed);
            }

            this._observed = content;

            if (content) {
                this._resize.observe(content);
            }
        }

        public dispose() {
            this._disposed = true;

            const el = this._element;
            el.removeEventListener('scroll', this._onScroll);
            el.removeEventListener('wheel', this._onWheel);
            el.removeEventListener('touchstart', this._onTouchStart);
            el.removeEventListener('touchend', this._onTouchEnd);
            el.removeEventListener('touchcancel', this._onTouchEnd);
            el.removeEventListener('pointerdown', this._onPointerDown);
            el.ownerDocument.removeEventListener('keydown', this._onKeyDown);
            el.removeEventListener('focusin', this._onFocusIn);
            this._unwatchPageMoves();
            this._releasePointer();

            this._resize.disconnect();
            this._mutation.disconnect();

            if (this._holdTimer) clearTimeout(this._holdTimer);

            this._holdTimer = 0;
            this._observed = null;
            this._anchor = null;
        }
    }
}

// bit-lazy: teardown AiMessageList.dispose
