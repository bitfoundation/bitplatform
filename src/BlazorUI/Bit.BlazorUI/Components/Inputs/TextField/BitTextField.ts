namespace BitBlazorUI {
    export class TextField {
        private static _abortControllers: { [key: string]: AbortController } = {};

        public static setupMultilineInput(id: string, inputElement: HTMLInputElement, autoHeight: boolean, preventEnter: boolean) {
            if (!inputElement) return;

            const ac = new AbortController();
            TextField._abortControllers[id] = ac;

            if (autoHeight) {
                inputElement.addEventListener('input', e => {
                    TextField.adjustHeight(inputElement);
                }, { signal: ac.signal });

                //const observer = new MutationObserver((mutations) => {
                //    mutations.forEach((mutation) => {
                //        console.log("Value changed programmatically:", inputElement.value, mutation);
                //    });
                //});
                //observer.observe(inputElement, { attributes: true, subtree: true, attributeOldValue: true, attributeFilter: ['value'] });

                //Object.defineProperty(inputElement, "value", {
                //    set(newValue) {
                //        console.log("Value changed programmatically:", newValue);
                //        this.setAttribute("value", newValue); // Update the DOM attribute
                //    },
                //});

            }

            if (preventEnter) {
                inputElement.addEventListener('keydown', e => {
                    if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                    }
                }, { signal: ac.signal });
            }
        }

        public static adjustHeight(inputElement: HTMLInputElement) {
            if (!inputElement) return;
            
            inputElement.style.height = 'auto';
            inputElement.style.height = inputElement.scrollHeight + 'px';
        }

        public static dispose(id: string) {
            const ac = TextField._abortControllers[id];
            if (!ac) return;

            ac.abort();

            delete TextField._abortControllers[id];
        }
    }
}