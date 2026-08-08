namespace BitBlazorUI {
    export class TagsInput {
        public static setup(input: HTMLInputElement) {
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

                const hasText = input.value.trim().length > 0;

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

            input.addEventListener('paste', (e: ClipboardEvent) => {
                // A single line input drops the line breaks out of whatever is pasted into it, so a
                // column copied out of a spreadsheet would otherwise arrive as one run-on tag. The
                // breaks are turned into the first separator before the text lands, which is what the
                // splitting on the .NET side then reads as a list. Without a separator there is nothing
                // to express a list with, so the paste is left to the browser.
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
