namespace BitBlazorUI {
    export class Calendars {
        private static _handlers = new Map<string, AbortController>();

        // The time input a press of the pointer is giving the focus to, until the press is released.
        private static _pressedTimeInput: HTMLInputElement | null = null;

        private static _navKeys = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Home', 'End', 'PageUp', 'PageDown'];

        // The hour and minute inputs of the time picker a calendar can carry.
        private static _timeInputs = '.bit-cal-tin, .bit-dtp-tin, .bit-dtrp-tin';

        // The field of a picker, which is the only text input of one: the callout of a date picker holds
        // nothing but buttons and the number inputs above.
        private static _fields = '.bit-dtp-inp, .bit-dtrp-inp';

        // Attaches the keydown listeners that prevent the default behavior (scrolling the page) of the keys
        // the calendar and the field of a picker are operated with - the actual keyboard logic runs in the
        // Blazor keydown handlers, which cannot conditionally preventDefault per key. The tab order of a
        // picker's callout is held by the callout itself while it floats as a modal dialog (see the trapFocus
        // of Callouts.toggle), so it is registered and taken back with every open and close.
        // The id is the calendar itself: the root of a standalone one, the callout of a picker. A picker
        // passes the id of its field wrapper as well, since that part of it - the field and the clear
        // button on it - sits outside of the callout the rest of it is in.
        public static setup(id: string, componentId: string | null) {
            Calendars.dispose(id);

            const root = document.getElementById(id);
            if (!root) return;

            const controller = new AbortController();

            root.addEventListener('keydown', e => Calendars.preventScroll(e), { signal: controller.signal });

            // A time input takes the focus with its text selected, so the first digit typed into it replaces the part
            // instead of being appended to it. It is selected here, as the focus lands, rather than from .NET a round
            // trip later: by then the focus may have moved on - a quick Tab or Shift+Tab - and select() pulls it back
            // onto the input in Chromium and WebKit. A read-only input has nothing to replace, so it is left alone.
            root.addEventListener('focusin', e => Calendars.selectTimeInput(e), { signal: controller.signal });

            // A press of the pointer that gives a time input the focus goes on to collapse that selection to a caret at
            // the press point in WebKit, so the release of the press selects the text once more.
            root.addEventListener('mousedown', e => Calendars.notePressedTimeInput(e), { signal: controller.signal });
            root.addEventListener('mouseup', e => Calendars.reselectPressedTimeInput(e), { signal: controller.signal });

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

        public static focusCell(cellId: string, preventScroll?: boolean) {
            document.getElementById(cellId)?.focus({ preventScroll: !!preventScroll });
        }

        private static selectTimeInput(e: FocusEvent) {
            const input = Calendars.timeInputOf(e.target);
            if (input) Calendars.selectText(input);
        }

        // Only a press that moves the focus onto the input counts: one in an input that already has it places the
        // caret, as a press in any input does.
        private static notePressedTimeInput(e: MouseEvent) {
            const input = e.button === 0 ? Calendars.timeInputOf(e.target) : null;
            Calendars._pressedTimeInput = input && input !== document.activeElement ? input : null;
        }

        private static reselectPressedTimeInput(e: MouseEvent) {
            const input = Calendars._pressedTimeInput;
            Calendars._pressedTimeInput = null;

            // A press released outside the input, or one that left it without the focus, is not a click into it.
            if (!input || e.target !== input || input !== document.activeElement) return;

            // The release's default is prevented too, for an engine that collapses the selection on the release itself.
            e.preventDefault();
            Calendars.selectText(input);
        }

        // The hour or minute input of a calendar's time picker, unless it is read-only or disabled.
        private static timeInputOf(target: EventTarget | null) {
            const input = target as HTMLInputElement | null;
            if (!input || input.tagName !== 'INPUT' || !input.matches(Calendars._timeInputs)) return null;

            return input.readOnly || input.disabled ? null : input;
        }

        private static selectText(input: HTMLInputElement) {
            try {
                input.select();
            } catch { } // an input that takes no selection keeps what it has
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
            // bar and Enter count only while the field is read-only, which is exactly when nothing is typed
            // with them: there they open the callout of a closed field instead of scrolling the page or
            // submitting the form around it. An editable field types a space and submits with Enter.
            if (!input.matches(Calendars._fields)) return;

            if (e.key === 'ArrowUp' || e.key === 'ArrowDown') {
                e.preventDefault();
                return;
            }

            if (!input.readOnly) return;

            if (e.key === ' ' || (e.key === 'Enter' && input.getAttribute('aria-expanded') !== 'true')) {
                e.preventDefault();
            }
        }

    }
}

// bit-lazy: teardown Calendars.dispose
