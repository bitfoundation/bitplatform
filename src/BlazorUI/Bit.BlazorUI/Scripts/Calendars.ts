namespace BitBlazorUI {
    export class Calendars {
        private static _handlers = new Map<string, { element: HTMLElement, handler: (e: KeyboardEvent) => void }>();

        private static _navKeys = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown'];

        // Attaches a keydown listener that only prevents the default behavior (page scrolling) of the
        // navigation keys pressed on the day buttons. The actual keyboard logic runs in the Blazor
        // keydown handlers, which cannot conditionally preventDefault per key.
        public static setup(id: string) {
            Calendars.dispose(id);

            const root = document.getElementById(id);
            if (!root) return;

            const handler = (e: KeyboardEvent) => {
                if (Calendars._navKeys.indexOf(e.key) === -1) return;

                const target = e.target as HTMLElement | null;
                if (!target || !target.closest('.bit-cal-dbt')) return;

                e.preventDefault();
            };
            root.addEventListener('keydown', handler);

            Calendars._handlers.set(id, { element: root, handler });
        }

        public static dispose(id: string) {
            const entry = Calendars._handlers.get(id);
            if (!entry) return;

            entry.element.removeEventListener('keydown', entry.handler);
            Calendars._handlers.delete(id);
        }

        public static focusDay(dayId: string) {
            document.getElementById(dayId)?.focus();
        }
    }
}
