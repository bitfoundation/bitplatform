namespace BitBlazorUI {
    export class AppShell {
        private static STORE_KEY = 'bit-appshell-scrolls';

        // How many positions are kept. The map is one JSON blob in session storage, so an app that is
        // navigated around for an hour would otherwise carry every url it has ever shown into every
        // write. The oldest entries are the ones dropped, which is what the insertion order of the object
        // gives for free: a url that is visited again is re-inserted at the end by touch().
        private static STORE_MAX = 100;

        public static PreScroll: number = 0;

        // The key the scrolling of the page on screen is stored under: its url, or in the History mode its url
        // and the history entry showing it (see keyOf).
        private static _currentKey: string;
        private static _container: HTMLElement | undefined;
        private static _scrolls: { [key: string]: number | undefined } = {};

        // The frame a pending store is waiting on, and whether anything has changed since the last one was
        // written. A scroll fires many times per gesture and each write is a JSON.stringify plus a
        // synchronous session storage write, so the position is only ever written once per animation frame
        // - and the final position of a gesture is caught by the flush listeners below, which run even
        // when the page is being torn down before that frame ever arrives.
        private static _frame = 0;
        private static _dirty = false;

        private static _flushBound = false;

        // The url the browser's back or forward button last went to, which is what the History mode of the
        // restore tells a traversal apart from a link or a NavigateTo by. popstate is the one event that marks
        // a traversal, and it is listened to in the CAPTURE phase so it is seen before Blazor's own listener
        // hands the navigation to .NET - which on WebAssembly can render the new page, and ask for its restore,
        // before a listener registered after Blazor's would have run at all. It is the url rather than a flag
        // because a traversal .NET never reports - one that only changes the fragment - must not be taken for
        // the next navigation.
        private static _poppedUrl: string | undefined;

        // How long a restore keeps trying to reach a position the content is not tall enough for yet, and
        // what it is trying to reach: -1 while nothing is being restored, which is also what tells the
        // scroll listener that the moves it is seeing are the reader's own.
        private static RESTORE_WINDOW = 2000;
        private static _restoreTop = -1;
        private static _restoreEnd = 0;
        private static _restoreFrame = 0;
        private static _restoreEvents = ['wheel', 'touchstart', 'pointerdown', 'keydown'];

        public static initScroll(container: HTMLElement, url: string, historyOnly?: boolean) {
            AppShell._container = container;
            AppShell._scrolls = AppShell.read();
            const key = AppShell._currentKey = AppShell.keyOf(url, historyOnly);
            AppShell.storeScroll(key, AppShell.PreScroll > 0 ? AppShell.PreScroll : AppShell._scrolls[key]);
            // Spent. It is where the reader had got to on the shell the SERVER rendered, so it only ever
            // belongs to the first url of the session; a shell whose persistence is turned on again later
            // would otherwise be handed a position from a page it has long since navigated away from.
            AppShell.PreScroll = 0;
            // A page opened at a position of 0 is left exactly where it is rather than being sent to the
            // top: the browser may already have scrolled the container to the fragment of the url it was
            // opened at, and a restore of a position nobody ever stored would undo it.
            if (AppShell._scrolls[key]! > 0) {
                AppShell.restore(AppShell._scrolls[key]);
            }
            AppShell.addScroll();
            AppShell.bindFlush();
        }

        public static locationChangedScroll() {
            // Whatever was being restored belongs to the page being left.
            AppShell.cancelRestore();
            // The position of the page being left is written out now rather than being left to the frame
            // a pending store is waiting on: the next thing to happen is a render of the new page, and a
            // scroll of the container to the new page's position would be read by that pending store as
            // the position of the OLD url.
            AppShell.flush();
            AppShell.removeScroll();
        }

        public static afterRenderScroll(url: string, historyOnly?: boolean) {
            const key = AppShell._currentKey = AppShell.keyOf(url, historyOnly);
            AppShell.storeScroll(key, AppShell._scrolls[key]);
            // In the History mode only the back and forward buttons put the reader back where they left a page;
            // any other navigation to it opens it at its top, as a browser does for a page scrolling the document.
            const traversal = AppShell._poppedUrl === url;
            AppShell._poppedUrl = undefined;
            if (AppShell._scrolls[key]! > 0 && (!historyOnly || traversal)) {
                AppShell.restore(AppShell._scrolls[key]);
            } else if (url.indexOf('#') < 0) {
                // A page with nothing stored, or stored at 0, opens at its top. The container is the one the
                // page being left was scrolled in, and nothing else moves it on a navigation, so without this
                // a page never visited before would open at whatever depth the previous one was left at.
                // A url with a fragment is the exception: the browser scrolls that one to its target itself.
                AppShell._container?.scrollTo({ top: 0, behavior: 'instant' });
            }
            AppShell.addScroll();
        }

        public static disposeScroll() {
            AppShell.cancelRestore();
            AppShell.flush();
            AppShell.removeScroll();
            AppShell.unbindFlush();
            AppShell._container = undefined;
        }

        // Empties the stored positions, both the ones in hand and the ones in session storage, so that an
        // application that signs a user out does not restore the previous one's place in the pages the
        // next one visits. Given a url, only that page is forgotten - a page whose content has been
        // replaced under the reader is no longer at the position it was left at.
        public static clearScrolls(url?: string) {
            AppShell.cancelRestore();

            if (url) {
                // The url's own key and the key of every history entry the History mode kept it under.
                Object.keys(AppShell._scrolls)
                    .filter(k => k === url || k.indexOf(url + AppShell.ENTRY_SEPARATOR) === 0)
                    .forEach(k => delete AppShell._scrolls[k]);
                AppShell._dirty = true;
                AppShell.flush();
                return;
            }

            AppShell._scrolls = {};
            AppShell.cancelFrame();
            AppShell._dirty = false;

            try {
                window.sessionStorage.removeItem(AppShell.STORE_KEY);
            } catch { /* storage unavailable; the in-memory map is cleared either way */ }
        }

        private static addScroll() {
            // Passive: nothing here ever prevents the scroll, and saying so keeps the browser from waiting
            // on this listener before it paints the next frame of the app's primary scroller.
            AppShell._container?.addEventListener('scroll', AppShell.onScroll, { passive: true });
        }

        private static removeScroll() {
            AppShell._container?.removeEventListener('scroll', AppShell.onScroll);
        }

        private static onScroll() {
            // A move this class is making itself is not the reader's place to keep. Without this the
            // scroll event of a restore that the content was not tall enough for yet would store the
            // position it was CLAMPED to, and the place being restored to would be lost on the way to it.
            if (AppShell._restoreTop >= 0) return;

            AppShell.storeScroll(AppShell._currentKey, AppShell._container?.scrollTop);
        }

        // The property of history.state the History mode marks an entry with, and what joins it to the url.
        private static ENTRY_STATE = '_bitAshEntry';
        private static ENTRY_SEPARATOR = '\n';

        // What the position of a page is stored under. In the Url mode that is the url, so every visit to it
        // shares one place. In the History mode it is the history ENTRY: a page reached twice by links is two
        // entries, and the top a new visit opens at must not be stored over where the reader left the earlier
        // one, which is where the back button takes them. The entry is told apart by an id this keeps in its
        // history.state - which the browser hands back on a traversal and keeps across a reload - merged into
        // whatever the router put there. A navigation that replaces the entry replaces its state too, and is
        // given a fresh id, which is right: it is a new visit. A state the router keeps as something other
        // than an object cannot carry the id, and the url is used instead.
        private static keyOf(url: string, historyOnly?: boolean): string {
            if (!historyOnly) return url;

            try {
                const state = history.state;
                if (state != null && typeof state !== 'object') return url;

                let entry = state?.[AppShell.ENTRY_STATE];
                if (typeof entry !== 'string') {
                    entry = Date.now().toString(36) + Math.random().toString(36).slice(2, 8);
                    history.replaceState(Object.assign({}, state, { [AppShell.ENTRY_STATE]: entry }), '');
                }

                return url + AppShell.ENTRY_SEPARATOR + entry;
            } catch {
                return url;
            }
        }

        // Puts the container back where the url was left. The content of a page being returned to is
        // rarely as tall as it will be by the time it has finished arriving - a fetch still in flight, an
        // image without a size in its markup, a virtualized list that has only rendered its first screen -
        // and a container that is not tall enough yet clamps the move to wherever it can reach. So the
        // move is repeated as the content grows, until it lands or until the window below is spent.
        private static restore(top: number | undefined) {
            AppShell.cancelRestore();

            const container = AppShell._container;
            if (!container) return;

            const target = Math.max(0, top || 0);

            container.scrollTo({ top: target, behavior: 'instant' });

            // The top of the content is where a page that was never scrolled opens, and it is reachable
            // however short the content is, so there is nothing to wait for.
            if (target === 0) return;

            AppShell._restoreTop = target;
            AppShell._restoreEnd = AppShell.now() + AppShell.RESTORE_WINDOW;
            AppShell.bindRestoreCancel();
            AppShell._restoreFrame = requestAnimationFrame(AppShell.restoreStep);
        }

        private static restoreStep() {
            AppShell._restoreFrame = 0;

            const container = AppShell._container;
            const target = AppShell._restoreTop;
            if (!container || target < 0) return;

            const max = Math.max(0, container.scrollHeight - container.clientHeight);
            const reachable = Math.min(target, max);

            if (Math.abs(container.scrollTop - reachable) > 1) {
                container.scrollTo({ top: target, behavior: 'instant' });
            }

            // Landed, or out of time. Either way what was stored for this url is left as it was, so a
            // page whose content never grew that far is still put back there the next time it is opened.
            if (max >= target - 1 || AppShell.now() >= AppShell._restoreEnd) {
                AppShell.cancelRestore();
                return;
            }

            AppShell._restoreFrame = requestAnimationFrame(AppShell.restoreStep);
        }

        private static cancelRestore() {
            if (AppShell._restoreFrame) {
                cancelAnimationFrame(AppShell._restoreFrame);
                AppShell._restoreFrame = 0;
            }

            AppShell._restoreTop = -1;
            AppShell.unbindRestoreCancel();
        }

        // Being put back where they left off is worth nothing to a reader who is already going somewhere
        // else, so the first thing they do gives up on the restore. The four events are the ways a scroll
        // is asked for that are not this class asking for it; the scroll event itself is not one of them,
        // since every move the restore makes raises one.
        private static bindRestoreCancel() {
            AppShell._restoreEvents.forEach(e =>
                window.addEventListener(e, AppShell.cancelRestore, { passive: true, capture: true }));
        }

        private static unbindRestoreCancel() {
            AppShell._restoreEvents.forEach(e =>
                window.removeEventListener(e, AppShell.cancelRestore, { capture: true } as any));
        }

        private static now(): number {
            try {
                return performance.now();
            } catch {
                return Date.now();
            }
        }

        private static storeScroll(key: string, value: number | undefined) {
            if (!key) return;

            const known = key in AppShell._scrolls;

            AppShell._scrolls[key] = value || 0;

            // A key already at the end of the order is where touch() would put it, and the cap was
            // enforced when it got there - so the scrolling of one page, which stores a position per
            // frame, does not re-key the map and walk its keys for every one of them.
            if (known === false || AppShell._mru !== key) {
                AppShell.touch(key);
            }

            AppShell.schedule();
        }

        // The url at the end of the insertion order, so a repeated store of the same page can tell that
        // there is nothing to move.
        private static _mru: string | undefined;

        // Moves a url to the end of the insertion order and drops whatever falls out of the cap, so the
        // map stays bounded by the pages most recently looked at rather than by every page ever visited.
        private static touch(key: string) {
            AppShell._mru = key;

            const value = AppShell._scrolls[key];
            delete AppShell._scrolls[key];
            AppShell._scrolls[key] = value;

            const keys = Object.keys(AppShell._scrolls);
            for (let i = 0; i < keys.length - AppShell.STORE_MAX; i++) {
                delete AppShell._scrolls[keys[i]];
            }
        }

        private static schedule() {
            AppShell._dirty = true;

            if (AppShell._frame) return;

            AppShell._frame = requestAnimationFrame(() => {
                AppShell._frame = 0;
                AppShell.write();
            });
        }

        // Writes whatever is pending right now, for the moments there is no next frame to wait for: the
        // page being hidden, the tab being closed, the navigation that is about to re-render the shell.
        private static flush() {
            AppShell.cancelFrame();
            AppShell.write();
        }

        private static cancelFrame() {
            if (AppShell._frame === 0) return;

            cancelAnimationFrame(AppShell._frame);
            AppShell._frame = 0;
        }

        private static write() {
            if (AppShell._dirty === false) return;

            AppShell._dirty = false;

            try {
                window.sessionStorage.setItem(AppShell.STORE_KEY, JSON.stringify(AppShell._scrolls));
            } catch { /* private mode, disabled storage or a full quota; the positions stay in memory */ }
        }

        private static read(): { [key: string]: number | undefined } {
            try {
                const stored = JSON.parse(sessionStorage.getItem(AppShell.STORE_KEY) || '{}');
                return (stored && typeof stored === 'object') ? stored : {};
            } catch {
                // Unavailable storage, or a value another script left behind that is not the map this
                // wrote. Either way there is nothing to restore, and starting from an empty map is what
                // keeps every write after this one working.
                return {};
            }
        }

        private static onFlush() {
            AppShell.flush();
        }

        private static bindFlush() {
            if (AppShell._flushBound) return;

            AppShell._flushBound = true;

            // pagehide is the one teardown notification a mobile browser reliably gives - unload is not
            // fired when a tab is discarded or restored from the back/forward cache - and the hidden half
            // of visibilitychange covers the app being switched away from without being torn down at all.
            window.addEventListener('pagehide', AppShell.onFlush);
            document.addEventListener('visibilitychange', AppShell.onFlush);
            window.addEventListener('popstate', AppShell.onPopState, { capture: true });
        }

        private static unbindFlush() {
            if (AppShell._flushBound === false) return;

            AppShell._flushBound = false;

            window.removeEventListener('pagehide', AppShell.onFlush);
            document.removeEventListener('visibilitychange', AppShell.onFlush);
            window.removeEventListener('popstate', AppShell.onPopState, { capture: true });
            AppShell._poppedUrl = undefined;
        }

        private static onPopState() {
            AppShell._poppedUrl = location.href;
        }



        // How much of the layout viewport the on-screen keyboard is covering, published on the root of a
        // shell as --bit-ash-keyboard-inset so its own stylesheet can take that much off the height of the
        // scrolling middle - and so the page can position chrome of its own against the same number.
        //
        // The visual viewport is the only place this can be read from: opening the keyboard leaves the
        // LAYOUT viewport (and therefore every percentage height on the page) exactly as it was on the
        // platforms that need this, so a shell measured in percentages goes on believing it owns a screen
        // whose bottom is now behind the keyboard. Where the browser does shrink the layout viewport
        // instead - a page asking for `interactive-widget=resizes-content`, or a desktop browser - the two
        // viewports keep matching and this reports 0, which is the right answer: there is nothing left to
        // take off a height that has already been taken off.
        private static _keyboards: { [key: string]: { element: HTMLElement, handler: () => void, frame: number, last: number, style: HTMLStyleElement, dotnetObj?: DotNetObject } } = {};

        public static setupKeyboard(id: string, element: HTMLElement, dotnetObj?: DotNetObject) {
            if (!element) return;

            AppShell.disposeKeyboard(id);

            const viewport = window.visualViewport;
            if (!viewport) return;

            // The number is published through a stylesheet of this shell's own rather than as an inline
            // custom property, because the style attribute of the root is written by Blazor on every
            // render that changes it and anything this side had put there would be wiped - after which
            // the unchanged-inset check below would never write it again while the keyboard stayed open.
            // The shell is addressed by an attribute the renderer never knew about, so nothing removes
            // it either, and the class is repeated in the selector to outweigh the 0px the stylesheet
            // declares whatever order the two are loaded in.
            const style = document.createElement('style');
            document.head.appendChild(style);
            element.setAttribute('data-bit-ash-kbd', id);

            const state = { element, handler: () => { }, frame: 0, last: -1, style, dotnetObj };

            const measure = () => {
                state.frame = 0;

                // A pinch-zoomed page shrinks its visual viewport in exactly the way an open keyboard
                // does, so measuring through one would report a keyboard that is not there - and taking
                // that much off the shell while the reader is zoomed in is the one thing worse than
                // ignoring the keyboard. The measurement is left at whatever it was until the zoom is let
                // go of, which keeps an open keyboard accounted for through a zoom as well.
                if (Math.abs((viewport.scale || 1) - 1) > 0.01) return;

                // The height of the LAYOUT viewport, which is what the keyboard does not shrink on the
                // platforms this is for - read off the document element rather than as window.innerHeight
                // because that one counts the horizontal scrollbar the visual viewport height leaves out,
                // and the difference between the two would be reported as a keyboard of that height.
                const layout = document.documentElement?.clientHeight || window.innerHeight;

                // offsetTop is how far the visual viewport has itself been pushed down the layout one,
                // which is what a page scrolled by the browser to keep a focused field in view leaves
                // behind; without it the keyboard would appear to grow by that much.
                const inset = Math.max(0, Math.round(layout - viewport.height - viewport.offsetTop));

                if (inset === state.last) return;

                const grew = inset > Math.max(0, state.last);

                state.last = inset;
                state.style.textContent = `.bit-ash[data-bit-ash-kbd="${id}"]{--bit-ash-keyboard-inset:${inset}px}`;

                // The browser brings the focused field into view as the keyboard opens, but it does so against
                // the shell as it stood before the line above shortened its middle - so a field it left just
                // above the keyboard can now be below the bottom of the container, clipped out of sight.
                if (grew) {
                    AppShell.revealFocused(state.element);
                }

                // A marker for the CSS that cannot be written against a length - the bottom bar a shell
                // hides while the reader is typing, the map that drops its controls - so a page does not
                // have to round-trip through C# to know the keyboard is up.
                if (inset > 0) {
                    state.element.setAttribute('data-bit-ash-keyboard', '');
                } else {
                    state.element.removeAttribute('data-bit-ash-keyboard');
                }

                // And the same number for the page that has to place something in C# rather than in CSS.
                // It is sent on the change rather than per frame, and a failed send is a shell that has
                // gone away between the measurement and the call, which is nothing this can act on.
                state.dotnetObj?.invokeMethodAsync('OnKeyboardInset', inset).catch(() => { });
            };

            state.handler = () => {
                if (state.frame) return;
                state.frame = requestAnimationFrame(measure);
            };

            viewport.addEventListener('resize', state.handler, { passive: true });
            viewport.addEventListener('scroll', state.handler, { passive: true });

            AppShell._keyboards[id] = state;

            measure();
        }

        public static disposeKeyboard(id: string) {
            const state = AppShell._keyboards[id];
            if (!state) return;

            delete AppShell._keyboards[id];

            if (state.frame) {
                cancelAnimationFrame(state.frame);
            }

            const viewport = window.visualViewport;
            if (viewport) {
                viewport.removeEventListener('resize', state.handler);
                viewport.removeEventListener('scroll', state.handler);
            }

            state.style.remove();
            state.element.removeAttribute('data-bit-ash-kbd');
            state.element.removeAttribute('data-bit-ash-keyboard');
        }

        // Scrolls the main container of a shell just far enough to bring the focused element inside it back above
        // its bottom edge (less the scroll padding the page set), and never so far that its top goes under the top
        // edge - a tall text area keeps its first line in view. Only this one container is moved: the document of
        // a shell page does not scroll, and the visual viewport is the browser's to place.
        private static revealFocused(root: HTMLElement) {
            const main = root.querySelector<HTMLElement>(':scope > .bit-ash-center > [data-bit-ash-main]');
            const active = document.activeElement as HTMLElement | null;
            if (!main || !active || active === main || !main.contains(active)) return;

            const style = getComputedStyle(main);
            const box = main.getBoundingClientRect();
            const rect = active.getBoundingClientRect();
            const top = box.top + (parseFloat(style.scrollPaddingTop) || 0);
            const bottom = box.top + main.clientHeight - (parseFloat(style.scrollPaddingBottom) || 0);

            const by = Math.min(rect.bottom - bottom, rect.top - top);
            if (by <= 0) return;

            main.scrollBy({ top: by, behavior: 'instant' as ScrollBehavior });
        }



        // The scroll state of a shell's main container, written onto its root as two attributes for the page's CSS:
        // data-bit-ash-scrolled while it is away from its top, and data-bit-ash-scroll-direction (up or down) for
        // the way it was last moved. Nothing goes back to .NET, which is the point: a header that hides while the
        // reader scrolls down has to react within the frame, not after a round trip per frame.
        private static SCROLL_TRAVEL = 8;
        private static _scrollStates: { [key: string]: { root: HTMLElement, main: HTMLElement, handler: () => void, frame: number, scrolled: boolean, direction: string, turn: number, last: number } } = {};

        public static setupScrollState(id: string, root: HTMLElement, main: HTMLElement) {
            AppShell.disposeScrollState(id);

            if (!root || !main) return;

            const state = { root, main, handler: () => { }, frame: 0, scrolled: false, direction: '', turn: 0, last: 0 };

            const measure = () => {
                state.frame = 0;

                // Clamped, so the rubber band of an overscroll past either edge is not read as a scroll the other
                // way as it springs back.
                const max = Math.max(0, main.scrollHeight - main.clientHeight);
                const top = Math.min(max, Math.max(0, main.scrollTop));

                // Within a pixel of the top is at it: a scroll offset is fractional on a scaled display.
                const scrolled = top > 1;

                // More than a screenful in one frame is a jump rather than a scroll: PersistScroll putting the reader
                // back on a page, End, a link to an anchor. None of them is the reader heading down the content, and a
                // header hidden by one would greet them gone on a page they have only just arrived at.
                const jumped = Math.abs(top - state.last) > main.clientHeight;
                state.last = top;

                let direction = state.direction;
                if (scrolled === false || jumped) {
                    // Back at the top there is no direction to hide anything for.
                    direction = '';
                    state.turn = top;
                } else if (direction === 'down') {
                    // The turning point follows the container while it keeps going the same way, and the direction
                    // only flips once it has come back by more than a few pixels from the furthest point it reached.
                    if (top > state.turn) state.turn = top;
                    else if (top < state.turn - AppShell.SCROLL_TRAVEL) { direction = 'up'; state.turn = top; }
                } else if (direction === 'up') {
                    if (top < state.turn) state.turn = top;
                    else if (top > state.turn + AppShell.SCROLL_TRAVEL) { direction = 'down'; state.turn = top; }
                } else if (Math.abs(top - state.turn) > AppShell.SCROLL_TRAVEL) {
                    direction = top > state.turn ? 'down' : 'up';
                    state.turn = top;
                }

                if (scrolled !== state.scrolled) {
                    state.scrolled = scrolled;
                    if (scrolled) {
                        root.setAttribute('data-bit-ash-scrolled', '');
                    } else {
                        root.removeAttribute('data-bit-ash-scrolled');
                    }
                }

                if (direction !== state.direction) {
                    state.direction = direction;
                    if (direction) {
                        root.setAttribute('data-bit-ash-scroll-direction', direction);
                    } else {
                        root.removeAttribute('data-bit-ash-scroll-direction');
                    }
                }
            };

            state.handler = () => {
                if (state.frame) return;
                state.frame = requestAnimationFrame(measure);
            };

            main.addEventListener('scroll', state.handler, { passive: true });

            AppShell._scrollStates[id] = state;

            // A container that is already scrolled - restored by PersistScroll, or by the browser to the fragment
            // of the url - is marked as such straight away. It has no direction until it is moved.
            state.turn = state.last = Math.max(0, main.scrollTop);
            measure();
        }

        public static disposeScrollState(id: string) {
            const state = AppShell._scrollStates[id];
            if (!state) return;

            delete AppShell._scrollStates[id];

            if (state.frame) {
                cancelAnimationFrame(state.frame);
            }

            state.main.removeEventListener('scroll', state.handler);
            state.root.removeAttribute('data-bit-ash-scrolled');
            state.root.removeAttribute('data-bit-ash-scroll-direction');
        }
    }
}

