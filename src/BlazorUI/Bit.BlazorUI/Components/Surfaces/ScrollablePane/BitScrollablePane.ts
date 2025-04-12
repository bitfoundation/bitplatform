namespace BitBlazorUI {
    export class ScrollablePane {
        public static scrollToEnd(element: HTMLInputElement) {
            if (!element) return;

            element.scrollTo(element.scrollWidth, element.scrollHeight);
        }
    }
}