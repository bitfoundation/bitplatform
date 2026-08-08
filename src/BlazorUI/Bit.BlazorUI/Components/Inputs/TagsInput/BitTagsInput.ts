namespace BitBlazorUI {
    export class TagsInput {
        public static setup(input: HTMLInputElement) {
            input.addEventListener('keydown', (e: KeyboardEvent) => {
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
                const separatorsJson = input.dataset.separators;
                if (!separatorsJson) return;

                try {
                    const separators: string[] = JSON.parse(separatorsJson);
                    if (e.key.length === 1 && separators.includes(e.key)) {
                        e.preventDefault();
                    }
                } catch { }
            }, true); // capture phase - runs before the browser default and Blazor's handler
        }
    }
}
