namespace BitBlazorUI {
    export class Ratings {
        private static _handlers = new Map<string, { element: HTMLElement, handler: (e: KeyboardEvent) => void }>();

        private static _navKeys = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown'];

        // Attaches a keydown listener that only prevents the default behavior (page scrolling) of the
        // navigation keys pressed on the rating items. The actual keyboard logic runs in the Blazor
        // keydown handler, which cannot conditionally preventDefault per key: its flag is applied by
        // the next render, so the first press of a key would scroll the page anyway. Kept key-scoped
        // so Tab, Space and Enter still behave normally.
        public static setup(id: string) {
            Ratings.dispose(id);

            const root = document.getElementById(id);
            if (!root) return;

            const handler = (e: KeyboardEvent) => {
                if (Ratings._navKeys.indexOf(e.key) === -1) return;

                const target = e.target as HTMLElement | null;
                if (!target || !target.closest('.bit-rtg-btn')) return;

                e.preventDefault();
            };
            root.addEventListener('keydown', handler);

            Ratings._handlers.set(id, { element: root, handler });
        }

        public static dispose(id: string) {
            const entry = Ratings._handlers.get(id);
            if (!entry) return;

            entry.element.removeEventListener('keydown', entry.handler);
            Ratings._handlers.delete(id);
        }
    }
}
