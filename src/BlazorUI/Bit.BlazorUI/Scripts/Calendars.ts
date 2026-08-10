namespace BitBlazorUI {
    export class Calendars {
        private static _handlers = new Map<string, { element: HTMLElement, handler: (e: KeyboardEvent) => void }>();

        private static _navKeys = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown'];

        // The cells of every calendar grid that navigates with the keyboard: the day buttons of the
        // standalone BitCalendar and of the day picker inside the callout of a BitDatePicker, plus the
        // month and year buttons of that same callout.
        private static _cells = '.bit-cal-dbt, .bit-dtp-dbt, .bit-dtp-pkb';

        // Everything that can hold the focus inside a calendar. The roving tabindex of the grids takes
        // every cell but one out of the tab sequence, which is why tabindex="-1" is excluded here.
        private static _focusables = 'button:not([disabled]), input:not([disabled]), [tabindex]:not([tabindex="-1"])';

        // Attaches a keydown listener that prevents the default behavior (page scrolling) of the
        // navigation keys pressed on the grid cells - the actual keyboard logic runs in the Blazor
        // keydown handlers, which cannot conditionally preventDefault per key - and, for a calendar that
        // is a modal dialog, keeps Tab and Shift+Tab cycling inside it as the dialog pattern requires.
        public static setup(id: string, trapFocus: boolean) {
            Calendars.dispose(id);

            const root = document.getElementById(id);
            if (!root) return;

            const handler = (e: KeyboardEvent) => {
                if (trapFocus && e.key === 'Tab') {
                    Calendars.wrapFocus(root, e);
                    return;
                }

                if (Calendars._navKeys.indexOf(e.key) === -1) return;

                const target = e.target as HTMLElement | null;
                if (!target || !target.closest(Calendars._cells)) return;

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

        public static focusCell(cellId: string) {
            document.getElementById(cellId)?.focus();
        }

        private static wrapFocus(root: HTMLElement, e: KeyboardEvent) {
            const focusables = Array.from(root.querySelectorAll<HTMLElement>(Calendars._focusables))
                .filter(el => el.offsetWidth > 0 || el.offsetHeight > 0 || el.getClientRects().length > 0);

            if (focusables.length === 0) return;

            const first = focusables[0];
            const last = focusables[focusables.length - 1];
            const active = document.activeElement;

            if (e.shiftKey && active === first) {
                last.focus();
                e.preventDefault();
            } else if (!e.shiftKey && active === last) {
                first.focus();
                e.preventDefault();
            }
        }
    }
}
