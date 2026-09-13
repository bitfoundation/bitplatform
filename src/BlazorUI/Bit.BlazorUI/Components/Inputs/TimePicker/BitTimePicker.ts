namespace BitBlazorUI {
    export class TimePicker {
        private static _bitControllers: BitController[] = [];

        // The keys a focused button leaves to the browser, which scrolls the page with every one of them.
        // The space bar and Enter are deliberately not among them: those are what presses a button.
        private static readonly _buttonKeys =
            ['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'PageUp', 'PageDown', 'Home', 'End'];

        // What the hour, minute and second inputs take on top of what a number input does by itself: the four
        // keys the picker steps and jumps the value with, plus the space bar, which types nothing into a
        // number field and would only scroll the page behind the callout. The arrows are left out - a number
        // input steps itself with them, which is exactly what the picker wants them to do.
        private static readonly _timeInputKeys = ['PageUp', 'PageDown', 'Home', 'End', ' '];

        public static setup(callout: HTMLElement, input: HTMLInputElement | null, trapFocus: boolean): string {
            const bitController = new BitController();

            // Blazor cannot preventDefault per key, so the keys the picker acts on are stopped here instead -
            // otherwise every one of them would scroll the page out from under the callout as well as do what
            // it was pressed for. Tab, Escape and Enter are deliberately left alone, so focus can still leave
            // the picker and the dialog keys keep working. The listener goes on the callout rather than on the
            // root: everything the picker is operated with sits inside of it, standalone as well as in a
            // popup, and it stays attached while the callout is moved out to the body to be shown.
            callout.addEventListener('keydown', e => {
                const event = e as KeyboardEvent;
                const target = event.target as HTMLElement | null;
                if (target === null) return;

                // A callout that floats over the page reports itself a modal dialog, which the tab order has to
                // honor: without this the focus would walk out of the popup and onto the page behind it, where
                // the overlay swallows every click that could bring it back. Standalone there is no dialog and
                // no overlay, so the focus is free to leave the way it would leave any other part of the page.
                if (trapFocus && event.key === 'Tab') {
                    Utils.wrapFocus(callout, event);
                    return;
                }

                if (target.closest('button') !== null) {
                    if (TimePicker._buttonKeys.indexOf(event.key) < 0) return;

                    event.preventDefault();
                    return;
                }

                if (target.closest('.bit-tpc-tin') !== null) {
                    if (TimePicker._timeInputKeys.indexOf(event.key) < 0) return;

                    event.preventDefault();
                }
            }, { signal: bitController.controller.signal });

            // The field works the callout with the very keys the browser scrolls the page with, so their
            // defaults are stopped here too - a key the picker has just opened its callout with must not also
            // scroll the page out from under it. The space bar only counts while the field is read-only, which
            // is exactly when nothing is typed with it: an editable one types a space instead.
            input?.addEventListener('keydown', e => {
                const key = (e as KeyboardEvent).key;

                if (key !== 'ArrowUp' && key !== 'ArrowDown' && (key !== ' ' || input.readOnly === false)) return;

                e.preventDefault();
            }, { signal: bitController.controller.signal });

            TimePicker._bitControllers.push(bitController);

            return bitController.id;
        }

        public static dispose(id: string): void {
            const bitController = TimePicker._bitControllers.find(bc => bc.id == id);
            bitController?.controller.abort();

            TimePicker._bitControllers = TimePicker._bitControllers.filter(bc => bc.id != id);
        }
    }
}
