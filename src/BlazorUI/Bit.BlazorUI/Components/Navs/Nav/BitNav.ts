namespace BitBlazorUI {
    export class Nav {
        // The nav owns the arrow keys, Home and End (they walk the tree) and Space on a chevron (it
        // toggles the branch). The browser's default for those keys is to scroll the page, which has to
        // be cancelled *before* the event reaches Blazor's .NET handler. @onkeydown:preventDefault
        // cannot do it: Blazor evaluates it at render time, so it cannot know the upcoming key, lags a
        // keystroke behind (the first arrow press still scrolls) and a stale "true" swallows the Tab
        // that follows, trapping the focus inside the nav. A single capture-phase listener decides per
        // key up front instead, and leaves everything else - Tab, Enter, typing - untouched.
        private static readonly NAV_KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];

        private static installed = false;

        public static install() {
            if (Nav.installed || typeof document === 'undefined') return;
            Nav.installed = true;

            document.addEventListener('keydown', (e: KeyboardEvent) => {
                if (e.ctrlKey || e.altKey || e.metaKey) return;

                const target = e.target as HTMLElement | null;
                if (!target?.closest) return;

                // An editable rendered by a custom item template owns its own keys: Home, End and the
                // arrows move the caret there rather than the focus through the nav.
                if (Nav.isEditable(target)) return;

                // A disabled nav handles no key at all, so nothing of the browser's own behavior is
                // taken away from it either.
                const item = target.closest('.bit-nav-ict, .bit-nav-gcb');
                if (!item || item.closest('.bit-nav.bit-dis')) return;

                // Space and Enter are the activation keys of the chevron, which sits inside the item's own
                // anchor: on top of the page scrolling of Space, the browser also follows that link when a
                // key is pressed on something it contains. Both defaults are cancelled here, exactly like
                // the click of the chevron is, and the toggling is left to the chevron's own handler.
                // Outside the chevron nothing is taken away: Enter keeps activating the item itself.
                if (e.key === ' ' || e.key === 'Spacebar' || e.key === 'Enter') {
                    if (target.closest('.bit-nav-cbt')) {
                        e.preventDefault();
                    }
                    return;
                }

                if (Nav.NAV_KEYS.indexOf(e.key) >= 0) {
                    e.preventDefault();
                }
            }, { capture: true });
        }

        private static isEditable(element: HTMLElement): boolean {
            if (element.isContentEditable) return true;

            const tag = element.tagName;
            return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
        }
    }

    Nav.install();
}
