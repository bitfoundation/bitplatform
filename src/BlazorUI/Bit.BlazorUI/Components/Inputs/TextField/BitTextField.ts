namespace BitBlazorUI {
    export class TextField {
        private static _abortControllers: { [key: string]: AbortController } = {};

        public static setupMultilineInput(id: string, inputElement: HTMLInputElement, autoHeight: boolean, preventEnter: boolean) {
            if (!inputElement) return;

            const ac = TextField._abortControllers[id] ?? new AbortController();
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

        public static setupGhostText(id: string, inputElement: HTMLInputElement) {
            if (!inputElement) return;

            const ac = TextField._abortControllers[id] ?? new AbortController();
            TextField._abortControllers[id] = ac;
            const signal = ac.signal;

            // Tab key: accept ghost text and prevent focus navigation
            inputElement.addEventListener('keydown', e => {
                if (e.key !== 'Tab') return;

                const wrapper = inputElement.parentElement;
                if (!wrapper) return;

                const ghostSpan = wrapper.querySelector<HTMLElement>('.bit-tfl-ghs');
                if (!ghostSpan || !ghostSpan.textContent) return;

                e.preventDefault();

                const ghostText = ghostSpan.textContent;
                const start = inputElement.selectionStart ?? inputElement.value.length;
                const end = inputElement.selectionEnd ?? start;

                inputElement.value =
                    inputElement.value.substring(0, start) +
                    ghostText +
                    inputElement.value.substring(end);

                const newPos = start + ghostText.length;
                inputElement.setSelectionRange(newPos, newPos);

                // Notify Blazor's @oninput handler to update the bound value
                inputElement.dispatchEvent(new Event('input', { bubbles: true }));
            }, { signal });

            // Scroll sync: mirror input scroll position to the overlay
            inputElement.addEventListener('scroll', () => {
                const wrapper = inputElement.parentElement;
                if (!wrapper) return;

                const overlay = wrapper.querySelector<HTMLElement>('.bit-tfl-gho');
                if (!overlay) return;

                overlay.scrollTop = inputElement.scrollTop;
                overlay.scrollLeft = inputElement.scrollLeft;
            }, { signal });
        }

        public static scrollToEnd(inputElement: HTMLInputElement) {
            if (!inputElement) return;

            const len = inputElement.value.length;
            inputElement.focus();
            inputElement.setSelectionRange(len, len);

            if (inputElement.tagName.toLowerCase() === 'textarea') {
                inputElement.scrollTo(0, inputElement.scrollHeight);
            } else {
                inputElement.scrollTo(inputElement.scrollWidth, 0);
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