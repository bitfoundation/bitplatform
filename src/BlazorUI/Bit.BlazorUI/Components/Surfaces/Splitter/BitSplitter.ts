namespace BitBlazorUI {
    export class Splitter {
        public static handleSplitterDragging(event: TouchEvent) {
            document.body.style.overscrollBehavior = 'none';
        };

        public static handleSplitterDraggingEnd() {
            document.body.style.overscrollBehavior = '';
        };

        public static getSplitterWidth(element: HTMLElement): number | null {
            if (!element || typeof element.getBoundingClientRect !== 'function') return null;

            try {
                return element.getBoundingClientRect().width;
            } catch (e) {
                console.error("BitBlazorUI.Splitter.getSplitterWidth:", e);
                return null;
            }
        };

        public static setSplitterWidth(element: HTMLElement, width: number) {
            if (!element || !element.style) return;

            try {
                element.style.width = width + 'px';
            } catch (e) { console.error("BitBlazorUI.Splitter.setSplitterWidth:", e); }
        };

        public static getSplitterHeight(element: HTMLElement): number | null {
            if (!element || typeof element.getBoundingClientRect !== 'function') return null;

            try {
                return element.getBoundingClientRect().height;
            } catch (e) {
                console.error("BitBlazorUI.Splitter.getSplitterHeight:", e);
                return null;
            }
        };

        public static setSplitterHeight(element: HTMLElement, height: number) {
            if (!element || !element.style) return;

            try {
                element.style.height = height + 'px';
            } catch (e) { console.error("BitBlazorUI.Splitter.setSplitterHeight:", e); }
        };

        public static resetPaneDimensions(element: HTMLElement | undefined) {
            if (!element || !element.style) return;

            try {
                element.style.width = '';
                element.style.height = '';
            } catch (e) { console.error("BitBlazorUI.Splitter.resetPaneDimensions:", e); }
        };
    }
}