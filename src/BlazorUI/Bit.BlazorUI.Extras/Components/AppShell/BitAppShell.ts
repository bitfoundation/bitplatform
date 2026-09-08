namespace BitBlazorUI {
    export class AppShell {
        private static STORE_KEY = 'bit-appshell-scrolls';

        // How many positions are kept. The map is one JSON blob in session storage, so an app that is
        // navigated around for an hour would otherwise carry every url it has ever shown into every
        // write. The oldest entries are the ones dropped, which is what the insertion order of the object
        // gives for free: a url that is visited again is re-inserted at the end by touch().
        private static STORE_MAX = 100;

        public static PreScroll: number = 0;

        private static _currentUrl: string;
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

        // How long a restore keeps trying to reach a position the content is not tall enough for yet, and
        // what it is trying to reach: -1 while nothing is being restored, which is also what tells the
        // scroll listener that the moves it is seeing are the reader's own.
        private static RESTORE_WINDOW = 2000;
        private static _restoreTop = -1;
        private static _restoreEnd = 0;
        private static _restoreFrame = 0;
        private static _restoreEvents = ['wheel', 'touchstart', 'pointerdown', 'keydown'];

        public static initScroll(container: HTMLElement, url: string) {
            AppShell._container = container;
            AppShell._currentUrl = url;
            AppShell._scrolls = AppShell.read();
            AppShell.storeScroll(url, AppShell.PreScroll > 0 ? AppShell.PreScroll : AppShell._scrolls[url]);
            // Spent. It is where the reader had got to on the shell the SERVER rendered, so it only ever
            // belongs to the first url of the session; a shell whose persistence is turned on again later
            // would otherwise be handed a position from a page it has long since navigated away from.
            AppShell.PreScroll = 0;
            // A page opened at a position of 0 is left exactly where it is rather than being sent to the
            // top: the browser may already have scrolled the container to the fragment of the url it was
            // opened at, and a restore of a position nobody ever stored would undo it.
            if (AppShell._scrolls[url]! > 0) {
                AppShell.restore(AppShell._scrolls[url]);
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

        public static afterRenderScroll(url: string) {
            AppShell._currentUrl = url;
            AppShell.storeScroll(url, AppShell._scrolls[url]);
            AppShell.restore(AppShell._scrolls[url]);
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
                delete AppShell._scrolls[url];
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

            AppShell.storeScroll(AppShell._currentUrl, AppShell._container?.scrollTop);
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

        private static storeScroll(url: string, value: number | undefined) {
            if (!url) return;

            AppShell._scrolls[url] = value || 0;
            AppShell.touch(url);
            AppShell.schedule();
        }

        // Moves a url to the end of the insertion order and drops whatever falls out of the cap, so the
        // map stays bounded by the pages most recently looked at rather than by every page ever visited.
        private static touch(url: string) {
            const value = AppShell._scrolls[url];
            delete AppShell._scrolls[url];
            AppShell._scrolls[url] = value;

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
        }

        private static unbindFlush() {
            if (AppShell._flushBound === false) return;

            AppShell._flushBound = false;

            window.removeEventListener('pagehide', AppShell.onFlush);
            document.removeEventListener('visibilitychange', AppShell.onFlush);
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
        private static _keyboards: { [key: string]: { element: HTMLElement, handler: () => void, frame: number, last: number, dotnetObj?: DotNetObject } } = {};

        public static setupKeyboard(id: string, element: HTMLElement, dotnetObj?: DotNetObject) {
            if (!element) return;

            AppShell.disposeKeyboard(id);

            const viewport = window.visualViewport;
            if (!viewport) return;

            const state = { element, handler: () => { }, frame: 0, last: -1, dotnetObj };

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

                state.last = inset;
                state.element.style.setProperty('--bit-ash-keyboard-inset', `${inset}px`);

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

            state.element.style.removeProperty('--bit-ash-keyboard-inset');
            state.element.removeAttribute('data-bit-ash-keyboard');
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
}());
