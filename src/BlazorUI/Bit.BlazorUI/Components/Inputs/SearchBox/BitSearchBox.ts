namespace BitBlazorUI {
    export class SearchBox {
        // Keys that drive the suggest list and therefore must lose their default behavior
        // (moving the caret, scrolling the page) while the list is open.
        private static readonly navigationKeys = ['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown'];

        // Keys that only take over while an item is virtually focused, so that they keep working
        // as text editing keys (and as a form submit) the rest of the time.
        private static readonly selectionKeys = ['Home', 'End', 'Enter'];

        // Every key the component acts on. While an input method editor is composing, all of them
        // belong to its candidate window instead (enter commits the composition, the arrows walk
        // the candidates), so none of them may reach the component.
        private static readonly imeKeys = ['ArrowUp', 'ArrowDown', 'PageUp', 'PageDown', 'Home', 'End', 'Enter', 'Escape'];

        // The state is read back from the rendered aria attributes of the input itself, so the
        // guard never goes out of sync with what the component is actually showing.
        public static setupInput(input: HTMLInputElement) {
            if (!input || (input as any).__bitSrbKeyDown) return;

            let isComposing = false;
            input.addEventListener('compositionstart', () => isComposing = true);
            input.addEventListener('compositionend', () => isComposing = false);

            const handler = (e: KeyboardEvent) => {
                // Safari fires compositionend before the keydown that ends the composition, so the
                // flag alone is not enough; isComposing covers it and the legacy 229 key code covers
                // the browsers that do not set isComposing at all. Blazor listens for the keydown on
                // the document, so stopping the propagation here is what keeps the component from
                // reacting to a key that the IME is still using.
                if (isComposing || e.isComposing || e.keyCode === 229) {
                    if (SearchBox.imeKeys.indexOf(e.key) > -1) {
                        e.stopPropagation();
                    }
                    return;
                }

                if (e.defaultPrevented || e.altKey || e.ctrlKey || e.metaKey) return;
                if (input.getAttribute('aria-expanded') !== 'true') return;

                if (SearchBox.navigationKeys.indexOf(e.key) > -1 ||
                    (input.hasAttribute('aria-activedescendant') && SearchBox.selectionKeys.indexOf(e.key) > -1)) {
                    e.preventDefault();
                }
            };

            input.addEventListener('keydown', handler);
            (input as any).__bitSrbKeyDown = handler;
        }

        // The shortcut that puts the focus into a search box from anywhere on the page, keyed by the id of
        // the input so a page full of search boxes can each keep their own, and so the listener can be taken
        // off the document again when the component goes away.
        private static shortcuts: { [inputId: string]: (e: KeyboardEvent) => void } = {};

        // The value is written in the syntax of aria-keyshortcuts - a space separated list of combinations,
        // each one modifiers and a key joined by '+' - so the very same string the input advertises to
        // assistive technologies is the one that is listened for. ('Control+K Meta+K' is how one shortcut
        // covers both a Windows and a macOS keyboard.)
        public static registerShortcut(inputId: string, shortcut: string) {
            SearchBox.unregisterShortcut(inputId);

            if (!inputId || !shortcut) return;

            const combos = shortcut.split(/\s+/).filter(c => c.length > 0).map(combo => {
                const parts = combo.split('+').filter(p => p.length > 0);
                const key = (parts.pop() || '').toLowerCase();
                const mods = parts.map(m => m.toLowerCase());
                return {
                    key,
                    alt: mods.indexOf('alt') > -1,
                    ctrl: mods.indexOf('control') > -1 || mods.indexOf('ctrl') > -1,
                    meta: mods.indexOf('meta') > -1 || mods.indexOf('command') > -1 || mods.indexOf('cmd') > -1,
                    shift: mods.indexOf('shift') > -1,
                };
            }).filter(c => c.key.length > 0);

            if (combos.length === 0) return;

            const handler = (e: KeyboardEvent) => {
                if (e.defaultPrevented || e.isComposing || e.keyCode === 229) return;

                const key = (e.key || '').toLowerCase();
                const match = combos.find(c => c.key === key &&
                    c.alt === e.altKey && c.ctrl === e.ctrlKey && c.meta === e.metaKey && c.shift === e.shiftKey);

                if (!match) return;

                // An unmodified shortcut - the bare '/' of a documentation site - is a character somebody may
                // be in the middle of typing somewhere else on the page, so it only fires outside of a field.
                // A modified one is nobody else's, and works wherever the focus happens to be.
                if (!match.alt && !match.ctrl && !match.meta && SearchBox.isTypingTarget(e.target)) return;

                const input = document.getElementById(inputId) as HTMLInputElement;
                if (!input || input.disabled) return;

                e.preventDefault();
                input.focus();
            };

            SearchBox.shortcuts[inputId] = handler;
            document.addEventListener('keydown', handler);
        }

        public static unregisterShortcut(inputId: string) {
            const handler = SearchBox.shortcuts[inputId];
            if (!handler) return;

            document.removeEventListener('keydown', handler);
            delete SearchBox.shortcuts[inputId];
        }

        // Whether the key would land in something the user is writing in, which is what an unmodified
        // shortcut must never steal. A read-only field is not one of those: nothing is typed into it.
        private static isTypingTarget(target: EventTarget | null): boolean {
            const element = target as HTMLElement;
            if (!element || !element.tagName) return false;

            const tag = element.tagName.toLowerCase();

            if (tag === 'textarea') return !(element as HTMLTextAreaElement).readOnly;
            if (tag === 'select') return true;
            if (element.isContentEditable) return true;

            if (tag === 'input') {
                const input = element as HTMLInputElement;
                // The types that hold no text of their own (a checkbox, a button, a radio) swallow nothing.
                const typeless = ['checkbox', 'radio', 'button', 'submit', 'reset', 'file', 'image', 'range', 'color'];
                return !input.readOnly && typeless.indexOf((input.type || 'text').toLowerCase()) < 0;
            }

            return false;
        }

        public static moveCursorToEnd(inputElement: HTMLInputElement) {
            if (!inputElement) return;

            try {
                inputElement.selectionStart = inputElement.selectionEnd = inputElement.value.length;
            } catch (e) { /* an input that does not support selection just keeps its caret */ }
        }

        // Keeps the virtually focused suggest item inside the visible area of the scroll container.
        // Element.scrollIntoView is deliberately not used here because it also scrolls every
        // scrollable ancestor (including the page) which makes the whole callout jump around.
        public static scrollItemIntoView(containerId: string, itemId: string) {
            const container = document.getElementById(containerId);
            const item = document.getElementById(itemId);
            if (!container || !item) return;

            try {
                const containerRect = container.getBoundingClientRect();
                const itemRect = item.getBoundingClientRect();

                if (itemRect.top < containerRect.top) {
                    container.scrollTop -= (containerRect.top - itemRect.top);
                } else if (itemRect.bottom > containerRect.bottom) {
                    container.scrollTop += (itemRect.bottom - containerRect.bottom);
                }
            } catch (e) { console.error('BitBlazorUI.SearchBox.scrollItemIntoView:', e); }
        }
    }
}
