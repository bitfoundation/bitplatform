namespace BitBlazorUI {
    export class TagsInput {
        // The keys the tag elements answer to that would otherwise carry a browser default with them:
        // Home and End scroll the page, the arrow keys scroll it sideways, Alt with an arrow is the
        // back and forward navigation of the browser (which is what the reordering gesture would
        // otherwise leave the page over), and Backspace is the back navigation of the older engines.
        // Only the keys the component actually acts on are listed: holding back a key it does nothing
        // with would take a scroll away from the user and give nothing back.
        private static readonly tagKeys = ['ArrowLeft', 'ArrowRight', 'Home', 'End', 'Backspace', 'Delete', 'Enter', 'F2'];

        public static setup(input: HTMLInputElement, isEdit: boolean = false) {
            // The little input that replaces a tag while it is being corrected in place is set up every
            // time an edit is opened, and the engine may well hand the very same element back for the
            // next one, so the listeners are only ever attached once per element.
            if (input.dataset.bitTgiSetup) return;
            input.dataset.bitTgiSetup = 'true';

            input.addEventListener('keydown', (e: KeyboardEvent) => {
                // While an IME composition is open (Chinese, Japanese, Korean and every other input
                // method that builds a character out of several keystrokes), the Enter that picks a
                // candidate out of the suggestion window is not a confirmation of a tag, and neither is
                // the separator typed into it. The event is kept from reaching Blazor's delegated
                // listener on the document altogether, so nothing is committed half way through a word.
                // Engines that predate isComposing report the composition through the 229 key code.
                if (e.isComposing || e.keyCode === 229) {
                    e.stopPropagation();
                    return;
                }

                // A field that accepts nothing has no key to hold back: preventing the Enter of a
                // read-only tags input would only keep the form around it from being submitted.
                if (input.readOnly || input.disabled) return;

                // Enter inside the inline edit confirms the correction, so it must not reach the form
                // around the field either - and there is nothing else for it to do there.
                if (isEdit) {
                    if (e.key === 'Enter') {
                        e.preventDefault();
                    }
                    return;
                }

                const noTrim = input.dataset.noTrim === 'true';
                const hasText = (noTrim ? input.value : input.value.trim()).length > 0;

                // Enter: prevent default (form submit / browser action) unless input is empty
                // and CancelConfirmKeysOnEmpty is enabled.
                if (e.key === 'Enter') {
                    const cancelOnEmpty = input.dataset.cancelConfirmKeysOnEmpty === 'true';
                    if (hasText || !cancelOnEmpty) {
                        e.preventDefault();
                    }
                    return;
                }

                // Tab: keep the focus in the input so that the uncommitted text becomes a tag instead of
                // being left behind. Shift+Tab is never held back, and neither is Tab when the component
                // was asked not to commit on it: a field the keyboard cannot leave is a focus trap.
                if (e.key === 'Tab') {
                    const noAddOnTab = input.dataset.noAddOnTab === 'true';
                    if (hasText && !e.shiftKey && !noAddOnTab) {
                        e.preventDefault();
                    }
                    return;
                }

                // Single-char separator keys: prevent the character from being typed
                const separators = TagsInput.getSeparators(input);
                if (e.key.length === 1 && separators.includes(e.key)) {
                    e.preventDefault();
                }
            }, true); // capture phase - runs before the browser default and Blazor's handler

            if (isEdit) return;

            input.addEventListener('paste', (e: ClipboardEvent) => {
                // A single line input drops the line breaks out of whatever is pasted into it, so a
                // column copied out of a spreadsheet would otherwise arrive as one run-on tag. The
                // breaks are turned into the first separator before the text lands, which is what the
                // splitting on the .NET side then reads as a list. Without a separator there is nothing
                // to express a list with, so the paste is left to the browser.
                if (input.readOnly || input.disabled) return;

                const separators = TagsInput.getSeparators(input);
                if (separators.length === 0) return;

                const text = e.clipboardData?.getData('text');
                if (!text || !/[\r\n]/.test(text)) return;

                e.preventDefault();

                const value = text.replace(/(\r\n|[\r\n])+/g, separators[0]);

                // insertText keeps the caret, the selection it replaces and the undo stack of the field
                // intact, and raises the input event that the component listens to. It is a deprecated
                // command that an engine may refuse by returning false or by throwing outright, and both
                // of them mean the same thing here: the text has to be put in by hand, or the paste that
                // was just prevented would be lost altogether.
                let inserted = false;

                try {
                    inserted = document.execCommand('insertText', false, value);
                } catch {
                    inserted = false;
                }

                if (!inserted) {
                    const start = input.selectionStart ?? input.value.length;
                    const end = input.selectionEnd ?? input.value.length;

                    input.value = input.value.slice(0, start) + value + input.value.slice(end);
                    input.selectionStart = input.selectionEnd = start + value.length;

                    input.dispatchEvent(new Event('input', { bubbles: true }));
                }
            });
        }

        // The keys the chips answer to are handled on the .NET side, which runs long after the browser
        // has already acted on them: the page would be scrolled by Home, and the whole document left
        // behind by Alt with an arrow, before the tag ever moved. A single listener on the root - rather
        // than a handler per chip - holds those defaults back for the events that started on a chip,
        // and does the two things a drag needs that the .NET side cannot reach.
        public static setupTags(root: HTMLElement) {
            if (root.dataset.bitTgiTagsSetup) return;
            root.dataset.bitTgiTagsSetup = 'true';

            root.addEventListener('keydown', (e: KeyboardEvent) => {
                if (e.isComposing || e.keyCode === 229) return;

                const target = e.target as HTMLElement;

                // Only the chip itself: the little input that replaces it while it is being corrected in
                // place needs every one of those keys to do exactly what it does in a line of text.
                if (!target || !target.classList || !target.classList.contains('bit-tgi-tag')) return;

                if (TagsInput.tagKeys.includes(e.key)) {
                    e.preventDefault();
                }
            }, true);

            let dragging = false;

            root.addEventListener('dragstart', (e: DragEvent) => {
                if (!TagsInput.closestTag(e.target)) return;

                dragging = true;

                // Firefox refuses to start a drag whose data transfer holds nothing at all, so the
                // reordering would simply never begin there. The tag itself is what is written, which
                // also makes a chip dragged out of the field paste as its own text.
                const data = e.dataTransfer;
                if (!data) return;

                try {
                    data.setData('text/plain', (e.target as HTMLElement).innerText ?? '');
                    data.effectAllowed = 'move';
                } catch { }
            }, true);

            root.addEventListener('dragover', (e: DragEvent) => {
                // Only a chip of this very field is being carried: anything else dragged over the field
                // (a file, a selection out of another page) is none of the reordering's business.
                if (!dragging || !e.dataTransfer) return;

                if (!TagsInput.closestTag(e.target)) return;

                // Without it the pointer keeps showing the copy cursor over a drag that moves a chip.
                e.dataTransfer.dropEffect = 'move';
            }, true);

            root.addEventListener('dragend', () => { dragging = false; }, true);
            root.addEventListener('drop', () => { dragging = false; }, true);
        }

        private static closestTag(target: EventTarget | null): HTMLElement | null {
            const element = target as HTMLElement;

            return element && element.closest ? element.closest('.bit-tgi-tag') : null;
        }

        private static getSeparators(input: HTMLInputElement): string[] {
            const json = input.dataset.separators;
            if (!json) return [];

            try {
                const separators = JSON.parse(json);
                return Array.isArray(separators) ? separators : [];
            } catch {
                return [];
            }
        }
    }
}
