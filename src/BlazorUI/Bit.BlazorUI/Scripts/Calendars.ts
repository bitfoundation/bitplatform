namespace BitBlazorUI {
    export class Calendars {
        private static _handlers = new Map<string, AbortController>();

        private static _navKeys = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown'];

        // The hour and minute inputs of the time picker a calendar can carry.
        private static _timeInputs = '.bit-cal-tin, .bit-dtp-tin, .bit-dtrp-tin';

        // The field of a picker, which is the only text input of one: the callout of a date picker holds
        // nothing but buttons and the number inputs above.
        private static _fields = '.bit-dtp-inp, .bit-dtrp-inp';

        // Attaches the keydown listeners that prevent the default behavior (scrolling the page) of the keys
        // the calendar and the field of a picker are operated with - the actual keyboard logic runs in the
        // Blazor keydown handlers, which cannot conditionally preventDefault per key - and, for a calendar
        // that is a modal dialog, keeps Tab and Shift+Tab cycling inside it as the dialog pattern requires.
        // The id is the calendar itself: the root of a standalone one, the callout of a picker. A picker
        // passes the id of its field wrapper as well, since that part of it - the field and the clear
        // button on it - sits outside of the callout the rest of it is in.
        public static setup(id: string, trapFocus: boolean, componentId: string | null) {
            Calendars.dispose(id);

            const root = document.getElementById(id);
            if (!root) return;

            const controller = new AbortController();

            root.addEventListener('keydown', e => {
                if (trapFocus && e.key === 'Tab') {
                    Utils.wrapFocus(root, e);
                    return;
                }

                Calendars.preventScroll(e);
            }, { signal: controller.signal });

            document.getElementById(componentId ?? '')
                ?.addEventListener('keydown', e => Calendars.preventScroll(e), { signal: controller.signal });

            Calendars._handlers.set(id, controller);
        }

        public static dispose(id: string) {
            const controller = Calendars._handlers.get(id);
            if (!controller) return;

            controller.abort();
            Calendars._handlers.delete(id);
        }

        public static focusCell(cellId: string) {
            document.getElementById(cellId)?.focus();
        }

        // Which keys the browser has to be stopped from scrolling the page with depends on what holds the
        // focus, since the same key is typed with, navigated with or unused depending on where it lands.
        private static preventScroll(e: KeyboardEvent) {
            const target = e.target as HTMLElement | null;
            if (!target) return;

            // Every button of a picker - the day, month and year cells the grids navigate with the arrows,
            // the headers and the arrows that page them, today, clear, and the spin and meridiem buttons of
            // the time picker - has no use of its own for any of these keys, so scrolling the page is all
            // the browser would do with them. The space bar and Enter are deliberately not among them:
            // those are what presses a button.
            if (target.closest('button')) {
                if (Calendars._navKeys.indexOf(e.key) === -1) return;

                e.preventDefault();
                return;
            }

            const input = target.closest('input');
            if (!input) return;

            // The time inputs step themselves with the arrows and move their caret with Home and End, so
            // only the keys that do nothing there but scroll are stopped.
            if (input.matches(Calendars._timeInputs)) {
                if (e.key !== 'PageUp' && e.key !== 'PageDown' && e.key !== ' ') return;

                e.preventDefault();
                return;
            }

            // The field opens the callout with the very keys the browser scrolls the page with, so a key it
            // has just opened its callout with must not also scroll the page out from under it. The space
            // bar counts only while the field is read-only, which is exactly when nothing is typed with it:
            // an editable one types a space instead.
            if (!input.matches(Calendars._fields)) return;

            if (e.key !== 'ArrowUp' && e.key !== 'ArrowDown' && (e.key !== ' ' || input.readOnly === false)) return;

            e.preventDefault();
        }

    }
}