(function () {
    // The scrolling the reader does before Blazor has started, on the shell rendered by the server. Only
    // the FIRST shell of a page can be scrolled at this point, since it is the one in the server's markup
    // and a second shell is something only a started application can render.
    function bind() {
        // The well-known id first, then the attribute every app shell's container carries whatever its id
        // is: a shell given an Id of its own derives its container id from that one, and the scrolling
        // done before Blazor has started is worth keeping for it too.
        const container = document.getElementById('BitAppShell-container')
            ?? document.querySelector<HTMLElement>('[data-bit-ash-main]');

        if (!container) return false;

        container.addEventListener('scroll', () => {
            BitBlazorUI.AppShell.PreScroll = container.scrollTop;
        }, { passive: true });

        return true;
    }

    // The script is meant to run after the markup it is looking for, which is where a Blazor host page puts
    // it. A page that loads it from the head instead is still served, by looking again once the document
    // has been parsed - the scrolling this keeps is the reader's, and it only happens after that anyway.
    if (bind() === false && document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => { bind(); }, { once: true });
    }

    // The keys that scroll a page - the arrows, Page Up/Down, Home/End and the space bar - are aimed by the
    // browser at whatever the reader last focused or pressed on, and at the document while that is nothing at
    // all. The document of a page whose application scrolls in a shell does not scroll, so until the reader
    // has clicked or tabbed into the shell those keys did nothing in any engine. While that is the case they
    // are handed to the main container of the shell instead, which is where the document's scrolling went.
    // Once the reader has pressed inside a container the browser aims the keys there on its own, so that is
    // left to it - and so is a page whose document does scroll, and any key a handler of the page took.
    let lastDown: EventTarget | null = null;

    window.addEventListener('pointerdown', e => { lastDown = e.target; }, { capture: true, passive: true });

    window.addEventListener('keydown', e => {
        if (e.defaultPrevented || e.isComposing || e.altKey || e.ctrlKey || e.metaKey) return;

        const body = document.body;
        if (!body || (e.target !== body && e.target !== document.documentElement) || body.isContentEditable) return;

        if (lastDown instanceof Element && lastDown.isConnected && lastDown.closest('[data-bit-ash-main]')) return;

        const space = e.key === ' ' || e.key === 'Spacebar';
        if (e.shiftKey && space === false) return;

        // The one shell of the page: a shell nested in another one - an example on a page, a preview in a
        // dialog - is a region of that page rather than the page, and a page of several side by side has no
        // one region the document's scrolling belongs to.
        const mains = Array.from(document.querySelectorAll<HTMLElement>('[data-bit-ash-main]'))
            .filter(m => m.parentElement?.closest('[data-bit-ash-main]') == null);
        if (mains.length !== 1) return;

        const main = mains[0];
        const line = 40;
        const page = Math.max(line, main.clientHeight * 0.875);

        let left = 0, top = 0, to: number | undefined;
        switch (space ? ' ' : e.key) {
            case 'ArrowDown': top = line; break;
            case 'ArrowUp': top = -line; break;
            case 'ArrowRight': left = line; break;
            case 'ArrowLeft': left = -line; break;
            case 'PageDown': top = page; break;
            case 'PageUp': top = -page; break;
            case ' ': top = e.shiftKey ? -page : page; break;
            case 'Home': to = 0; break;
            case 'End': to = main.scrollHeight; break;
            default: return;
        }

        const horizontal = left !== 0;
        const doc = document.scrollingElement || document.documentElement;
        if (horizontal ? doc.scrollWidth > doc.clientWidth + 1 : doc.scrollHeight > doc.clientHeight + 1) return;

        // The reader is not to move a shell the page stopped from scrolling (NoScroll, an Overflow of Hidden),
        // and one with nothing to scroll along that axis leaves the key to the page.
        const overflow = getComputedStyle(main)[horizontal ? 'overflowX' : 'overflowY'];
        if (overflow !== 'auto' && overflow !== 'scroll') return;
        if (horizontal ? main.scrollWidth <= main.clientWidth : main.scrollHeight <= main.clientHeight) return;

        e.preventDefault();

        // Animated as the container's own scroll-behavior says - smooth unless the reader asked for reduced
        // motion - except for a held key, whose repeats would each restart the animation from wherever the
        // last one had got to and crawl.
        const behavior: ScrollBehavior | undefined = e.repeat ? 'instant' as ScrollBehavior : undefined;
        if (to !== undefined) {
            main.scrollTo({ top: to, behavior });
        } else {
            main.scrollBy({ left, top, behavior });
        }
    });
}());
