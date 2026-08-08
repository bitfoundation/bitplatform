namespace BitBlazorUI {
    export class TextField {
        private static _abortControllers: { [key: string]: AbortController } = {};
        private static _ghostTexts: { [key: string]: string } = {};
        private static _maxRows: { [key: string]: number | null } = {};
        private static _inputElements: { [key: string]: HTMLInputElement } = {};

        public static setupMultilineInput(id: string, inputElement: HTMLInputElement, autoHeight: boolean, preventEnter: boolean, maxRows: number | null) {
            if (!inputElement) return;

            const ac = TextField._abortControllers[id] ?? new AbortController();
            TextField._abortControllers[id] = ac;
            TextField._maxRows[id] = maxRows ?? null;

            if (autoHeight) {
                inputElement.addEventListener('input', e => {
                    TextField.resize(inputElement, TextField._maxRows[id]);
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

        public static adjustHeight(id: string, inputElement: HTMLInputElement, maxRows: number | null) {
            if (!inputElement) return;

            TextField._maxRows[id] = maxRows ?? null;

            TextField.resize(inputElement, maxRows ?? null);
        }

        // Collapses the input first so scrollHeight reports the height the content actually needs, then
        // grows it back to that height. When a row ceiling is set the growth stops there and the content
        // scrolls inside the input instead of pushing the rest of the page down.
        private static resize(inputElement: HTMLInputElement, maxRows: number | null) {
            inputElement.style.height = 'auto';

            const contentHeight = inputElement.scrollHeight;

            if (!maxRows || maxRows <= 0) {
                inputElement.style.height = contentHeight + 'px';
                inputElement.style.overflowY = '';
                return;
            }

            const styles = getComputedStyle(inputElement);
            const fontSize = parseFloat(styles.fontSize) || 16;
            const lineHeight = parseFloat(styles.lineHeight) || (fontSize * 1.2);
            const paddings = (parseFloat(styles.paddingTop) || 0) + (parseFloat(styles.paddingBottom) || 0);
            const borders = styles.boxSizing === 'border-box'
                ? (parseFloat(styles.borderTopWidth) || 0) + (parseFloat(styles.borderBottomWidth) || 0)
                : 0;

            const maxHeight = (lineHeight * maxRows) + paddings + borders;

            if (contentHeight > maxHeight) {
                inputElement.style.height = maxHeight + 'px';
                inputElement.style.overflowY = 'auto';
            } else {
                inputElement.style.height = contentHeight + 'px';
                inputElement.style.overflowY = 'hidden';
            }
        }

        public static setupGhostText(id: string, inputElement: HTMLInputElement, dotnetObj: DotNetObject) {
            if (!inputElement) return;

            const ac = TextField._abortControllers[id] ?? new AbortController();
            TextField._abortControllers[id] = ac;
            TextField._inputElements[id] = inputElement;
            const signal = ac.signal;

            const getOverlay = () => inputElement.parentElement?.querySelector<HTMLElement>('.bit-tfl-gho') ?? null;
            const hasGhost = () => (TextField._ghostTexts[id] ?? '').length > 0;
            const getSelection = () => {
                try {
                    const start = inputElement.selectionStart;
                    const end = inputElement.selectionEnd;

                    if (typeof start === 'number' && typeof end === 'number') {
                        return { start, end, supportsSelection: true };
                    }
                } catch {
                    // Some input types (e.g. number) may throw when reading selection APIs.
                }

                const fallback = inputElement.value.length;
                return { start: fallback, end: fallback, supportsSelection: false };
            };

            const syncScroll = () => {
                const overlay = getOverlay();
                if (!overlay) return;
                overlay.scrollTop = inputElement.scrollTop;
                overlay.scrollLeft = inputElement.scrollLeft;
            };

            const clearGhost = () => {
                TextField._ghostTexts[id] = '';
                const overlay = getOverlay();
                if (overlay) overlay.textContent = inputElement.value;
                syncScroll();
            };

            // Accept the stored ghost text at the current caret position.
            const acceptGhost = () => {
                if (inputElement.readOnly || inputElement.disabled) return;

                const ghost = TextField._ghostTexts[id] ?? '';
                if (!ghost) return;

                const { start, end, supportsSelection } = getSelection();

                inputElement.value =
                    inputElement.value.substring(0, start) +
                    ghost +
                    inputElement.value.substring(end);

                const newPos = start + ghost.length;
                if (supportsSelection) {
                    try {
                        inputElement.setSelectionRange(newPos, newPos);
                    } catch {
                        // Ignore unsupported selection range operations.
                    }
                }

                // Clear ghost immediately after acceptance.
                clearGhost();

                // Both events are dispatched: the input one feeds an Immediate binding, and the change one
                // is what a plain (non-Immediate) binding listens to, so the accepted text reaches the
                // bound value either way instead of waiting for the input to lose focus.
                inputElement.dispatchEvent(new Event('input', { bubbles: true }));
                inputElement.dispatchEvent(new Event('change', { bubbles: true }));
                dotnetObj.invokeMethodAsync('OnGhostTextAccepted', ghost);
            };

            // On every keystroke: immediately clear the ghost suggestion.
            // The overlay is JS-owned; Blazor never touches its content.
            inputElement.addEventListener('input', clearGhost, { signal });

            // Tab/Enter: accept the ghost suggestion.
            inputElement.addEventListener('keydown', e => {
                const isAcceptKey = e.key === 'Tab' || e.key === 'Enter';

                if (isAcceptKey && hasGhost()) {
                    if (inputElement.readOnly || inputElement.disabled) return;

                    const { start, end } = getSelection();
                    const atEnd = start === inputElement.value.length && end === start;
                    if (!atEnd) return;

                    e.preventDefault();
                    e.stopPropagation();
                    acceptGhost();
                    return;
                }

                if (!hasGhost()) return;

                // A modifier on its own (Shift before a capital letter, Ctrl before a shortcut) does not
                // change the value, so the suggestion survives it instead of blinking out of existence.
                if (e.key === 'Shift' || e.key === 'Control' || e.key === 'Alt' || e.key === 'Meta') return;

                // Clear immediately on any other key press so stale ghost text never
                // lingers until the later input event.
                clearGhost();
            }, { signal });

            // Click/touch accept: when there is a ghost suggestion and the caret is at
            // the end of the current value, treat click/touch as accepting the suggestion.
            const acceptGhostOnPointer = () => {
                if (inputElement.readOnly || inputElement.disabled) return;

                if (!hasGhost()) return;

                const { start, end } = getSelection();
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
            overlay.scrollTop = inputElement.scrollTop;
            overlay.scrollLeft = inputElement.scrollLeft;
        }

        public static dispose(id: string) {
            const ac = TextField._abortControllers[id];

            if (ac) {
                ac.abort();
            }

            delete TextField._abortControllers[id];
            delete TextField._ghostTexts[id];
            delete TextField._maxRows[id];
            delete TextField._inputElements[id];
        }
    }
}
