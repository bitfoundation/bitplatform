namespace BitBlazorUI {
    export class TextField {
        private static _abortControllers: { [key: string]: AbortController } = {};

        public static setupMultilineInput(id: string, inputElement: HTMLInputElement, autoHeight: boolean, preventEnter: boolean) {
            const ac = new AbortController();
            TextField._abortControllers[id] = ac;

            if (autoHeight) {
                inputElement.addEventListener('input', e => {
                    inputElement.style.height = 'auto';
                    inputElement.style.height = inputElement.scrollHeight + 'px';
                }, { signal: ac.signal });
            }

            if (preventEnter) {
                inputElement.addEventListener('keydown', e => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                    }
                }, { signal: ac.signal });
            }
        }

        public static dispose(id: string) {
            const ac = TextField._abortControllers[id];
            if (!ac) return;

            ac.abort();

            delete TextField._abortControllers[id];
        }
    }
}