namespace BitBlazorUI {
    export class TextField {
        private static _abortControllers: { [key: string]: AbortController } = {};
        private static _ghostTexts: { [key: string]: string } = {};
        private static _inputElements: { [key: string]: HTMLInputElement } = {};

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
            TextField._inputElements[id] = inputElement;
            const signal = ac.signal;

            const getOverlay = () => inputElement.parentElement?.querySelector<HTMLElement>('.bit-tfl-gho') ?? null;
            const hasGhost = () => (TextField._ghostTexts[id] ?? '').length > 0;

            const syncScroll = () => {
                const overlay = getOverlay();
                if (!overlay) return;
                overlay.scrollTop = inputElement.scrollTop;
                overlay.scrollLeft = inputElement.scrollLeft;
            };

            // Accept the stored ghost text at the current caret position.
            const acceptGhost = () => {
                const ghost = TextField._ghostTexts[id] ?? '';
                if (!ghost) return;

                const start = inputElement.selectionStart ?? inputElement.value.length;
                const end = inputElement.selectionEnd ?? start;

                inputElement.value =
                    inputElement.value.substring(0, start) +
                    ghost +
                    inputElement.value.substring(end);

                const newPos = start + ghost.length;
                inputElement.setSelectionRange(newPos, newPos);

                // Clear ghost immediately after acceptance.
                TextField._ghostTexts[id] = '';
                const overlay = getOverlay();
                if (overlay) overlay.textContent = inputElement.value;

                inputElement.dispatchEvent(new Event('input', { bubbles: true }));
                dotnetObj.invokeMethodAsync('OnGhostTextAccepted', ghost);
            };

            // On every keystroke: immediately clear the ghost suggestion (index3.html pattern).
            // The overlay is JS-owned; Blazor never touches its content.
            inputElement.addEventListener('input', () => {
                TextField._ghostTexts[id] = '';
                const overlay = getOverlay();
                if (overlay) overlay.textContent = inputElement.value;
                syncScroll();
            }, { signal });

            // Tab/Enter: accept the ghost suggestion.
            inputElement.addEventListener('keydown', e => {
                const isAcceptKey = e.key === 'Tab' || e.key === 'Enter';

                if (isAcceptKey && hasGhost()) {
                    e.preventDefault();
                    acceptGhost();
                    return;
                }

                if (!hasGhost()) return;

                // Clear immediately on any other key press so stale ghost text never
                // lingers until the later input event.
                TextField._ghostTexts[id] = '';
                const overlay = getOverlay();
                if (overlay) overlay.textContent = inputElement.value;
            }, { signal });

            // Click/touch accept: when there is a ghost suggestion and the caret is at
            // the end of the current value, treat click/touch as accepting the suggestion.
            const acceptGhostOnPointer = () => {
                if (!hasGhost()) return;

                const start = inputElement.selectionStart ?? inputElement.value.length;
                const end = inputElement.selectionEnd ?? start;
                const atEnd = start === inputElement.value.length && end === start;

                if (!atEnd) return;

                acceptGhost();
            };

            inputElement.addEventListener('click', acceptGhostOnPointer, { signal });
            inputElement.addEventListener('touchend', acceptGhostOnPointer, { signal });

            // Sync overlay scroll to input scroll (covers cursor navigation without input events).
            inputElement.addEventListener('scroll', syncScroll, { signal });
        }

        // Called by C# (OnAfterRenderAsync) whenever the GhostText parameter changes.
        // Stores the new ghost text and refreshes the overlay to show value + ghost.
        public static setGhostText(id: string, ghostText: string) {
            TextField._ghostTexts[id] = ghostText ?? '';
            const inputElement = TextField._inputElements[id];
            if (!inputElement) return;
            const overlay = inputElement.parentElement?.querySelector<HTMLElement>('.bit-tfl-gho');
            if (!overlay) return;
            overlay.textContent = inputElement.value + (ghostText ?? '');
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
            delete TextField._ghostTexts[id];
            delete TextField._inputElements[id];
        }
    }
}