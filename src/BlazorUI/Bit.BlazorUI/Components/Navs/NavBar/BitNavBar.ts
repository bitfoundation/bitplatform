namespace BitBlazorUI {
    export class NavBar {
        // The navbar owns the arrow keys, Home and End: they move the focus along the bar. The browser's
        // default for those keys is to scroll the page, which has to be cancelled *before* the event
        // reaches Blazor's .NET handler. @onkeydown:preventDefault cannot do it: Blazor evaluates it at
        // render time, so it cannot know the upcoming key, lags a keystroke behind (the first arrow press
        // still scrolls) and a stale "true" swallows the Tab that follows, trapping the focus inside the
        // bar. A single capture-phase listener decides per key up front instead, and leaves everything
        // else - Tab, Enter, Space, typing - untouched.
        private static readonly NAV_KEYS = ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End'];

        private static installed = false;

        public static install() {
            if (NavBar.installed || typeof document === 'undefined') return;
            NavBar.installed = true;

            document.addEventListener('keydown', (e: KeyboardEvent) => {
                if (e.ctrlKey || e.altKey || e.metaKey) return;

                if (NavBar.NAV_KEYS.indexOf(e.key) < 0) return;

                const target = e.target as HTMLElement | null;
                if (!target?.closest) return;

                // An editable rendered by a custom item template owns its own keys: Home, End and the
                // arrows move the caret there rather than the focus along the bar.
                if (NavBar.isEditable(target)) return;

                // A disabled navbar handles no key at all, so nothing of the browser's own behavior is
                // taken away from it either.
                const item = target.closest('.bit-nbr-itm');
                if (!item || item.closest('.bit-nbr.bit-dis')) return;

                e.preventDefault();
            }, { capture: true });
        }

        private static isEditable(element: HTMLElement): boolean {
            if (element.isContentEditable) return true;

            const tag = element.tagName;
            return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
        }
    }

    NavBar.install();
}
