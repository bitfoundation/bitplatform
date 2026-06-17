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

                // Tab: prevent focus loss when the input has uncommitted text
                if (e.key === 'Tab' && hasText) {
                    e.preventDefault();
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
