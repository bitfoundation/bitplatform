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

        public static setupGhostText(id: string, inputElement: HTMLInputElement, dotnetObj: DotNetObject) {
            if (!inputElement) return;

            const ac = TextField._abortControllers[id] ?? new AbortController();
            TextField._abortControllers[id] = ac;
            const signal = ac.signal;

            // Shared helper: insert ghost text at the current caret/selection position,
            // dispatch the input event so Blazor updates the bound value, then notify .NET.
            const acceptGhost = () => {
                const wrapper = inputElement.parentElement;
                if (!wrapper) return;

                const ghostSpan = wrapper.querySelector<HTMLElement>('.bit-tfl-ghs');
                if (!ghostSpan || !ghostSpan.textContent) return;

                const ghostText = ghostSpan.textContent;
                const start = inputElement.selectionStart ?? inputElement.value.length;
                const end = inputElement.selectionEnd ?? start;

                inputElement.value =
                    inputElement.value.substring(0, start) +
                    ghostText +
                    inputElement.value.substring(end);

                const newPos = start + ghostText.length;
                inputElement.setSelectionRange(newPos, newPos);

                inputElement.dispatchEvent(new Event('input', { bubbles: true }));
                dotnetObj.invokeMethodAsync('OnGhostAccepted', ghostText);
            };

            // Tab key: accept ghost text and prevent focus navigation
            inputElement.addEventListener('keydown', e => {
                if (e.key !== 'Tab') return;

                const wrapper = inputElement.parentElement;
                if (!wrapper) return;

                const ghostSpan = wrapper.querySelector<HTMLElement>('.bit-tfl-ghs');
                if (!ghostSpan || !ghostSpan.textContent) return;

                e.preventDefault();
                acceptGhost();
            }, { signal });

            // Click/touch on the ghost span: accept at the current caret position,
            // consistent with the Tab key behavior. Listen on the wrapper so the
            // listener survives Blazor re-renders that swap out the ghost span.
            const wrapper = inputElement.parentElement;
            if (wrapper) {
                wrapper.addEventListener('click', e => {
                    if (!(e.target as HTMLElement).closest('.bit-tfl-ghs')) return;
                    acceptGhost();
                }, { signal });
            }

            const syncScroll = () => {
                const wrapper = inputElement.parentElement;
                if (!wrapper) return;

                const overlay = wrapper.querySelector<HTMLElement>('.bit-tfl-gho');
                if (!overlay) return;

                overlay.scrollTop = inputElement.scrollTop;
                overlay.scrollLeft = inputElement.scrollLeft;
            };

            // Update the transparent value span synchronously on every input event so
            // its width is always correct before we sync the scroll offset. This mirrors
            // the index3.html pattern where overlay content is owned by JS and kept in
            // sync immediately, avoiding the Blazor async re-render timing gap.
            inputElement.addEventListener('input', () => {
                const wrapper = inputElement.parentElement;
                if (!wrapper) return;

                const valueSpan = wrapper.querySelector<HTMLElement>('.bit-tfl-ghv');
                if (valueSpan) valueSpan.textContent = inputElement.value;

                syncScroll();
            }, { signal });

            // Also sync on the scroll event (covers programmatic scrolls and cursor
            // navigation that doesn't trigger an input event).
            inputElement.addEventListener('scroll', syncScroll, { signal });
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