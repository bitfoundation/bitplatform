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
