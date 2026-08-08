namespace BitBlazorUI {
    export class TextField {
        // The three concerns of a text field (the multiline behaviors, the ghost text and the IME guard)
        // are wired independently and can be turned on and off at any point in the life of a component,
        // so each of them owns its AbortController instead of sharing a single one.
        private static _abortControllers: { [key: string]: { [feature: string]: AbortController } } = {};
        private static _ghostTexts: { [key: string]: string } = {};
        private static _maxRows: { [key: string]: number | null } = {};
        private static _inputElements: { [key: string]: HTMLInputElement } = {};

        private static getSignal(id: string, feature: string): AbortSignal {
            const features = TextField._abortControllers[id] ?? (TextField._abortControllers[id] = {});

            // Setting a feature up again replaces its listeners rather than adding a second copy of them,
            // which is what makes every setup call below safe to repeat.
            features[feature]?.abort();

            const ac = new AbortController();
            features[feature] = ac;

            return ac.signal;
        }

        public static setupMultilineInput(id: string, inputElement: HTMLInputElement, autoHeight: boolean, preventEnter: boolean, maxRows: number | null) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'multiline');
            TextField._maxRows[id] = maxRows ?? null;

            if (autoHeight) {
                inputElement.addEventListener('input', e => {
                    TextField.resize(inputElement, TextField._maxRows[id]);
                }, { signal });
            } else {
                // The heights the auto growing left behind are inline styles, so they would keep the
                // element at the size of its content long after the feature was turned off. Clearing them
                // hands the sizing back to the Rows attribute and to the stylesheet.
                inputElement.style.height = '';
                inputElement.style.overflowY = '';
            }

            if (preventEnter) {
                inputElement.addEventListener('keydown', e => {
                    // Enter commits the candidate of an input method editor, so it must keep its meaning
                    // while a composition session is running. The keyCode check covers the browsers that
                    // report the legacy 229 instead of setting isComposing.
                    if (e.isComposing || e.keyCode === 229) return;

                    if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                    }
                }, { signal });
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

        // Keeps an input method editor (Pinyin, Kana, Hangul, ...) from leaking into the component while
        // it is composing. Blazor listens for the events on the document, so stopping them here - on the
        // element itself, before they bubble - is what hides the composition session from it.
        public static setupComposition(id: string, inputElement: HTMLInputElement) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'composition');

            let isComposing = false;

            const composing = (e: KeyboardEvent | InputEvent) =>
                isComposing || e.isComposing || (e as KeyboardEvent).keyCode === 229;

            inputElement.addEventListener('compositionstart', () => {
                isComposing = true;
            }, { signal });

            inputElement.addEventListener('compositionend', () => {
                isComposing = false;

                // Whether the last input event of a session arrives before or after compositionend is not
                // the same in every engine, so one is dispatched here to make sure the committed text
                // always reaches the binding. A repeated value is a no-op on the C# side.
                inputElement.dispatchEvent(new Event('input', { bubbles: true }));
            }, { signal });

            // The half-composed text never reaches the bound value; the committed text is handed over in
            // one piece when the session ends.
            inputElement.addEventListener('input', e => {
                if (composing(e as InputEvent)) {
                    e.stopPropagation();
                }
            }, { signal });

            // Enter commits the candidate of the editor and Escape cancels it, so neither of them means
            // what it usually does while a session is running. Most engines report those keydowns as the
            // "Process" key, which no handler is looking for anyway, but the ones that report the real
            // key would otherwise submit a form or close a dialog in the middle of typing a word.
            inputElement.addEventListener('keydown', e => {
                if (!composing(e)) return;

                if (e.key === 'Enter' || e.key === 'Escape') {
                    e.stopPropagation();
                }
            }, { signal });
        }

        // Selecting the value from the focus handler alone is not enough: the click that gave the input
        // its focus ends with a mouseup that puts the caret where it landed and drops the selection
        // again, so that one mouseup is swallowed.
        public static setupSelectOnFocus(id: string, inputElement: HTMLInputElement) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'selectOnFocus');

            let justFocused = false;

            inputElement.addEventListener('focus', () => {
                justFocused = true;

                try {
                    inputElement.select();
                } catch {
                    // Selection APIs are not available on every input type.
                }
            }, { signal });

            inputElement.addEventListener('mouseup', e => {
                if (!justFocused) return;

                justFocused = false;
                e.preventDefault();
            }, { signal });

            inputElement.addEventListener('blur', () => {
                justFocused = false;
            }, { signal });
        }

        public static setupGhostText(id: string, inputElement: HTMLInputElement, dotnetObj: DotNetObject) {
            if (!inputElement) return;

            const signal = TextField.getSignal(id, 'ghost');
            TextField._inputElements[id] = inputElement;

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
                // Tab and Enter belong to the input method editor while a composition session is running:
                // they move through and commit its candidates, so the suggestion is left alone.
                if (e.isComposing || e.keyCode === 229) return;

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

        // Moves the caret, or selects a range of the text, of the input element. A null start selects
        // from the beginning and a null end selects to the very end of the value.
        public static setSelectionRange(inputElement: HTMLInputElement, start: number | null, end: number | null) {
            if (!inputElement) return;

            try {
                const length = inputElement.value.length;
                const from = Math.max(0, Math.min(start ?? 0, length));
                const to = Math.max(from, Math.min(end ?? length, length));

                inputElement.setSelectionRange(from, to);
            } catch (e) {
                // Selection APIs are not available on every input type (a number or an email input
                // throws), which is not an error worth breaking the caller over.
            }
        }

        // Tears a single feature down, which is what happens when a text field stops being multiline or
        // stops being immediate while the rest of it stays alive.
        public static disposeFeature(id: string, feature: string) {
            const features = TextField._abortControllers[id];
            if (!features) return;

            features[feature]?.abort();
            delete features[feature];
        }

        public static dispose(id: string) {
            const features = TextField._abortControllers[id];

            if (features) {
                Object.keys(features).forEach(feature => features[feature].abort());
            }

            delete TextField._abortControllers[id];
            delete TextField._ghostTexts[id];
            delete TextField._maxRows[id];
            delete TextField._inputElements[id];
        }
    }
}
