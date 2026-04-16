namespace BitBlazorUI {
    export class TagsInput {
        public static setup(input: HTMLInputElement) {
            input.addEventListener('keydown', (e: KeyboardEvent) => {
                // Prevent Tab from moving focus when the input has uncommitted text
                if (e.key === 'Tab' && input.value.trim().length > 0) {
                    e.preventDefault();
                    return;
                }

                // Prevent single-char separator keys from being typed into the input
                const separatorsJson = input.dataset.separators;
                if (!separatorsJson) return;

                try {
                    const separators: string[] = JSON.parse(separatorsJson);
                    if (e.key.length === 1 && separators.includes(e.key)) {
                        e.preventDefault();
                    }
                } catch { }
            }, true); // capture phase — runs before the browser default and Blazor's handler
        }
    }
}
