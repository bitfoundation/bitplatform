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

        public static initScroll(container: HTMLElement, url: string) {
            AppShell._container = container;
            AppShell._currentUrl = url;
            AppShell._scrolls = AppShell.read();
            AppShell.storeScroll(url, AppShell.PreScroll > 0 ? AppShell.PreScroll : AppShell._scrolls[url]);
            // Spent. It is where the reader had got to on the shell the SERVER rendered, so it only ever
            // belongs to the first url of the session; a shell whose persistence is turned on again later
            // would otherwise be handed a position from a page it has long since navigated away from.
            AppShell.PreScroll = 0;
            if (AppShell._scrolls[url]! > 0) {
                AppShell._container.scrollTo({ top: AppShell._scrolls[url], behavior: 'instant' });
            }
            AppShell.addScroll();
            AppShell.bindFlush();
        }

        public static locationChangedScroll() {
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
            AppShell._container?.scrollTo({ top: AppShell._scrolls[url], behavior: 'instant' });
            AppShell.addScroll();
        }

        public static disposeScroll() {
            AppShell.flush();
            AppShell.removeScroll();
            AppShell.unbindFlush();
            AppShell._container = undefined;
        }

        // Empties the stored positions, both the ones in hand and the ones in session storage, so that an
        // application that signs a user out does not restore the previous one's place in the pages the
        // next one visits.
        public static clearScrolls() {
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
            AppShell.storeScroll(AppShell._currentUrl, AppShell._container?.scrollTop);
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
        private static _keyboards: { [key: string]: { element: HTMLElement, handler: () => void, frame: number, last: number } } = {};

        public static setupKeyboard(id: string, element: HTMLElement) {
            if (!element) return;

            AppShell.disposeKeyboard(id);

            const viewport = window.visualViewport;
            if (!viewport) return;

            const state = { element, handler: () => { }, frame: 0, last: -1 };

            const measure = () => {
                state.frame = 0;

                // offsetTop is how far the visual viewport has itself been pushed down the layout one,
                // which is what a page scrolled by the browser to keep a focused field in view leaves
                // behind; without it the keyboard would appear to grow by that much.
                const inset = Math.max(0, Math.round(window.innerHeight - viewport.height - viewport.offsetTop));

                if (inset === state.last) return;

                state.last = inset;
                state.element.style.setProperty('--bit-ash-keyboard-inset', `${inset}px`);
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
        }
    }
}

(function () {
    // The scrolling the reader does before Blazor has started, on the shell rendered by the server. It is
    // read off the well-known id the FIRST shell of a page carries, since that is the only one that can
    // exist at this point: the element is in the server's markup, and a second shell is something only a
    // started application can render.
    const container = document.getElementById('BitAppShell-container');
    if (!container) return;

    container.addEventListener('scroll', () => {
        BitBlazorUI.AppShell.PreScroll = container.scrollTop;
    }, { passive: true });
}());
