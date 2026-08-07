namespace BitBlazorUI {
    export class SearchBox {
        public static moveCursorToEnd(inputElement: HTMLInputElement) {
            inputElement.selectionStart = inputElement.selectionEnd = inputElement.value.length;
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
